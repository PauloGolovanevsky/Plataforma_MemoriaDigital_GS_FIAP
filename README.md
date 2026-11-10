# Memória Digital

API REST acadêmica desenvolvida para o projeto da FIAP. A aplicação organiza o recebimento e a persistência de entradas que podem conter mensagens, links ou imagens, preparando a análise de possíveis golpes digitais.

O projeto demonstra Programação Orientada a Objetos, separação em camadas, injeção de dependência, Entity Framework Core, SQLite, programação assíncrona, API REST e testes automatizados.

## Objetivo e problema de negócio

Golpes digitais frequentemente utilizam mensagens persuasivas, links suspeitos e outros conteúdos para induzir o usuário a fornecer dados, realizar pagamentos ou acessar páginas não confiáveis. O objetivo do Memória Digital é fornecer uma API capaz de receber essas entradas, registrar o histórico da análise e disponibilizar resultados estruturados quando houver um mecanismo de análise implementado.

Nesta versão, a API já suporta o cadastro, a consulta e a listagem paginada de análises. A criação registra a análise no estado `Processing`; ainda não existe um analisador que conclua automaticamente o processamento.

## Funcionalidades implementadas

- Criação de análises com uma única entrada.
- Entradas do tipo `Message`, `Link` ou `Image`.
- Validação de mensagens e URLs.
- Validação de referência ou metadados mínimos para imagens.
- Persistência em SQLite com Entity Framework Core.
- Migration inicial.
- Consulta de análise por identificador.
- Listagem paginada de análises.
- Ordenação da paginação por `CreatedAt` decrescente e `Id` como critério secundário.
- DTOs para entrada e saída da API.
- Serialização dos enums como strings JSON, como `Processing` e `Message`.
- Documentação visual com Scalar/OpenAPI.
- Testes unitários do domínio.
- Testes de integração HTTP com SQLite em memória.

## Limitações atuais

- A análise criada permanece em `Processing`.
- Não há mecanismo de processamento que conclua uma análise.
- Não há analisadores de mensagem, link ou imagem implementados na API.
- Imagens não são analisadas; a entrada é apenas validada e registrada.
- Não existe integração real com inteligência artificial.
- Não há autenticação ou autorização.
- Não há frontend.
- Não há afirmação de fraude ou detecção infalível. O domínio prevê classificação interna de risco, mas os endpoints atuais ainda não produzem uma classificação final.

## Tecnologias e ferramentas

- .NET SDK `10.0.404`, fixado em `global.json`.
- .NET `net10.0`.
- ASP.NET Core Web API com Controllers.
- Entity Framework Core `10.0.14`.
- SQLite.
- Scalar.AspNetCore `2.6.0`.
- Microsoft.AspNetCore.OpenApi `10.0.15`.
- xUnit `2.9.3`.
- Microsoft.AspNetCore.Mvc.Testing `10.0.14`.
- Microsoft.Data.Sqlite `10.0.14` para os testes de integração.

## Arquitetura

```text
src/
├── MemoriaDigital.Api
├── MemoriaDigital.Application
├── MemoriaDigital.Domain
└── MemoriaDigital.Infrastructure

tests/
├── MemoriaDigital.UnitTests
└── MemoriaDigital.IntegrationTests
```

### `MemoriaDigital.Domain`

Contém as entidades, enums e regras centrais do domínio:

- `Analysis`
- `AnalysisInput`
- `Indicator`
- `Recommendation`
- Estados, tipos de entrada, níveis de risco, severidades e prioridades.

As entidades encapsulam suas coleções e controlam as transições de estado e a classificação determinística prevista para o domínio.

### `MemoriaDigital.Application`

Contém os casos de uso, DTOs e contratos de persistência:

- Criação de análise.
- Consulta por ID.
- Listagem paginada.
- `IAnalysisRepository`.
- `IAnalysisService`.

Essa camada não expõe diretamente as entidades do domínio para a API.

### `MemoriaDigital.Infrastructure`

Contém a persistência:

- `ApplicationDbContext`.
- Configurações do EF Core.
- Repositório de análises.
- Migration inicial.
- Relacionamentos entre análise, entrada, indicadores e recomendações.

### `MemoriaDigital.Api`

Contém:

- `AnalysisController`.
- Configuração de DI.
- Configuração do SQLite.
- Serialização JSON dos enums.
- Scalar/OpenAPI.

### Projetos de teste

`MemoriaDigital.UnitTests` testa principalmente entidades e regras determinísticas do domínio.

`MemoriaDigital.IntegrationTests` testa a API via `WebApplicationFactory`, usando uma conexão SQLite em memória isolada por teste.

