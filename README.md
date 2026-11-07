# Roadmaps API

API desenvolvida em C# e ASP.NET Core para gerenciamento de roadmaps de estudos, tópicos e acompanhamento de progresso.

## Tecnologias

- .NET 10
- C#
- ASP.NET Core Web API
- Entity Framework Core 10
- SQL Server 2022 com Full-Text Search
- Docker e Docker Compose
- xUnit para testes automatizados

## Estrutura da solução

A solução está organizada em projetos com responsabilidades distintas:

| Projeto | Responsabilidade |
|---|---|
| `Roadmaps.Domain` | Entidades e regras de negócio |
| `Roadmaps.Application` | Casos de uso e lógica de aplicação |
| `Roadmaps.Infrastructure` | Persistência, implementações de infraestrutura e integrações externas |
| `Roadmaps.Api` | Endpoints HTTP e configuração da API |
| `Roadmaps.UnitTests` | Testes unitários |
| `Roadmaps.IntegrationTests` | Testes de integração |

Os projetos estão distribuídos em dois diretórios:

- `src/`: código-fonte da aplicação.
- `tests/`: projetos de testes automatizados.

A camada `Application` utiliza uma organização por funcionalidade, inicialmente com os módulos `Roadmaps` e `Topics`.

## Pré-requisitos

- .NET SDK 10
- Docker com Docker Compose

O arquivo `global.json` define a versão-base `10.0.403` do SDK, permitindo atualizações de patch compatíveis dentro da mesma feature band.

## Configuração inicial

Copie o arquivo `.env.example` para `.env` e ajuste as variáveis conforme necessário.

No PowerShell:

```powershell
Copy-Item .env.example .env
```

O arquivo `.env` contém configurações locais e é ignorado pelo Git.

### Connection string

A conexão com o SQL Server depende de onde a API está sendo executada:

| Ambiente | Host do banco |
|---|---|
| API local | `localhost,1433` |
| API no Docker | `db,1433` |

Para executar a API localmente, configure a connection string utilizando o User Secrets do .NET:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=Roadmaps;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True" --project src/Roadmaps.Api
```

Substitua `SUA_SENHA` pela senha configurada no `.env`. Se alterar `MSSQL_PORT`, ajuste também a porta na connection string local.

Os segredos são armazenados fora do repositório. O projeto já possui um `UserSecretsId` configurado.

No Docker, a variável `ConnectionStrings__DefaultConnection` do `.env` é repassada à API pelo Compose. Ao alterar a senha, atualize tanto `MSSQL_SA_PASSWORD` quanto a senha dessa connection string. O arquivo `.env` não é carregado automaticamente pela API executada no host.

Com a configuração padrão do ASP.NET Core, a precedência é: argumentos de linha de comando > variáveis de ambiente > User Secrets (em Development) > `appsettings.Development.json` > `appsettings.json`. Os arquivos `appsettings` versionados contêm apenas configurações de logging e hosts, sem credenciais.

A API utiliza Entity Framework Core com SQL Server. O registro do `RoadmapsDbContext` é realizado por `AddInfrastructure`, usando `ConnectionStrings:DefaultConnection`.

Em `Development`, a API aplica automaticamente as migrations pendentes durante a inicialização. Em produção, a aplicação das migrations deve ocorrer de forma controlada, por exemplo com scripts SQL revisados antes do deploy.

## Desenvolvimento

### Executar infraestrutura no Docker e API localmente

Este é o fluxo recomendado para desenvolvimento.

Inicie a infraestrutura:

```powershell
docker compose up -d
```

O comando inicia somente o SQL Server. Aguarde o container ficar saudável antes de iniciar a API.

Depois, execute a API localmente com Hot Reload:

```powershell
dotnet watch run --project src/Roadmaps.Api
```

A porta utilizada pela API local é definida em `src/Roadmaps.Api/Properties/launchSettings.json`.

### Executar API e infraestrutura no Docker

Para executar todos os serviços no Docker:

```powershell
docker compose --profile full up -d --build
```

A API estará acessível em `http://localhost:8080` (ou na porta configurada em `API_PORT`). A rota `/` retorna HTTP 404 enquanto não houver um endpoint raiz; isso é esperado.

O serviço da API utiliza o profile `full`, enquanto o SQL Server é iniciado por padrão.

### Encerrar os serviços

Para interromper a API executada localmente, utilize `Ctrl+C` no terminal.

Para encerrar os containers, incluindo a API do profile `full`:

```powershell
docker compose --profile full down
```

Esse comando remove os containers e a rede, mas preserva os dados do banco.

### Resetar o banco de dados

**Atenção:** o comando abaixo remove os volumes do projeto e exclui permanentemente os dados armazenados no SQL Server.

```powershell
docker compose --profile full down -v
```

Para iniciar novamente somente a infraestrutura:

```powershell
docker compose up -d
```

## Comandos .NET

Restaurar as dependências:

```powershell
dotnet restore
```

Compilar a solução:

```powershell
dotnet build
```

Executar os testes automatizados disponíveis:

```powershell
dotnet test
```

Executar a API sem Hot Reload:

```powershell
dotnet run --project src/Roadmaps.Api
```

## Entity Framework Core e migrations

