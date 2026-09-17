# CodeBleuPreprocessor

API em **ASP.NET Core (.NET 10)** que prepara arquivos de código C# para o cálculo de **CodeBLEU**, normalizando-os em um único dataset de texto plano, mais os datasets já normalizados e o script Python que executa o cálculo final. É a etapa de pré-processamento e cálculo de métricas de um Trabalho de Conclusão de Curso (TCC) que avalia a qualidade do código C# gerado por LLMs, consumindo as soluções produzidas pelo [AIConnection](https://github.com/IngridBatista/AIConnection) e armazenadas no [GeneratedCodeByAI](https://github.com/IngridBatista/GeneratedCodeByAI).

## O que o repositório contém

1. **API de normalização** (`CodeBleuPreprocessor/`) — projeto ASP.NET Core que varre pastas de código `.cs` e gera datasets normalizados (sem comentários, sem `using`s, sem indentação), prontos para comparação via CodeBLEU.
2. **Datasets normalizados** (`NormalizedGeneratedCode/` e `NormalizedReferenceCode/`) — a saída já gerada pela API acima, versionada no repositório.
3. **Script de cálculo** (`run_codebleu.py`) — script Python que lê um arquivo do código gerado e um arquivo de referência e calcula as métricas de CodeBLEU entre eles.
4. **Resultados consolidados** — a tabela ao final deste README, com o resultado de cada uma das 84 execuções do CodeBLEU (4 modelos × 3 questões × 7 participantes).

## API de normalização

A API expõe um único endpoint que:

1. Varre recursivamente uma pasta informada em busca de todos os arquivos `.cs`, ignorando artefatos de build/gerados automaticamente (`*.Designer.cs`, `*.g.cs`, `*.generated.cs`, `AssemblyInfo.cs`).
2. Ordena os arquivos encontrados de forma determinística (por caminho).
3. Para cada arquivo, faz o parsing do código com o **Roslyn** (`Microsoft.CodeAnalysis.CSharp`) e aplica as seguintes transformações:
   - Remove todos os comentários (linha única, múltiplas linhas e comentários de documentação `///`/`/** */`).
   - Remove todas as diretivas `using`.
   - "Desembrulha" o conteúdo de dentro de um `namespace` (tradicional ou file-scoped), deixando apenas os membros da classe quando o arquivo tem um único namespace de nível superior.
   - Normaliza todo o espaçamento e as quebras de linha, produzindo o código em uma única linha contínua, sem indentação.
4. Grava cada resultado como uma linha em um arquivo de saída (`outputFile`).

### Endpoint

```
POST /CodeBleuPreprocessor/geracao-dataset?sourceFolder={caminho}&outputFile={caminho}
```

| Parâmetro | Descrição |
|---|---|
| `sourceFolder` | Pasta raiz onde os arquivos `.cs` serão buscados recursivamente |
| `outputFile` | Caminho do arquivo de texto de saída, um "código normalizado" por linha, em UTF-8 sem BOM |

Retorna `200 OK` com uma mensagem de confirmação em caso de sucesso, ou `500` com os detalhes do erro em caso de falha.

### Exemplo de transformação

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

## Datasets normalizados

Os datasets abaixo já foram gerados pela API e estão versionados no repositório:

```
NormalizedGeneratedCode/
├── CLAUDE/
│   ├── ArrayDifference/Participant_1/GeneratedCodeClaudeArrayDifferenceParticipant1.txt
│   ├── ...Participant_2 ... Participant_7
│   ├── SequenceComparison/Participant_.../...
│   └── StringArrayEncoding/Participant_.../...
├── DEEPSEEK/  (mesma estrutura)
├── GEMINI/    (mesma estrutura)
└── GPT/       (mesma estrutura)

NormalizedReferenceCode/
├── ArrayDifference/ReferenceCodeArrayDifference.txt
├── SequenceComparison/ReferenceCodeSequenceComparison.txt
└── StringArrayEncoding/ReferenceCodeStringArrayEncoding.txt
```

