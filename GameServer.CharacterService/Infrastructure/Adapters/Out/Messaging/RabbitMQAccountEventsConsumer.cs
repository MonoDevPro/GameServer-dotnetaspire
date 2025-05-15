using System.Text;
using System.Text.Json;
using GameServer.CharacterService.Application.Models;
using GameServer.CharacterService.Application.Ports.Out;
using GameServer.CharacterService.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Messaging;

/// <summary>
/// Consumidor de eventos de conta do RabbitMQ, com reconexão automática e DLX/DLQ.
/// Projetado para ser usado como um IHostedService.
/// </summary>
public class RabbitMQAccountEventsConsumer : IAccountEventsConsumer, IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly IAccountsCache _accountsCache;
    private readonly ILogger<RabbitMQAccountEventsConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Exchange e filas
    private const string ExchangeName = "account.events";
    private static readonly (string Queue, string RoutingKey)[] Bindings =
    [
        ("character-service.account.created",       "account.created"),
        ("character-service.account.updated",       "account.updated"),
        ("character-service.account.status-changed","account.status-changed")
    ];

    public event EventHandler<AccountCreatedEvent>?         AccountCreated;
    public event EventHandler<AccountUpdatedEvent>?         AccountUpdated;
    public event EventHandler<AccountStatusChangedEvent>?   AccountStatusChanged;

    // Token para controlar o ciclo de vida do serviço/consumo
    private readonly CancellationTokenSource _cts = new();

    public RabbitMQAccountEventsConsumer(
        IConnectionFactory connectionFactory,
        IAccountsCache accountsCache,
        ILogger<RabbitMQAccountEventsConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _accountsCache = accountsCache;
        _logger = logger;

        // Configurações da ConnectionFactory (aplicadas antes de criar a conexão)
        if (_connectionFactory is ConnectionFactory cf)
        {
            cf.AutomaticRecoveryEnabled = true;
            cf.TopologyRecoveryEnabled = true;
            // cf.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var combinedToken = linkedCts.Token;

        try
        {
            _logger.LogInformation("RabbitMQ: Iniciando conexão...");
            combinedToken.ThrowIfCancellationRequested();

            _connection = await _connectionFactory.CreateConnectionAsync(combinedToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: combinedToken);

            if (_connection is IRecoverable recoverableConnection)
            {
                recoverableConnection.RecoveryAsync += RecoverableConnection_RecoveryAsync;
                _logger.LogDebug("RabbitMQ: Assinado evento RecoveryAsync na conexão.");
            }
            if (_channel is IRecoverable recoverableChannel)
            {
                recoverableChannel.RecoveryAsync += RecoverableChannel_RecoveryAsync;
                _logger.LogDebug("RabbitMQ: Assinado evento RecoveryAsync no canal.");
            }


            _logger.LogInformation("RabbitMQ: Conexão estabelecida. Declarando topologia...");
            combinedToken.ThrowIfCancellationRequested();

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: combinedToken);
            _logger.LogDebug("RabbitMQ: Exchange '{ExchangeName}' declarada.", ExchangeName);

            foreach (var (queue, rk) in Bindings)
            {
                combinedToken.ThrowIfCancellationRequested();
                await DeclareQueueWithDeadLetter(queue, rk);
            }


            _logger.LogInformation("RabbitMQ: Topologia inicializada. Configurando QoS...");
            combinedToken.ThrowIfCancellationRequested();

            await _channel.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: combinedToken);
            _logger.LogDebug("RabbitMQ: QoS configurado com prefetch={Prefetch}", 10);


            _logger.LogInformation("RabbitMQ: Iniciando consumo em todas as filas...");
            foreach (var (queue, _) in Bindings)
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleEventAsync;

                await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, cancellationToken: combinedToken);
                _logger.LogInformation("RabbitMQ: Consumidor iniciado para fila '{Queue}'.", queue);
            }

            _logger.LogInformation("RabbitMQ: Consumo iniciado com sucesso.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ: Inicialização ou conexão cancelada.");
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ: Erro durante a inicialização ou conexão: {Message}", ex.Message);
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private Task RecoverableConnection_RecoveryAsync(object? sender, AsyncEventArgs e)
    {
        _logger.LogInformation("RabbitMQ: Evento RecoveryAsync disparado na conexão.");
        return Task.CompletedTask;
    }

    private Task RecoverableChannel_RecoveryAsync(object? sender, AsyncEventArgs e)
    {
        _logger.LogInformation("RabbitMQ: Evento RecoveryAsync disparado no canal.");
        return Task.CompletedTask;
    }


    private async Task DeclareQueueWithDeadLetter(string queueName, string routingKey)
    {
        var dlxName = $"{queueName}.dlx";
        var dlqName = $"{queueName}.dlq";

        var channel = _channel ?? throw new InvalidOperationException("Channel not initialized.");

        await channel.ExchangeDeclareAsync(dlxName, ExchangeType.Topic, durable: true, autoDelete: false);
        _logger.LogDebug("RabbitMQ: DLX '{DlxName}' declarada.", dlxName);

        await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false);
        _logger.LogDebug("RabbitMQ: DLQ '{DlqName}' declarada.", dlqName);

        await channel.QueueBindAsync(dlqName, dlxName, routingKey);
        _logger.LogDebug("RabbitMQ: DLQ '{DlqName}' ligada à DLX '{DlxName}' com routingKey '{RK}'.", dlqName, dlxName, routingKey);


        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = dlxName,
            ["x-dead-letter-routing-key"] = routingKey
        };

        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        _logger.LogDebug("RabbitMQ: Fila principal '{QueueName}' declarada com DLX configurada.", queueName);

        await channel.QueueBindAsync(queueName, ExchangeName, routingKey);
        _logger.LogDebug("RabbitMQ: Fila principal '{QueueName}' ligada ao Exchange '{ExchangeName}' com routingKey '{RK}'.", queueName, ExchangeName, routingKey);
    }

    private async Task HandleEventAsync(object? sender, BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        var channel = _channel ?? throw new InvalidOperationException("Channel not initialized.");

        try
        {
            if (_cts.IsCancellationRequested)
            {
                _logger.LogInformation("Processamento de mensagem cancelado devido ao desligamento. Nacking DeliveryTag {DeliveryTag} para DLQ.", ea.DeliveryTag);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            switch (routingKey)
            {
                case "account.created":
                    var createdEvt = JsonSerializer.Deserialize<AccountCreatedEvent>(json, _jsonOptions);
                    if (createdEvt != null)
                    {
                        _logger.LogInformation("Evento recebido: account.created {AccountId}", createdEvt.Id);
                        await _accountsCache.SetAsync(new AccountCache(createdEvt.Id, createdEvt.Email, createdEvt.IsActive));
                        AccountCreated?.Invoke(this, createdEvt);
                    }
                    else
                    {
                        _logger.LogWarning("Falha ao deserializar evento account.created da routing key '{RK}'. Mensagem: {Json}. Nacking para DLQ.", ea.RoutingKey, json);
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }
                    break;

                case "account.updated":
                    var updatedEvt = JsonSerializer.Deserialize<AccountUpdatedEvent>(json, _jsonOptions);
                    if (updatedEvt != null)
                    {
                        _logger.LogInformation("Evento recebido: account.updated {AccountId}", updatedEvt.Id);
                        var existing = await _accountsCache.GetByIdAsync(updatedEvt.Id);
                        if (existing != null)
                        {
                            existing.Update(updatedEvt.Email, updatedEvt.IsActive);
                            await _accountsCache.SetAsync(existing);
                        }
                        else
                        {
                            _logger.LogWarning("Evento account.updated recebido para conta não encontrada no cache: {AccountId}. Mensagem: {Json}. Acking.", updatedEvt.Id, json);
                        }
                        AccountUpdated?.Invoke(this, updatedEvt);
                    }
                    else
                    {
                        _logger.LogWarning("Falha ao deserializar evento account.updated da routing key '{RK}'. Mensagem: {Json}. Nacking para DLQ.", ea.RoutingKey, json);
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }
                    break;

                case "account.status-changed":
                    var statusEvt = JsonSerializer.Deserialize<AccountStatusChangedEvent>(json, _jsonOptions);
                    if (statusEvt != null)
                    {
                        _logger.LogInformation("Evento recebido: account.status-changed {AccountId}, IsActive: {IsActive}",
                            statusEvt.Id, statusEvt.IsActive);
                        var existing = await _accountsCache.GetByIdAsync(statusEvt.Id);
                        if (existing != null)
                        {
                            existing.Update(existing.Email, statusEvt.IsActive);
                            await _accountsCache.SetAsync(existing);
                        }
                        else
                        {
                            _logger.LogWarning("Evento account.status-changed recebido para conta não encontrada no cache: {AccountId}. Mensagem: {Json}. Acking.", statusEvt.Id, json);
                        }
                        AccountStatusChanged?.Invoke(this, statusEvt);
                    }
                    else
                    {
                        _logger.LogWarning("Falha ao deserializar evento account.status-changed da routing key '{RK}'. Mensagem: {Json}. Nacking para DLQ.", ea.RoutingKey, json);
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }
                    break;

                default:
                    _logger.LogWarning("RoutingKey desconhecida: {RK} da fila '{Queue}'. Mensagem: {Json}. Nacking para DLQ.", routingKey, ea.RoutingKey, json);
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            _logger.LogDebug("Evento {RK} com DeliveryTag {DeliveryTag} confirmado.", routingKey, ea.DeliveryTag);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar evento '{RK}' com DeliveryTag {DeliveryTag}. Mensagem: {Json}. Nacking para DLQ.",
                routingKey, ea.DeliveryTag, json);

            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            _logger.LogWarning("Evento {RK} com DeliveryTag {DeliveryTag} Nacked (requeue=false) devido a erro inesperado.", routingKey, ea.DeliveryTag);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ: Sinalizando cancelamento...");
        await _cts.CancelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("RabbitMQ: Iniciando liberação de recursos...");

        try { await _cts.CancelAsync(); }
        catch
        {
            // ignored
        }

        // Cria um CancellationTokenSource para dar um timeout nas operações de fechamento
        using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5 segundos para fechar
        var shutdownToken = shutdownCts.Token;

        if (_channel != null)
        {
            try
            {
                if (_channel.IsOpen)
                {
                    _logger.LogDebug("RabbitMQ: Fechando canal...");
                    // Passa o token do timeout
                    await _channel.CloseAsync(shutdownToken);
                    _logger.LogDebug("RabbitMQ: Canal fechado.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("RabbitMQ: Fechamento do canal cancelado por timeout.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ: Erro ao tentar fechar o canal.");
            }
            finally
            {
                // DisposeAsync deve ser chamado sempre que possível para liberar recursos não gerenciados
                try
                {
                    await _channel.DisposeAsync();
                    _logger.LogDebug("RabbitMQ: Canal liberado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ: Erro ao liberar o canal.");
                }
            }
            _channel = null;
        }

        if (_connection != null)
        {
            try
            {
                if (_connection.IsOpen)
                {
                    _logger.LogDebug("RabbitMQ: Fechando conexão...");
                    // Passa o token do timeout
                    await _connection.CloseAsync(shutdownToken);
                    _logger.LogDebug("RabbitMQ: Conexão fechada.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("RabbitMQ: Fechamento da conexão cancelado por timeout.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ: Erro ao tentar fechar a conexão.");
            }
            finally
            {
                // DisposeAsync deve ser chamado sempre que possível
                try
                {
                    await _connection.DisposeAsync();
                    _logger.LogDebug("RabbitMQ: Conexão liberada.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ: Erro ao liberar a conexão.");
                }
            }
            _connection = null;
        }

        // O shutdownCts é automaticamente disposto pelo 'using'

        try { _cts.Dispose(); }
        catch
        {
            // ignored
        }

        _logger.LogInformation("RabbitMQ: Recursos liberados com sucesso.");
    }

    // Assume que a interface IAccountEventsConsumer foi atualizada:
    /*
    public interface IAccountEventsConsumer
    {
        event EventHandler<AccountCreatedEvent>? AccountCreated;
        event EventHandler<AccountUpdatedEvent>? AccountUpdated;
        event EventHandler<AccountStatusChangedEvent>? AccountStatusChanged;

        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
    */
}