O projeto utiliza EF Core com o provider SQL Server. O contexto de persistência fica em `src/Roadmaps.Infrastructure/Persistence/RoadmapsDbContext.cs` e é registrado na API por meio de `AddInfrastructure`.

As migrations são mantidas em `src/Roadmaps.Infrastructure/Persistence/Migrations/` e versionadas junto com o código.

### Convenções de persistência

As seguintes convenções já estão configuradas:

| Recurso | Convenção |
|---|---|
| Collation do modelo | `Latin1_General_100_CI_AI` |
| `DateOnly` | `date` |
| `DateTime` | `datetime2(3)` (precisão de milissegundos) |
| Auditoria | `CreatedAt` e `UpdatedAt` para entidades que implementam `IAuditableEntity` |
| Concorrência | `RowVersion` para entidades que implementam `IConcurrencyEntity` |
| Mapeamentos | Descoberta automática de `IEntityTypeConfiguration<T>` no assembly de Infrastructure |

A auditoria é preenchida pelo `RoadmapsDbContext` durante `SaveChanges` e `SaveChangesAsync`:

- Ao inserir uma entidade auditável, `CreatedAt` e `UpdatedAt` recebem a data e hora atual em UTC.
- Ao modificar uma entidade auditável, `UpdatedAt` é atualizado e `CreatedAt` é preservado.

O SQL Server armazena `datetime2(3)` sem informação de fuso horário. Por isso, a aplicação adota UTC como convenção para os timestamps de auditoria.

Para entidades que implementam `IConcurrencyEntity`, a propriedade `RowVersion` é configurada como token de concorrência gerenciado pelo SQL Server.

**Convenções para entidades futuras:** utilizar chaves `Id` inteiras geradas por `IDENTITY` quando aplicável, definir comprimentos explícitos para strings persistidas e mapear enums compatíveis para `tinyint` (valores de 0 a 255). Essas regras serão aplicadas nos mapeamentos das entidades concretas; ainda não são convenções globais implementadas no contexto.

A configuração e os comportamentos de persistência serão verificados por testes de integração com SQL Server e Testcontainers na [issue #35](https://github.com/aleretamero/roadmaps-api/issues/35), após a infraestrutura prevista na [issue #7](https://github.com/aleretamero/roadmaps-api/issues/7).

### Ferramenta de migrations

O repositório utiliza `dotnet-ef` como ferramenta local, com versão definida no manifesto de ferramentas da solução. Para restaurá-la em uma nova máquina, execute na raiz:

```powershell
dotnet tool restore
```

Os comandos abaixo devem ser executados a partir da raiz do repositório. Eles utilizam `Roadmaps.Infrastructure` como projeto das migrations e `Roadmaps.Api` como projeto de inicialização, que fornece a configuração da conexão.

### Criar uma migration

Após alterar o modelo de persistência:

```powershell
dotnet tool run dotnet-ef migrations add NomeDaMigration --project src/Roadmaps.Infrastructure --startup-project src/Roadmaps.Api --output-dir Persistence/Migrations
```

Revise os arquivos gerados antes de versioná-los.

### Aplicar migrations manualmente

Com o SQL Server em execução e a connection string configurada:

```powershell
dotnet tool run dotnet-ef database update --project src/Roadmaps.Infrastructure --startup-project src/Roadmaps.Api
```

Para listar as migrations e consultar seu estado no banco:

```powershell
dotnet tool run dotnet-ef migrations list --project src/Roadmaps.Infrastructure --startup-project src/Roadmaps.Api
```

### Aplicação automática em Development

Durante a inicialização da API, quando o ambiente é `Development`, o `Program.cs` executa `Database.MigrateAsync()` para aplicar migrations pendentes.

Esse comportamento **não é executado automaticamente em Production**. A implantação em produção deve utilizar um processo controlado de atualização do banco.

### Gerar script SQL para produção

Para gerar um script idempotente, que verifica quais migrations já foram aplicadas:

```powershell
dotnet tool run dotnet-ef migrations script --idempotent --project src/Roadmaps.Infrastructure --startup-project src/Roadmaps.Api --output migration.sql
```

Revise o conteúdo de `migration.sql` e execute-o no ambiente de destino conforme o processo de deploy. O script é um artefato gerado sob demanda; os arquivos de migration em C# são a fonte versionada no Git.

### Migration inicial

A migration `InitialCreate` foi criada e aplicada ao SQL Server local. Como ainda não existem entidades de negócio mapeadas, seus métodos `Up` e `Down` estão vazios. Sua aplicação é registrada na tabela `__EFMigrationsHistory`.

Isso é esperado nesta etapa: as tabelas de negócio serão adicionadas em migrations posteriores.

## SQL Server e Full-Text Search

O ambiente utiliza SQL Server 2022 com Full-Text Search, por meio de uma imagem Docker personalizada.

O Dockerfile do banco está localizado em:

`docker/sqlserver/Dockerfile`

Para verificar se o Full-Text Search está instalado, execute no SQL Server:

```sql
SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS IsFullTextInstalled;
```

O resultado esperado é `1`.

Os dados do SQL Server são armazenados em um volume nomeado do Docker, permitindo sua preservação entre reinicializações dos containers.