## Estrutura de pastas

```text
MemoriaDigital.slnx
global.json
README.md

src/
├── MemoriaDigital.Api/
│   ├── Controllers/AnalysisController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Properties/launchSettings.json
├── MemoriaDigital.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
├── MemoriaDigital.Domain/
│   ├── Entities/
│   └── Enums/
└── MemoriaDigital.Infrastructure/
    └── Persistence/
        ├── Configurations/
        ├── Migrations/
        └── Repositories/

tests/
├── MemoriaDigital.UnitTests/
└── MemoriaDigital.IntegrationTests/
```

## Pré-requisitos

- Windows, Linux ou macOS com suporte ao .NET 10.
- .NET SDK `10.0.404` ou uma versão compatível com o `global.json`.
- Acesso ao NuGet para restaurar as dependências.
- A ferramenta `dotnet-ef` `10.0.14` para criar ou aplicar migrations.

Verifique o SDK com:

```powershell
dotnet --version
```

Caso necessário, instale a ferramenta EF Core:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.14
```

## Configuração e execução da API

Na raiz do repositório:

```powershell
dotnet restore
dotnet build
```

A connection string padrão está em `src/MemoriaDigital.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=memoria-digital.db"
  }
}
```

Inicie a API com:

```powershell
dotnet run --project src/MemoriaDigital.Api
```

Os perfis de execução estão em `src/MemoriaDigital.Api/Properties/launchSettings.json`.

## SQLite e migrations

Para criar a migration inicial, caso necessário:

```powershell
dotnet ef migrations add InitialCreate `
  --project src/MemoriaDigital.Infrastructure `
  --startup-project src/MemoriaDigital.Api `
  --output-dir Persistence/Migrations
```

Para criar ou atualizar o banco:

```powershell
dotnet ef database update `
  --project src/MemoriaDigital.Infrastructure `
  --startup-project src/MemoriaDigital.Api
```

A migration atualmente versionada é `InitialCreate`, localizada em `src/MemoriaDigital.Infrastructure/Persistence/Migrations`.

## Testes

Execute todos os testes:

```powershell
dotnet test
```

Somente testes unitários:

```powershell
dotnet test tests/MemoriaDigital.UnitTests
```

Somente testes de integração:

```powershell
dotnet test tests/MemoriaDigital.IntegrationTests
```

Os testes de integração não utilizam o arquivo SQLite de desenvolvimento. Cada fábrica cria uma conexão SQLite em memória e um banco isolado.

## API REST

Os enums enviados e retornados pela API utilizam seus nomes em formato de string.

### Criar análise

```http
POST /api/analyses
Content-Type: application/json
```

Requisição:

```json
{
  "inputType": "Message",
  "content": "Mensagem de teste."
}
```

Resposta: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "createdAt": "2026-01-01T12:00:00+00:00",
  "completedAt": null,
  "status": "Processing",
  "riskLevel": null,
  "input": {
    "id": "00000000-0000-0000-0000-000000000000",
    "type": "Message",
    "content": "Mensagem de teste.",
    "reference": null,
    "fileName": null,
    "mediaType": null
  },
  "indicators": [],
  "recommendations": []
}
```

A análise permanece em `Processing` até existir um mecanismo de processamento.

### Consultar análise por ID

```http
GET /api/analyses/{id}
```

Respostas:

- `200 OK` quando a análise existe.
- `404 Not Found` quando o ID não está registrado.

Exemplo:

```text
GET /api/analyses/00000000-0000-0000-0000-000000000000
```

### Listar análises com paginação

```http
GET /api/analyses?page=1&pageSize=10
```

Parâmetros:

- `page`: número da página, maior ou igual a 1.
- `pageSize`: quantidade de registros, entre 1 e 100.

Resposta: `200 OK`

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0
}
```

Parâmetros inválidos retornam `400 Bad Request`. A listagem é ordenada por `CreatedAt` decrescente e, em caso de empate, por `Id` decrescente.

## Scalar/OpenAPI

Em ambiente de desenvolvimento, a API expõe a especificação OpenAPI e a interface visual Scalar.

Com a aplicação em execução, acesse:

```text
http://localhost:<porta>/scalar/v1
```

O Scalar permite visualizar a documentação e testar os endpoints disponíveis.

## Resumo dos testes

O projeto possui:

- 25 casos de teste unitário para entidades e regras do domínio.
- 12 casos de teste de integração HTTP para criação, consulta, paginação, validações, serialização dos enums e isolamento do banco.

Os testes de integração utilizam SQLite em memória e não alteram o banco SQLite usado pela API.
