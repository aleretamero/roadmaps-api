# Roadmaps API

API desenvolvida em C# e ASP.NET Core para gerenciamento de roadmaps de estudos, tópicos e acompanhamento de progresso.

## Tecnologias

- .NET 10 e C#
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

Os projetos estão distribuídos em `src/` (aplicação) e `tests/` (testes automatizados).

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

O arquivo `.env` contém configurações locais e é ignorado pelo Git. Não versione senhas nem connection strings com credenciais.

### Connection string

O host do SQL Server depende de onde a API está sendo executada:

| Execução da API | Host do banco |
|---|---|
| API local | `localhost,1433` |
| API no Docker Compose | `db,1433` |

Para executar a API localmente, configure a connection string usando o User Secrets do .NET:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=Roadmaps;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True" --project src/Roadmaps.Api
```

Substitua `SUA_SENHA` pela senha configurada no `.env`. Se alterar `MSSQL_PORT`, ajuste também a porta na connection string local. O `TrustServerCertificate=True` é utilizado aqui apenas para o ambiente local com certificado não confiável; nos ambientes compartilhados, configure TLS e validação de certificado adequados.

Os segredos são armazenados fora do repositório. O projeto já possui um `UserSecretsId` configurado.

No Docker, a variável `ConnectionStrings__DefaultConnection` do `.env` é repassada à API pelo Compose. Ao alterar a senha, atualize tanto `MSSQL_SA_PASSWORD` quanto a senha da connection string. O arquivo `.env` não é carregado automaticamente pela API executada diretamente no host.

Na configuração padrão do ASP.NET Core, a precedência é: argumentos de linha de comando > variáveis de ambiente > User Secrets (em Development) > `appsettings.Development.json` > `appsettings.json`. Os arquivos `appsettings` versionados contêm apenas configurações de logging e hosts, sem credenciais.

O `RoadmapsDbContext` é registrado por `AddInfrastructure`, usando `ConnectionStrings:DefaultConnection`.

## Desenvolvimento

### Executar infraestrutura no Docker e API localmente

Este é o fluxo recomendado para desenvolvimento.

Inicie os serviços de infraestrutura:

```powershell
docker compose up -d
```

O Compose inicia o SQL Server (`db`), aguarda seu healthcheck e executa o serviço de provisionamento `db-init`. Esse serviço cria o banco `Roadmaps` com a collation esperada caso ainda não exista, ou valida a collation se ele já existir.

Confira o estado dos serviços:

```powershell
docker compose ps -a
```

O resultado esperado é `db` saudável e `db-init` finalizado com código `0`. O `db-init` é um processo de execução única: não precisa permanecer em execução. Se ele falhar, consulte:

```powershell
docker compose logs db-init
```

Depois, execute a API localmente com Hot Reload:

```powershell
dotnet watch run --project src/Roadmaps.Api
```

Em `Development`, a API aplica as migrations pendentes na inicialização. A porta utilizada pela API local é definida em `src/Roadmaps.Api/Properties/launchSettings.json`.

### Executar API e infraestrutura no Docker

Para executar todos os serviços no Docker:

```powershell
docker compose --profile full up -d --build
```

A sequência de dependências é:

1. `db` inicia e fica saudável.
2. `db-init` cria ou valida o banco e termina com sucesso.
3. `api` pode iniciar; em `Development`, aplica migrations pendentes.

A API estará acessível em `http://localhost:8080` (ou na porta configurada em `API_PORT`). A rota `/` retorna HTTP 404 enquanto não houver um endpoint raiz; isso é esperado.

O serviço `api` utiliza o profile `full`; `db` e `db-init` participam da execução padrão.

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

Para iniciar novamente somente a infraestrutura, incluindo o provisionamento do banco:

```powershell
docker compose up -d
```

**Não utilize `down -v` como procedimento de correção de collation em bancos com dados importantes.**

## Provisionamento do SQL Server

### Objetivo e configuração

O banco `Roadmaps` deve utilizar a collation `Latin1_General_100_CI_AI`:

- `CI`: comparações sem distinção entre letras maiúsculas e minúsculas (*case-insensitive*).
- `AI`: comparações sem distinção de acentos (*accent-insensitive*).

O script versionado em `docker/sqlserver/init/01-create-database.sql` é responsável por **criar ou validar o banco**. Ele é executado conectado ao banco de sistema `master` e segue estas regras:

| Estado do banco `Roadmaps` | Comportamento |
|---|---|
| Não existe | Cria o banco com `Latin1_General_100_CI_AI` |
| Existe com a collation correta | Conclui sem alterar o banco |
| Existe com outra collation | Interrompe com erro `50001` |

O script **não altera automaticamente** a collation de um banco existente. Uma divergência deve ser investigada e corrigida em procedimento separado, com avaliação de impacto, backup e janela de manutenção quando necessário. Alterações de collation podem afetar objetos existentes, índices e comparações.

O script é seguro para reexecução quando a configuração já está correta. Sua execução deve ser coordenada para evitar provisionamentos concorrentes do mesmo banco.

### Development: provisionamento automático via Compose

No ambiente local, o serviço `db-init` executa o script usando `sqlcmd` após o SQL Server ficar saudável. A execução utiliza o usuário `sa` **somente para a infraestrutura de desenvolvimento**.

