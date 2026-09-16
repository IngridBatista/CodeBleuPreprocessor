# CodeBleuPreprocessor

API em **ASP.NET Core (.NET 10)** que prepara arquivos de código C# para o cálculo de **CodeBLEU**, normalizando-os em um único dataset de texto plano. Faz parte da infraestrutura de um Trabalho de Conclusão de Curso (TCC) que avalia a qualidade do código C# gerado por LLMs, consumindo as soluções produzidas pelo [AIConnection](https://github.com/IngridBatista/AIConnection) e armazenadas em repositórios como o [GeneratedCodeByAI](https://github.com/IngridBatista/GeneratedCodeByAI).

## O que o projeto faz

O CodeBLEU compara a similaridade estrutural/sintática entre o código gerado por um modelo e uma solução de referência. Para que essa comparação seja justa, é preciso eliminar diferenças que não têm relação com a lógica do código (comentários, `using`s, indentação, quebras de linha, wrapper de `namespace`). É exatamente essa normalização que este projeto automatiza.

A API expõe um único endpoint que:

1. Varre recursivamente uma pasta informada em busca de todos os arquivos `.cs`, ignorando artefatos de build/gerados automaticamente (`*.Designer.cs`, `*.g.cs`, `*.generated.cs`, `AssemblyInfo.cs`).
2. Ordena os arquivos encontrados de forma determinística (por caminho).
3. Para cada arquivo, faz o parsing do código com o **Roslyn** (`Microsoft.CodeAnalysis.CSharp`) e aplica as seguintes transformações:
   - Remove todos os comentários (linha única, múltiplas linhas e comentários de documentação `///`/`/** */`).
   - Remove todas as diretivas `using`.
   - "Desembrulha" o conteúdo de dentro de um `namespace` (tradicional ou file-scoped), deixando apenas os membros da classe quando o arquivo tem um único namespace de nível superior.
   - Normaliza todo o espaçamento e as quebras de linha, produzindo o código em uma única linha contínua, sem indentação.
4. Grava cada resultado como uma linha em um arquivo de saída (`outputFile`), gerando um dataset onde cada linha corresponde a um arquivo `.cs` processado, formato pronto para ser consumido por ferramentas de cálculo de CodeBLEU.

## Endpoint

```
POST /CodeBleuPreprocessor/geracao-dataset?sourceFolder={caminho}&outputFile={caminho}
```

| Parâmetro | Descrição |
|---|---|
| `sourceFolder` | Pasta raiz onde os arquivos `.cs` serão buscados recursivamente (ex.: a pasta do repositório `GeneratedCodeByAI` ou de referências de especialista) |
| `outputFile` | Caminho do arquivo de texto de saída, um "código normalizado" por linha, em UTF-8 sem BOM |

Retorna `200 OK` com uma mensagem de confirmação em caso de sucesso, ou `500` com os detalhes do erro em caso de falha (ex.: pasta inexistente).

## Exemplo de transformação

Entrada (`ClaudeArrayDifferenceSeniorParticipant1.cs`):

```csharp
namespace CLAUDE.ARRAY_DIFFERENCE.SENIOR.PARTICIPANT_1
{
    public class ClaudeArrayDifferenceSeniorParticipant1
    {
        public static int[] ObterElementosExclusivos(int[] array1, int[] array2)
        {
            // remove comentário
            return array1.Except(array2).ToArray();
        }
    }
}
```

Saída (uma linha no dataset, sem comentários, sem `namespace`, sem `using`, sem indentação):

```
public class ClaudeArrayDifferenceSeniorParticipant1 { public static int[] ObterElementosExclusivos(int[] array1, int[] array2) { return array1.Except(array2).ToArray(); } }
```

## Stack técnica

- **.NET 10** / ASP.NET Core Web API
- `Microsoft.CodeAnalysis` (Roslyn)
- Swagger / OpenAPI habilitado para exploração e teste do endpoint

## Estrutura do projeto

```
CodeBleuPreprocessor/
├── Controllers/
│   └── CodeBleuPreprocessorController.cs   # Endpoint POST /geracao-dataset
├── Service/
│   └── CodeBleuPreprocessorService.cs      # Lógica de normalização via Roslyn
├── CodeBleuPreprocessor.http               # Requisições de exemplo
└── Program.cs                              # Configuração do pipeline HTTP / Swagger
```

## Contexto

Este repositório é uma etapa intermediária do pipeline de avaliação do TCC: transforma o código bruto gerado pelos LLMs (armazenado no [GeneratedCodeByAI](https://github.com/IngridBatista/GeneratedCodeByAI)) em um dataset normalizado, que é então usado no cálculo das métricas de similaridade CodeBLEU (AST Match, Dataflow Match, N-gram Match e Weighted N-gram Match) contra as soluções de referência.
