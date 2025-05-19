# Estrutura de Diretório para Microsserviço com Arquitetura Hexagonal

## Visão Geral da Arquitetura Hexagonal

A arquitetura hexagonal (também conhecida como Ports & Adapters) separa a lógica de negócio das interações externas, permitindo que sua aplicação seja independente de tecnologias específicas.

## Estrutura de Diretórios

```
GameServer.AuthService/
├── Application/
│   ├── Ports/
│   │   ├── In/          # Interfaces para entrada de dados (casos de uso)
│   │   └── Out/         # Interfaces para saída de dados (repositórios, serviços)
│   ├── Services/        # Implementação dos casos de uso
│   └── Domain/
│       ├── Models/      # Entidades e objetos de valor
│       ├── Events/      # Eventos de domínio
│       └── Exceptions/  # Exceções específicas do domínio
│
├── Infrastructure/
│   ├── Adapters/
│   │   ├── In/          # Adaptadores de entrada (Controllers, APIs)
│   │   └── Out/         # Adaptadores de saída (Repositórios, Clientes HTTP)
│   ├── Configuration/   # Configurações de infraestrutura
│   └── Security/        # Implementações de segurança
│
├── Program.cs           # Ponto de entrada da aplicação
└── appsettings.json     # Configurações da aplicação
```

## Representação Visual da Arquitetura Hexagonal

```
┌──────────────────────────────────────────────────────────┐
│                                                          │
│  ┌─────────────┐         DOMÍNIO         ┌─────────────┐ │
│  │   Adapters  │      ┌──────────┐       │   Adapters  │ │
│  │     In      │◄────►│          │◄─────►│     Out     │ │
│  │             │      │ Aplicação│       │             │ │
│  │ Controllers │      │          │       │Repositories │ │
│  │    APIs     │      │ Services │       │   Clients   │ │
│  │             │      │          │       │             │ │
│  └─────┬───────┘      └──────────┘       └──────┬──────┘ │
│        │                                        │        │
└────────┼────────────────────────────────────────┼────────┘
         │                                        │
         ▼                                        ▼
   ┌───────────┐                           ┌────────────┐
   │ Interface │                           │  Sistemas  │
   │    do     │                           │  Externos  │
   │  Usuário  │                           │            │
   └───────────┘                           └────────────┘
```

## Descrição dos Componentes

### Domínio e Aplicação
- **Domain/Entities**: Classes que representam as entidades principais do negócio
- **Domain/ValueObjects**: Objetos de valor que encapsulam conceitos do domínio
- **Domain/Events**: Eventos que representam mudanças de estado no domínio
- **Domain/Exceptions**: Exceções específicas do domínio para tratamento de erros
- **Application/Models**: Classes que representam os dados de entrada e saída dos casos de uso
- **Application/Ports/In**: Definem os casos de uso que o sistema oferece
- **Application/Ports/Out**: Definem as interfaces para acessar recursos externos
- **Application/Services**: Implementam a lógica de negócio usando as portas

### Adaptadores
- **Infrastructure/Adapters/In**: Implementações que recebem solicitações externas, ex: Controllers, APIs
- **Infrastructure/Adapters/Out**: Implementações para acessar sistemas externos (bancos de dados, APIs)

### Configuração
- **Infrastructure/Configuration**: Configurações específicas do ambiente, como conexão com banco de dados, serviços externos, etc.
- **Infrastructure/Security**: Implementações de segurança, como autenticação e autorização

### Ponto de Entrada
- **Program.cs**: Ponto de entrada da aplicação, onde a configuração e inicialização ocorrem
- **appsettings.json**: Arquivo de configuração da aplicação, onde são definidas as variáveis de ambiente e configurações específicas

### Benefícios desta Estrutura
- **Testabilidade**: Núcleo de aplicação isolado, facilitando testes unitários
- **Flexibilidade**: Fácil substituição de tecnologias externas
- **Manutenibilidade**: Separação clara de responsabilidades

