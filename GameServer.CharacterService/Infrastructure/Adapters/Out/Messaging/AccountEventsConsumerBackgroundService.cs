using GameServer.CharacterService.Application.Ports.Out;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Messaging;

/// <summary>
/// Serviço em segundo plano que gerencia o consumidor de eventos de conta do RabbitMQ
/// </summary>
public class AccountEventsConsumerBackgroundService : BackgroundService
{
    private readonly IAccountEventsConsumer _consumer;
    private readonly ILogger<AccountEventsConsumerBackgroundService> _logger;

    public AccountEventsConsumerBackgroundService(
        IAccountEventsConsumer consumer,
        ILogger<AccountEventsConsumerBackgroundService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            _logger.LogInformation("Serviço de consumo de eventos de conta está sendo finalizado");
        });

        _logger.LogInformation("Serviço de consumo de eventos de conta iniciado");
        
        try
        {
            await _consumer.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar consumo de eventos de conta: {Message}", ex.Message);
        }
        
        await base.StopAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Parando serviço de consumo de eventos de conta");
        
        try
        {
            await _consumer.StopAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar consumo de eventos de conta: {Message}", ex.Message);
        }

        await base.StopAsync(stoppingToken);
    }
}