# Estrutura de Diretório para Microsserviço com Arquitetura Hexagonal (.NET)

## Visão Geral da Arquitetura Hexagonal

A arquitetura hexagonal (Ports & Adapters) separa a lógica de negócio das interações externas, tornando a aplicação independente de frameworks, bancos de dados e tecnologias específicas.

## Estrutura de Diretórios Real do Projeto

```
GameServer.AccountService/
│
├── AccountManagement/
│   ├── Adapters/
│   │   ├── In/              # Adaptadores de entrada (Controllers, APIs)
│   │   └── Out/             # Adaptadores de saída (Repositórios, Clientes HTTP)
│   ├── Application/
│   │   ├── CQRS/            # Implementação de Command/Query
│   │   ├── DTO/             # Objetos de transferência de dados
│   │   └── UsesCases/       # Implementação dos casos de uso
│   ├── Domain/
│   │   ├── Entities/        # Entidades do domínio
│   │   ├── Enums/
│   │   ├── Events/          # Eventos de domínio
│   │   ├── Exceptions/      # Exceções específicas do domínio
│   │   └── ValueObjects/
│   ├── Infrastructure/
│   │   ├── DI/              # Injeção de dependências
│   │   ├── UnityOfWork/     # Unidade de trabalho
│   │   └── Utils/           # Utilitários
│   └── Ports/
│       ├── In/              # Interfaces para entrada de dados (casos de uso)
│       └── Out/             # Interfaces para saída de dados (repositórios, serviços)
│
├── Program.cs               # Ponto de entrada da aplicação
├── appsettings.json          # Configuração principal
├── appsettings.Development.json # Configuração para ambiente de desenvolvimento
├── GameServer.AccountService.csproj # Arquivo de projeto .NET
├── Properties/              # Configurações do projeto (ex: launchSettings.json)
├── bin/                     # Saída de build
└── obj/                     # Arquivos temporários de build
```

## Representação Visual da Arquitetura Hexagonal

```
┌──────────────────────────────────────────────────────────┐
│                                                          │
│  ┌─────────────┐         DOMÍNIO         ┌─────────────┐ │
│  │   Adapters  │      ┌──────────┐       │   Adapters  │ │
│  │     In      │◄────►│          │◄─────►│     Out     │ │
│  │ Controllers │      │ Aplicação│       │Repositories │ │
│  │    APIs     │      │  Domain  │       │   Clients   │ │
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

### AccountManagement/
- **Adapters/In**: Adaptadores de entrada, como Controllers e APIs REST/gRPC.
- **Adapters/Out**: Adaptadores de saída, como repositórios, clientes HTTP, integrações externas.
- **Application/**: Camada de aplicação, onde ficam os casos de uso, DTOs e (se houver) CQRS.
- **Domain/**: Núcleo do domínio, com entidades, objetos de valor, enums, eventos e exceções.
- **Infrastructure/**: Implementações técnicas, utilitários, injeção de dependências, unidade de trabalho.
- **Ports/In**: Interfaces para entrada de dados (contratos dos casos de uso).
- **Ports/Out**: Interfaces para saída de dados (contratos de repositórios, serviços externos).

### Raiz do Serviço
- **Program.cs**: Ponto de entrada da aplicação.
- **appsettings.json / appsettings.Development.json**: Configurações da aplicação.
- **GameServer.AccountService.csproj**: Arquivo de projeto .NET.
- **Properties/**: Configurações do projeto (ex: launchSettings.json).
- **bin/** e **obj/**: Pastas geradas pelo build do .NET.

## Observações e Boas Práticas
- **Padronização**: Esta estrutura é replicada nos demais microsserviços do GameServer, facilitando manutenção e evolução.
- **Testabilidade**: Núcleo de domínio isolado, facilitando testes unitários.
- **Flexibilidade**: Permite troca de tecnologias externas sem impacto no domínio.
- **Manutenibilidade**: Separação clara de responsabilidades.

## Dicas
- Sempre alinhe a documentação com a estrutura real do projeto.
- Atualize este arquivo ao criar novas pastas ou componentes relevantes.
- Utilize comentários nos diretórios para facilitar o entendimento de novos desenvolvedores.

