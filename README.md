# Memória Digital

O objetivo é ajudar a organizar a avaliação de conteúdos suspeitos, apresentando indicadores encontrados, nível de risco e recomendações. Os resultados são baseados em regras definidas no projeto.

## Integrantes

| Integrante | RM |
|---|---|
| Heloísa Fleury Jardim | RM556378 |
| Juan Fuentes Rufino | RM557673 |
| Rickelmyn de Souza Ruescas | RM556055 |
| Paulo Henrique Monteiro Golovanevsky | RM555300 |
| Pedro Henrique Silva Batista | RM558137 |

## Funcionalidades

- Recebimento de entradas dos tipos `Message`, `Link` e `Image`.
- Validação dos dados recebidos, incluindo o formato de URLs.
- Análise determinística de mensagens e links por meio de regras e padrões conhecidos.
- Registro de indicadores encontrados, classificação de risco e recomendações.
- Registro de entradas de imagem, com status `Unsupported`, pois a análise de imagens não está implementada.
- Consulta de uma análise pelo identificador.
- Listagem paginada do histórico de análises.
- Persistência dos dados em SQLite com Entity Framework Core.
- Documentação dos endpoints por OpenAPI e Scalar.

## Como funciona a análise

A API aplica regras locais para procurar sinais comuns em mensagens e links. Entre os exemplos avaliados estão:

- Solicitações de senhas ou outros dados de acesso.
- Mensagens com urgência para realizar pagamentos ou transferências.
- Ameaças de bloqueio ou perda de acesso.
- Promessas de prêmios ou benefícios condicionados a alguma ação.
- Presença de URLs em mensagens.
- Links com endereço IP, domínio internacionalizado, encurtadores ou padrões de imitação de marcas conhecidas.

Quando uma regra é identificada, a análise pode registrar um indicador com descrição, severidade e evidência, além de recomendações relacionadas ao risco encontrado.

A classificação considera a severidade dos indicadores: baixa vale 1 ponto, média vale 3 e alta vale 5. A pontuação total define o nível de risco: de 0 a 2, baixo; de 3 a 5, médio; a partir de 6, alto. A presença de um único indicador de severidade alta também classifica o resultado como risco alto.

Uma entrada sem conteúdo alfanumérico pode resultar em `Inconclusive`. Entradas do tipo imagem são registradas como `Unsupported`. Mensagens e links que passam pela análise recebem o status `Completed`.

## Tecnologias

- C# e .NET 10
- ASP.NET Core Web API com Controllers
- Entity Framework Core 10
- SQLite
- Scalar e OpenAPI
- xUnit
- `WebApplicationFactory` e SQLite em memória para testes de integração

## Organização do projeto

```text
MemoriaDigital.slnx
src/
├── MemoriaDigital.Api/
├── MemoriaDigital.Application/
├── MemoriaDigital.Domain/
└── MemoriaDigital.Infrastructure/

tests/
├── MemoriaDigital.UnitTests/
└── MemoriaDigital.IntegrationTests/
```

- **MemoriaDigital.Domain:** entidades, enums, validações e regras de negócio, incluindo o ciclo de vida das análises e a classificação de risco.
- **MemoriaDigital.Application:** serviços, casos de uso, DTOs e interfaces que conectam a API ao domínio e à persistência. Também contém o mecanismo determinístico de análise.
- **MemoriaDigital.Infrastructure:** contexto do Entity Framework Core, configurações de mapeamento, repositórios e migrations do banco de dados.
- **MemoriaDigital.Api:** controllers, configuração de dependências, banco de dados, tratamento global de exceções e documentação OpenAPI/Scalar.
- **MemoriaDigital.UnitTests:** testes das regras do domínio e do mecanismo de análise.
- **MemoriaDigital.IntegrationTests:** testes HTTP da API e de persistência, utilizando banco SQLite em memória.

Uma análise possui uma entrada e pode conter vários indicadores e recomendações. O domínio controla as transições de estado e impede alterações após a finalização da análise.

## Pré-requisitos

- .NET SDK compatível com a versão definida em `global.json`.
- Acesso ao NuGet para restaurar os pacotes.
- Ferramenta `dotnet-ef` 10.0.14 para executar comandos de migration.

Para conferir a versão do SDK:

```bash
dotnet --version
```

Caso ainda não tenha a ferramenta do Entity Framework instalada:

```bash
dotnet tool install --global dotnet-ef --version 10.0.14
```

## Executar a aplicação

Na pasta raiz do repositório, restaure as dependências e compile a solução:

```bash
dotnet restore
dotnet build
```

A connection string padrão está definida em `src/MemoriaDigital.Api/appsettings.json` e utiliza o arquivo `memoria-digital.db`.

Para iniciar a API:

```bash
dotnet run --project src/MemoriaDigital.Api
```

Os perfis de execução e as portas locais estão definidos em `src/MemoriaDigital.Api/Properties/launchSettings.json`.

## Banco de dados e migrations

O banco utiliza SQLite e Entity Framework Core. Para aplicar as migrations manualmente, execute na raiz da solução:

```bash
dotnet ef database update --project src/MemoriaDigital.Infrastructure --startup-project src/MemoriaDigital.Api
```

As migrations ficam em `src/MemoriaDigital.Infrastructure/Persistence/Migrations`.

## Endpoints

A API disponibiliza os seguintes endpoints:

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/analyses` | Cria e executa uma análise para a entrada informada. |
| `GET` | `/api/analyses/{id}` | Consulta uma análise pelo identificador. |
| `GET` | `/api/analyses?page=1&pageSize=10` | Lista análises com paginação. |

### Criar análise

`POST /api/analyses`

Exemplo de requisição para uma mensagem:

```json
{
  "inputType": "Message",
  "content": "Sua conta será bloqueada. Acesse o link para regularizar seu cadastro."
}
```

Para uma entrada do tipo `Link`, informe uma URL absoluta válida no campo `content`:

```json
{
  "inputType": "Link",
  "content": "https://exemplo.com/atualizacao"
}
```

A resposta de criação utiliza `201 Created` e apresenta os dados da análise, incluindo status, nível de risco quando aplicável, entrada, indicadores e recomendações.

### Consultar análise

`GET /api/analyses/{id}`

- `200 OK`: análise encontrada.
- `404 Not Found`: identificador não encontrado.

### Listar análises

`GET /api/analyses?page=1&pageSize=10`

- `page`: número da página, a partir de 1.
- `pageSize`: quantidade de registros por página, entre 1 e 100.
- `200 OK`: retorna os registros e os dados da paginação.
- `400 Bad Request`: parâmetros de paginação inválidos.

A listagem é ordenada pela data de criação, da mais recente para a mais antiga. Em caso de empate, o identificador é utilizado como critério secundário.

## Documentação da API

Com a aplicação em execução no ambiente de desenvolvimento, acesse:

```text
http://localhost:<porta>/scalar/v1
```

## Testes

Para executar todos os testes:

```bash
dotnet test
```

Para executar apenas os testes unitários:

```bash
dotnet test tests/MemoriaDigital.UnitTests
```

Para executar apenas os testes de integração:

```bash
dotnet test tests/MemoriaDigital.IntegrationTests
```

## Limitações

- A análise de imagens ainda não está implementada; esses registros recebem o status `Unsupported`.
- A análise de mensagens e links utiliza regras determinísticas, sem IA.
