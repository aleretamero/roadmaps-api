# Roadmaps API

API desenvolvida em C# e ASP.NET Core para gerenciamento de roadmaps de estudos, tópicos e acompanhamento de progresso.

## Tecnologias

- .NET 10
- C#
- ASP.NET Core Web API
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

- SDK .NET 10 instalado.
- O arquivo `global.json` define a versão-base `10.0.403` do SDK, permitindo atualizações de patch compatíveis dentro da mesma feature band.

## Desenvolvimento

Para restaurar as dependências:

```powershell
dotnet restore
```

Para compilar a solução:

```powershell
dotnet build
```

Para executar os testes:

```powershell
dotnet test
```

Para iniciar a API:

```powershell
dotnet run --project src/Roadmaps.Api
```
