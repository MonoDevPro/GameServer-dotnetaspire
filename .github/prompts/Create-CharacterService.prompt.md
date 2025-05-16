> **Objetivo Geral:**
> Criar um microserviço .NET que será orquestrado pelo .NET Aspire, chamado **Server.CharacterService**, responsável por gerenciar personagens de jogadores e seus inventários, seguindo a **Arquitetura Hexagonal (Ports & Adapters)**, preparado para múltiplos ambientes (Desenvolvimento, QA, Produção) e implantável via .NET Aspire.

---

## 1. Comunicação entre Contextos de Conta e Personagem

### 1.1. Validação de Identidade via JWT

* O **AuthService** deve emitir um **JWT** contendo o `accountId` e claims necessários durante o login ([Styra][1]).
* O **CharacterService** **não** realiza chamadas síncronas ao AuthService para cada requisição de listagem — em vez disso, **valida localmente** o token JWT em todos os endpoints de personagem ([Styra][1]).

### 1.2. Propagação Assíncrona de Dados de Conta

* O AuthService publica eventos como `AccountCreated` e `AccountUpdated` no broker `RabbitMQ`(este broker ja está configurado no AppHost orquestrador, os microsserviços precisaram apenas adicionar o Aspire.RabbitMQ.Client) ([GeeksforGeeks][2]).
* O CharacterService consome esses eventos e mantém uma **tabela local** `AccountsCache` com dados essenciais de conta (ID, status, e-mail) ([GeeksforGeeks][2]).
* Ao chamar `GetCharactersByAccount(Guid accountId)`, o serviço faz **SELECT** em `Characters` filtrando por `accountId`, sem depender do AuthService no caminho crítico ([Stack Overflow][3]).

---

## 2. Camadas da Arquitetura Hexagonal

### 2.1. Domínio (Core)

* **Entidades:** `Character`, `InventoryItem`
* **Value Objects:** `CharacterId`, `ItemId`
* **Serviços de Domínio:** `CharacterCreationService`, `InventoryService`

### 2.2. Ports (Interfaces)

* **Driving Ports:**

  * `ICharacterUseCases`

    * `CreateCharacter(CreateCharacterRequest request)`
    * `GetCharactersByAccount(Guid accountId)`
  * `IInventoryUseCases`

    * `AddItem(Guid characterId, AddItemRequest request)`
    * `GetInventory(Guid characterId)`
* **Driven Ports:**

  * `ICharacterRepository` (CRUD de personagens)
  * `IInventoryRepository` (CRUD de itens de inventário)

### 2.3. Adapters

* **Inbound:**

  * Controladores ASP .NET Core Web API em `/api/characters` e `/api/inventory`
  * Validação de JWT via middleware (`AddAuthentication().AddJwtBearer()`) ([Styra][1])
* **Outbound:**

  * EF Core para PostgreSQL (`EfCharacterRepository`, `EfInventoryRepository`)
  * Cache Redis implementando `IInventoryRepository` para acelerar `GetInventory` ([GeeksforGeeks][2])
  * Consumidor de eventos Kafka/RabbitMQ para popular `AccountsCache` ([GeeksforGeeks][2])

---

## 3. Configuração de Ambientes e Implantação

* **.NET 9.0** com `WebApplication.CreateBuilder(args)`
* Health Check em `/healthz` ([DZone][4])
* `appsettings.{Development,QA,Production}.json`.

---

## 4. Requisitos Não-Funcionais

* **Escalabilidade:**

  * Serviço sem estado, auto-escalável.
* **Resiliência & Observabilidade:**

  * **Circuit breaker**, **timeout**, **retry** em chamadas a brokers/eventos ([DZone][4])
  * **Logging** estruturado (Serilog), **métricas** (Prometheus), **tracing** (OpenTelemetry).
* **Segurança:**

  * JWT Bearer e **RBAC** para proteger endpoints de modificação.
* **Manutenção de Inventário:**

  * `InventoryMaintenanceService` (BackgroundService) limpa itens expirados de forma agendada.

---

## 5. Entregáveis Esperados

1. **Solution** “CharacterService.sln” com projetos:

   * `Domain`, `Application`, `Infrastructure`, `Adapters`, `Api`
2. **Dockerfile** e **docker-compose.yml** para desenvolvimento local
3. **README** detalhando:

   * Setup Dev/QA/Prod
   * Exemplos de `GetCharactersByAccount` com JWT
   * Como validar e consumir eventos de `AccountCreated` e `AccountUpdated`

---