# GameServer.AuthServer

Microsserviço de autorização baseado em OpenIddict e ASP.NET Core Identity, orquestrado pelo .NET Aspire.

## Características

- **Autenticação e Autorização**: Baseado em OpenIddict e ASP.NET Core Identity
- **Microsserviço Aspire**: Totalmente integrado com .NET Aspire para orquestração
- **Persistência**: Entity Framework Core com PostgreSQL
- **Health Checks**: Monitoramento de saúde com endpoints `/health` e `/alive`
- **Telemetria**: OpenTelemetry integrado para logs, métricas e traces
- **Registro e Login**: Páginas Identity scaffolded para gestão de usuários

## Configuração

### Connection String

A connection string `authdb` é automaticamente injetada pelo Aspire. Para desenvolvimento local sem Aspire, o sistema usa um banco em memória como fallback.

```json
{
  "ConnectionStrings": {
    "authdb": ""
  }
}
```

### Variáveis de Ambiente

- `OTEL_EXPORTER_OTLP_ENDPOINT`: Endpoint do OpenTelemetry Collector (configurado pelo Aspire)

## Execução

### Com .NET Aspire (Recomendado)

```bash
cd GameServer.AppHost
dotnet run
```

O AuthServer será automaticamente:
- Configurado com a connection string correta
- Conectado ao PostgreSQL
- Instrumentado com telemetria
- Monitorado com health checks

### Desenvolvimento Local

```bash
cd GameServer.AuthServer
dotnet run
```

Nota: Em modo local, usará banco em memória.

## Migrations

### Aplicar Migrations

```bash
cd GameServer.AuthServer
dotnet ef database update
```

### Criar Nova Migration

```bash
cd GameServer.AuthServer
dotnet ef migrations add <NomeDaMigration> --output-dir Migrations
```

## Endpoints

### Autenticação

- `GET/POST /Identity/Account/Login` - Página de login
- `GET/POST /Identity/Account/Register` - Página de registro
- `POST /Identity/Account/Logout` - Logout

### OpenID Connect / OAuth2

- `GET /connect/authorize` - Endpoint de autorização
- `POST /connect/token` - Endpoint de token
- `GET /connect/userinfo` - Informações do usuário
- `POST /connect/introspect` - Introspecção de token
- `POST /connect/endsession` - Finalizar sessão

### Health Checks

- `GET /health` - Health check completo (apenas em desenvolvimento)
- `GET /alive` - Liveness check (apenas em desenvolvimento)

## Testando

### 1. Registro de Novo Usuário

```bash
curl -X POST https://localhost:7001/Identity/Account/Register \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Input.Email=test@example.com&Input.Password=Test123!&Input.ConfirmPassword=Test123!"
```

### 2. Login

```bash
curl -X POST https://localhost:7001/Identity/Account/Login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Input.Email=test@example.com&Input.Password=Test123!&Input.RememberMe=false"
```

### 3. Obter Token (Authorization Code Flow)

1. Acesse o endpoint de autorização:
```
GET https://localhost:7001/connect/authorize?client_id=oidc_certification_app_1&redirect_uri=https://www.certification.openid.net/test/a/d6e0d2a6-003e-4721-8b67-a24380468aa8/callback&response_type=code&scope=openid profile email&state=test
```

2. Após login, use o código retornado para obter o token:
```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code&client_id=oidc_certification_app_1&client_secret=secret_secret_secret&code=<AUTHORIZATION_CODE>&redirect_uri=https://www.certification.openid.net/test/a/d6e0d2a6-003e-4721-8b67-a24380468aa8/callback"
```

### 4. Usar Token

```bash
curl -X GET https://localhost:7001/connect/userinfo \
  -H "Authorization: Bearer <ACCESS_TOKEN>"
```

## Configuração de Clientes

Os clientes OAuth/OIDC são configurados em `appsettings.json` na seção `OpenIddict:Clients`. O sistema automaticamente registra esses clientes na inicialização.

## Usuário Padrão

Um usuário administrador padrão é criado automaticamente:
- **Email**: admin@gameserver.local
- **Senha**: Admin123!

## Logs e Telemetria

- Logs estruturados via Microsoft.Extensions.Logging
- Instrumentação automática do ASP.NET Core, EF Core e HTTP clients
- Exportação para OpenTelemetry Collector configurado pelo Aspire
- Traces de requests HTTP e operações de banco de dados

## Arquitetura

O AuthServer segue os padrões do .NET Aspire:

1. **Service Discovery**: Registrado automaticamente para descoberta por outros serviços
2. **Health Checks**: Verifica conectividade com banco de dados
3. **Resiliência**: HTTP clients com retry policies padrão
4. **Observabilidade**: Telemetria completa integrada

## Próximos Passos

- Configurar autenticação externa (Google, Microsoft, etc.)
- Implementar recuperação de senha
- Adicionar autorização baseada em roles/claims
- Configurar rate limiting
- Implementar auditoria de segurança
