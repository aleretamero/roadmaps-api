# Roadmaps API

API desenvolvida em C# e ASP.NET Core para gerenciamento de roadmaps de estudos, tópicos e acompanhamento de progresso.

## Tecnologias

- .NET 10
- C#
- ASP.NET Core Web API
- SQL Server 2022 com Full-Text Search
- Docker e Docker Compose
- xUnit para testes automatizados

## Estrutura da solução

A solução está organizada em projetos com responsabilidades distintas:

| Projeto | Responsabilidade |
|---|---|
| `Roadmaps.Domain` | Entidades e regras de negócio |
| `Roadmaps.Application` | Casos de uso e lógica de aplicação |
| `Roadmaps.Infrastructure` | Implementações de infraestrutura e integrações externas |
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

Substitua `SUA_SENHA` pela senha configurada no `.env`.

Os segredos são armazenados fora do repositório. O projeto já possui um `UserSecretsId` configurado.

A API ainda não utiliza a conexão com o banco de dados. A integração será implementada na etapa de persistência.

## Desenvolvimento

### Executar infraestrutura no Docker e API localmente

Este é o fluxo recomendado para desenvolvimento.

Inicie a infraestrutura:

```powershell
docker compose up -d
```

O comando inicia somente o SQL Server.

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

A API estará acessível em:

`http://localhost:8080`

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

Executar os testes automatizados:

```powershell
dotnet test
```

Executar a API sem Hot Reload:

```powershell
dotnet run --project src/Roadmaps.Api
```

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