- `NormalizedGeneratedCode/{MODELO}/{QUESTAO}/Participant_{N}/` — código normalizado gerado por cada LLM, para cada questão e participante (saída do [GeneratedCodeByAI](https://github.com/IngridBatista/GeneratedCodeByAI) já processada por esta API).
- `NormalizedReferenceCode/{QUESTAO}/` — código normalizado da solução de referência (especialista) de cada uma das 3 questões, usado como base de comparação para todos os modelos e participante ([GeneratedCodeBySpecialist](https://github.com/reginaldomota/code-metrics)).

> `CLAUDE/SequenceComparison` e `DEEPSEEK/SequenceComparison` não possuem a pasta `Participant_3`: a geração de código para esse par não produziu uma solução compilável, por isso não há CodeBLEU calculado para essas duas combinações (ver tabela de resultados).

## Script de cálculo (`run_codebleu.py`)

O script usa a biblioteca [`codebleu`](https://pypi.org/project/codebleu/) para calcular as métricas entre um arquivo de referência e um arquivo de código gerado.

### Pré-requisitos

- Python 3.8 ou superior
- `pip`

### Passo a passo de instalação

1. Clone o repositório e entre na pasta:
   ```bash
   git clone https://github.com/IngridBatista/CodeBleuPreprocessor.git
   cd CodeBleuPreprocessor
   ```
2. (Recomendado) Crie e ative um ambiente virtual:
   ```bash
   python -m venv venv
   # Windows
   venv\Scripts\activate
   # Linux/macOS
   source venv/bin/activate
   ```
3. Instale a biblioteca `codebleu`:
   ```bash
   pip install codebleu
   ```

### Passo a passo de execução

O script **não recebe parâmetros por linha de comando**, os caminhos do arquivo de referência (`ref_path`) e do arquivo gerado a ser comparado (`hyp_path`) estão fixos diretamente no código-fonte. Isso significa que, para cada execução, é necessário editar manualmente essas duas linhas antes de rodar o script:

```python
ref_path = base_dir / "NormalizedReferenceCode" / "ArrayDifference" / "ReferenceCodeArrayDifference.txt"
hyp_path = base_dir / "NormalizedGeneratedCode" / "GEMINI" / "ArrayDifference" / "Participant_4" / "GeneratedCodeGeminiArrayDifferenceParticipant4.txt"
```

1. Abra `run_codebleu.py` em um editor de texto.
2. Ajuste `ref_path` para a questão que deseja avaliar (`ArrayDifference`, `SequenceComparison` ou `StringArrayEncoding`).
3. Ajuste `hyp_path` para o modelo, a questão e o participante desejados (`{MODELO}/{QUESTAO}/Participant_{N}/...`).
4. Salve o arquivo e execute:
   ```bash
   python run_codebleu.py
   ```
5. O script imprime no terminal as 5 métricas da combinação avaliada:
   ```
   CodeBLEU: 0.xxxxxxxx
   N-gram: 0.xxxxxxxx
   Weighted N-gram: 0.xxxxxxxx
   AST Match: 0.xxxxxxxx
   Dataflow Match: 0.xxxxxxxx
   ```
6. Repita os passos 2 a 5 para cada uma das 84 combinações de modelo/questão/participante, anotando o resultado impresso a cada execução.

Foi assim que a tabela de resultados abaixo foi construída: 84 execuções manuais do script, uma por combinação, com os valores impressos no terminal copiados para a planilha de resultados.

## Resultados do CodeBLEU

Tabela completa com o resultado das 84 execuções (uma por combinação de modelo × questão × participante). As duas combinações sem código gerado válido (`Claude`/`SequenceComparison`/Participante 3 e `DeepSeek`/`SequenceComparison`/Participante 3) aparecem marcadas com "—".

| Modelo | Questão | Participante | CodeBLEU | N-gram | Weighted N-gram | AST Match | Dataflow Match |
|---|---|---|---|---|---|---|---|
| Claude | ArrayDifference | Participante 1 | 0.1042 | 0.0009 | 0.0124 | 0.3481 | 0.0556 |
| Claude | ArrayDifference | Participante 2 | 0.3564 | 0.0008 | 0.0024 | 0.4222 | 0.0000 |
| Claude | ArrayDifference | Participante 3 | 0.1889 | 0.0332 | 0.1566 | 0.4407 | 0.1250 |
| Claude | ArrayDifference | Participante 4 | 0.1142 | 0.0002 | 0.0491 | 0.3519 | 0.0556 |
| Claude | ArrayDifference | Participante 5 | 0.1662 | 0.0196 | 0.0942 | 0.4259 | 0.1250 |
| Claude | ArrayDifference | Participante 6 | 0.1761 | 0.0240 | 0.1146 | 0.4407 | 0.1250 |
| Claude | ArrayDifference | Participante 7 | 0.1699 | 0.0316 | 0.0648 | 0.4444 | 0.1389 |
| DeepSeek | ArrayDifference | Participante 1 | 0.2563 | 0.0503 | 0.0535 | 0.5185 | 0.4028 |
| DeepSeek | ArrayDifference | Participante 2 | 0.3414 | 0.0034 | 0.0066 | 0.3556 | 0.0000 |
| DeepSeek | ArrayDifference | Participante 3 | 0.1889 | 0.0332 | 0.1566 | 0.4407 | 0.1250 |
| DeepSeek | ArrayDifference | Participante 4 | 0.1889 | 0.0332 | 0.1566 | 0.4407 | 0.1250 |
| DeepSeek | ArrayDifference | Participante 5 | 0.1648 | 0.0160 | 0.0776 | 0.4407 | 0.1250 |
| DeepSeek | ArrayDifference | Participante 6 | 0.2013 | 0.0925 | 0.1101 | 0.4778 | 0.1250 |
| DeepSeek | ArrayDifference | Participante 7 | 0.3454 | 0.0617 | 0.1153 | 0.5519 | 0.6528 |
| Gemini | ArrayDifference | Participante 1 | 0.1307 | 0.0084 | 0.0144 | 0.4444 | 0.0556 |
| Gemini | ArrayDifference | Participante 2 | 0.2655 | 0.0979 | 0.1012 | 0.5296 | 0.3333 |
| Gemini | ArrayDifference | Participante 3 | 0.1971 | 0.0594 | 0.1494 | 0.4407 | 0.1389 |
| Gemini | ArrayDifference | Participante 4 | 0.1131 | 0.0001 | 0.0487 | 0.3481 | 0.0556 |
| Gemini | ArrayDifference | Participante 5 | 0.1131 | 0.0001 | 0.0487 | 0.3481 | 0.0556 |
| Gemini | ArrayDifference | Participante 6 | 0.1399 | 0.0221 | 0.0377 | 0.4444 | 0.0556 |
| Gemini | ArrayDifference | Participante 7 | 0.1831 | 0.0620 | 0.0702 | 0.4333 | 0.1667 |
| GPT | ArrayDifference | Participante 1 | 0.1308 | 0.0024 | 0.0135 | 0.3963 | 0.1111 |
| GPT | ArrayDifference | Participante 2 | 0.1992 | 0.0070 | 0.0082 | 0.5037 | 0.2778 |
| GPT | ArrayDifference | Participante 3 | 0.1620 | 0.0137 | 0.0796 | 0.4296 | 0.1250 |
| GPT | ArrayDifference | Participante 4 | 0.1197 | 0.0012 | 0.0554 | 0.3667 | 0.0556 |
| GPT | ArrayDifference | Participante 5 | 0.1759 | 0.0293 | 0.0798 | 0.4556 | 0.1389 |
| GPT | ArrayDifference | Participante 6 | 0.1558 | 0.0123 | 0.0601 | 0.4259 | 0.1250 |
| GPT | ArrayDifference | Participante 7 | 0.1719 | 0.0218 | 0.0964 | 0.4444 | 0.1250 |
| Claude | SequenceComparison | Participante 1 | 0.2626 | 0.0562 | 0.1024 | 0.5089 | 0.3830 |
| Claude | SequenceComparison | Participante 2 | 0.2905 | 0.1555 | 0.1589 | 0.5071 | 0.3404 |
| Claude | SequenceComparison | Participante 3 | — | — | — | — | — |
| Claude | SequenceComparison | Participante 4 | 0.3580 | 0.2485 | 0.2595 | 0.5464 | 0.3777 |
| Claude | SequenceComparison | Participante 5 | 0.3060 | 0.1514 | 0.1594 | 0.5250 | 0.3883 |
| Claude | SequenceComparison | Participante 6 | 0.3412 | 0.2333 | 0.2414 | 0.5286 | 0.3617 |
| Claude | SequenceComparison | Participante 7 | 0.3597 | 0.1768 | 0.2294 | 0.6125 | 0.4202 |
| DeepSeek | SequenceComparison | Participante 1 | 0.2606 | 0.0743 | 0.1203 | 0.5339 | 0.3138 |
| DeepSeek | SequenceComparison | Participante 2 | 0.2783 | 0.1772 | 0.1859 | 0.5054 | 0.2447 |
| DeepSeek | SequenceComparison | Participante 3 | — | — | — | — | — |
| DeepSeek | SequenceComparison | Participante 4 | 0.3443 | 0.1953 | 0.2012 | 0.5446 | 0.4362 |
| DeepSeek | SequenceComparison | Participante 5 | 0.3053 | 0.1510 | 0.1585 | 0.5179 | 0.3936 |
| DeepSeek | SequenceComparison | Participante 6 | 0.2884 | 0.1836 | 0.1917 | 0.4750 | 0.3032 |
| DeepSeek | SequenceComparison | Participante 7 | 0.3456 | 0.1582 | 0.2381 | 0.5339 | 0.4521 |
| Gemini | SequenceComparison | Participante 1 | 0.2662 | 0.0768 | 0.1082 | 0.5339 | 0.3457 |
| Gemini | SequenceComparison | Participante 2 | 0.2722 | 0.1569 | 0.1623 | 0.5089 | 0.2606 |
| Gemini | SequenceComparison | Participante 3 | 0.2472 | 0.1183 | 0.1295 | 0.4964 | 0.2447 |
| Gemini | SequenceComparison | Participante 4 | 0.2579 | 0.1143 | 0.1176 | 0.4964 | 0.3032 |
| Gemini | SequenceComparison | Participante 5 | 0.2981 | 0.1634 | 0.1833 | 0.5214 | 0.3245 |
| Gemini | SequenceComparison | Participante 6 | 0.2942 | 0.1360 | 0.1932 | 0.5286 | 0.3191 |
| Gemini | SequenceComparison | Participante 7 | 0.3044 | 0.1238 | 0.1608 | 0.5393 | 0.3936 |
| GPT | SequenceComparison | Participante 1 | 0.2659 | 0.0738 | 0.0997 | 0.5179 | 0.3723 |
| GPT | SequenceComparison | Participante 2 | 0.2840 | 0.1498 | 0.1616 | 0.5214 | 0.3032 |
| GPT | SequenceComparison | Participante 3 | 0.2257 | 0.0817 | 0.1014 | 0.4804 | 0.2394 |
| GPT | SequenceComparison | Participante 4 | 0.2903 | 0.1300 | 0.1443 | 0.5518 | 0.3351 |
| GPT | SequenceComparison | Participante 5 | 0.3017 | 0.1083 | 0.1426 | 0.5357 | 0.4202 |
| GPT | SequenceComparison | Participante 6 | 0.3094 | 0.1578 | 0.1578 | 0.5429 | 0.3777 |
| GPT | SequenceComparison | Participante 7 | 0.2911 | 0.1014 | 0.1304 | 0.5125 | 0.4202 |
| Claude | StringArrayEncoding | Participante 1 | 0.2482 | 0.1536 | 0.2733 | 0.4055 | 0.1605 |
| Claude | StringArrayEncoding | Participante 2 | 0.2531 | 0.2097 | 0.2582 | 0.4041 | 0.1405 |
| Claude | StringArrayEncoding | Participante 3 | 0.1769 | 0.0631 | 0.1537 | 0.3401 | 0.1505 |
| Claude | StringArrayEncoding | Participante 4 | 0.2171 | 0.1044 | 0.2281 | 0.4055 | 0.1304 |
| Claude | StringArrayEncoding | Participante 5 | 0.2380 | 0.1186 | 0.1432 | 0.4026 | 0.2876 |
| Claude | StringArrayEncoding | Participante 6 | 0.2189 | 0.1100 | 0.2314 | 0.3837 | 0.1505 |
| Claude | StringArrayEncoding | Participante 7 | 0.2526 | 0.1507 | 0.2329 | 0.4128 | 0.2140 |
| DeepSeek | StringArrayEncoding | Participante 1 | 0.1995 | 0.1411 | 0.2580 | 0.2718 | 0.1271 |
| DeepSeek | StringArrayEncoding | Participante 2 | 0.2469 | 0.2126 | 0.2647 | 0.3866 | 0.1237 |
| DeepSeek | StringArrayEncoding | Participante 3 | 0.1851 | 0.0623 | 0.1522 | 0.3823 | 0.1438 |
| DeepSeek | StringArrayEncoding | Participante 4 | 0.2607 | 0.1885 | 0.2761 | 0.4012 | 0.1773 |
| DeepSeek | StringArrayEncoding | Participante 5 | 0.2965 | 0.2186 | 0.2992 | 0.4273 | 0.2408 |
| DeepSeek | StringArrayEncoding | Participante 6 | 0.2686 | 0.2091 | 0.2659 | 0.4186 | 0.1806 |
| DeepSeek | StringArrayEncoding | Participante 7 | 0.2919 | 0.2403 | 0.2558 | 0.4375 | 0.2341 |
| Gemini | StringArrayEncoding | Participante 1 | 0.2172 | 0.1327 | 0.2206 | 0.3517 | 0.1639 |
| Gemini | StringArrayEncoding | Participante 2 | 0.2353 | 0.1882 | 0.2538 | 0.3823 | 0.1171 |
| Gemini | StringArrayEncoding | Participante 3 | 0.1723 | 0.0709 | 0.1475 | 0.3605 | 0.1104 |
| Gemini | StringArrayEncoding | Participante 4 | 0.2295 | 0.1126 | 0.2325 | 0.4055 | 0.1672 |
| Gemini | StringArrayEncoding | Participante 5 | 0.2402 | 0.1687 | 0.2127 | 0.3721 | 0.2074 |
| Gemini | StringArrayEncoding | Participante 6 | 0.2253 | 0.0996 | 0.2391 | 0.4186 | 0.1438 |
| Gemini | StringArrayEncoding | Participante 7 | 0.2296 | 0.1181 | 0.2423 | 0.4142 | 0.1438 |
| GPT | StringArrayEncoding | Participante 1 | 0.2200 | 0.1039 | 0.2390 | 0.4099 | 0.1271 |
| GPT | StringArrayEncoding | Participante 2 | 0.2388 | 0.1521 | 0.2637 | 0.3953 | 0.1438 |
| GPT | StringArrayEncoding | Participante 3 | 0.1795 | 0.0705 | 0.1492 | 0.3677 | 0.1304 |
| GPT | StringArrayEncoding | Participante 4 | 0.2482 | 0.1391 | 0.2691 | 0.4273 | 0.1572 |
| GPT | StringArrayEncoding | Participante 5 | 0.2278 | 0.0976 | 0.1533 | 0.4230 | 0.2375 |
| GPT | StringArrayEncoding | Participante 6 | 0.2259 | 0.1130 | 0.2323 | 0.4113 | 0.1472 |
| GPT | StringArrayEncoding | Participante 7 | 0.2501 | 0.1512 | 0.2512 | 0.4273 | 0.1706 |

Médias de CodeBLEU por modelo (considerando apenas as combinações com resultado válido): **DeepSeek** 0.2629, **Claude** 0.2399, **GPT** 0.2226, **Gemini** 0.2206.

## Stack técnica

- **.NET 10** / ASP.NET Core Web API — normalização do código-fonte
- `Microsoft.CodeAnalysis` (Roslyn) — parsing
- Swagger / OpenAPI habilitado para exploração e teste do endpoint
- **Python** + [`codebleu`](https://pypi.org/project/codebleu/) — cálculo das métricas CodeBLEU

## Estrutura do projeto

```
CodeBleuPreprocessor/
├── CodeBleuPreprocessor/                 # API .NET de normalização
│   ├── Controllers/
│   │   └── CodeBleuPreprocessorController.cs   # Endpoint POST /geracao-dataset
│   ├── Service/
│   │   └── CodeBleuPreprocessorService.cs      # Lógica de normalização via Roslyn
│   ├── CodeBleuPreprocessor.http               # Requisições de exemplo
│   └── Program.cs                              # Configuração do pipeline HTTP / Swagger
├── NormalizedGeneratedCode/               # Datasets normalizados por modelo/questão/participante
├── NormalizedReferenceCode/               # Datasets normalizados das soluções de referência
└── run_codebleu.py                        # Script de cálculo do CodeBLEU (caminhos manuais)
```

## Contexto

Este repositório é a etapa final do pipeline de avaliação por similaridade do TCC: normaliza o código bruto gerado pelos LLMs (armazenado no [GeneratedCodeByAI](https://github.com/IngridBatista/GeneratedCodeByAI)) e calcula, para cada combinação de modelo, questão e participante, as métricas de CodeBLEU (AST Match, Dataflow Match, N-gram Match e Weighted N-gram Match) contra as soluções de referência do especialista.