O comando `sqlcmd` utiliza `-b`, fazendo com que erros SQL retornem um código de saída diferente de zero. A API, quando executada pelo profile `full`, depende de `db-init` com `condition: service_completed_successfully`. Assim, uma falha no provisionamento impede que a API seja iniciada pelo fluxo normal do Compose.

Para conferir os logs:

```powershell
docker compose logs db-init
```

Para conferir a collation do banco, execute a consulta em seu cliente SQL:

```sql
SELECT name, collation_name
FROM sys.databases
WHERE name = N'Roadmaps';
```

O valor esperado de `collation_name` é `Latin1_General_100_CI_AI`.

### Staging e Production: provisionamento controlado

O serviço `db-init` do Compose foi preparado para **desenvolvimento local**. Não presuma que ele deva ser executado da mesma forma em Staging ou Production.

Para cada ambiente compartilhado:

1. Prepare a instância SQL Server, as permissões e os segredos de conexão conforme as políticas do ambiente.
2. Execute `docker/sqlserver/init/01-create-database.sql` contra `master` por uma etapa explícita de provisionamento, com uma identidade autorizada a criar o banco quando necessário.
3. Confirme que a execução terminou com sucesso e que a collation de `Roadmaps` é `Latin1_General_100_CI_AI`.
4. Aplique as migrations da aplicação por um processo separado e controlado, preferencialmente usando scripts SQL revisados.
5. Somente depois libere a inicialização ou atualização da API.

O arquivo SQL contém separadores `GO`, portanto deve ser executado por uma ferramenta que os reconheça, como `sqlcmd` ou um editor SQL compatível. Para automação com `sqlcmd`, use `-b` e trate códigos de saída diferentes de zero como falha do deploy.

**Segurança:** não utilize o usuário `sa` da infraestrutura local como credencial padrão de Staging/Production. Prefira identidades específicas, permissões adequadas a cada etapa e segredos gerenciados pelo ambiente. Configure conexão TLS com validação de certificado; a opção `-C` usada nos comandos locais de `sqlcmd` não deve ser reproduzida indiscriminadamente em produção.

Se o banco já existir com collation incorreta, **interrompa o provisionamento**. Não tente forçar `ALTER DATABASE ... COLLATE` durante a inicialização da API ou dentro de uma migration comum. A correção deve ser planejada separadamente, especialmente se o banco contiver dados e objetos dependentes da collation.

### Provisionamento não é migration

O script `01-create-database.sql` configura a existência e a collation do **banco**. Já as migrations do Entity Framework Core gerenciam a evolução do **schema da aplicação**.

Essas etapas são complementares e devem ocorrer nesta ordem:

`Instância SQL Server disponível → banco criado/validado → migrations aplicadas → API iniciada`

A configuração `modelBuilder.UseCollation("Latin1_General_100_CI_AI")` define a collation do modelo EF Core, mas **não substitui** o provisionamento explícito da collation padrão do banco.

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

Com o SQL Server em execução, o banco provisionado e a connection string configurada:

```powershell
dotnet tool run dotnet-ef database update --project src/Roadmaps.Infrastructure --startup-project src/Roadmaps.Api
```

Para listar as migrations e consultar seu estado no banco:

```powershell
dotnet tool run dotnet-ef migrations list --project src/Roadmaps.Infrastructure --startup-project src/Roadmaps.Api
```

### Aplicação automática em Development

Durante a inicialização da API, quando o ambiente é `Development`, o `Program.cs` executa `Database.MigrateAsync()` para aplicar migrations pendentes.

Esse comportamento **não é executado automaticamente em Staging/Production**. A implantação nesses ambientes deve utilizar um processo controlado de atualização do banco.

### Gerar script SQL para Staging/Production

Para gerar um script idempotente, que verifica quais migrations já foram aplicadas:

```powershell
dotnet tool run dotnet-ef migrations script --idempotent --project src/Roadmaps.Infrastructure --startup-project src/Roadmaps.Api --output migration.sql
```

Revise o conteúdo de `migration.sql` e execute-o no ambiente de destino conforme o processo de deploy, **depois do provisionamento do banco**. O script é um artefato gerado sob demanda; os arquivos de migration em C# são a fonte versionada no Git.

### Migration inicial

A migration `InitialCreate` foi criada e aplicada ao SQL Server local. Como ainda não existem entidades de negócio mapeadas, seus métodos `Up` e `Down` estão vazios. Sua aplicação é registrada na tabela `__EFMigrationsHistory`.

Isso é esperado nesta etapa: as tabelas de negócio serão adicionadas em migrations posteriores.

## SQL Server e Full-Text Search

O ambiente utiliza SQL Server 2022 com Full-Text Search, por meio de uma imagem Docker personalizada.

O Dockerfile do banco está localizado em `docker/sqlserver/Dockerfile`.

Para verificar se o Full-Text Search está instalado, execute no SQL Server:

```sql
SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS IsFullTextInstalled;
```

O resultado esperado é `1`.

Os dados do SQL Server são armazenados em um volume nomeado do Docker, permitindo sua preservação entre reinicializações dos containers.
