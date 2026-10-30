# Contribuindo com o MindCare

Obrigado pelo interesse! Este guia resume como preparar o ambiente e enviar mudanças.

## Fluxo

1. Faça um fork e crie uma branch a partir de `main` (`feat/agenda-semanal`, `fix/cpf-mascara`…).
2. Siga o [README](README.md#começando) para rodar o projeto.
3. Garanta que `dotnet build` (sem avisos: o projeto usa `TreatWarningsAsErrors`) e `dotnet test` passam.
4. Alterou o modelo de dados? Crie a migration correspondente (veja o README).
5. Abra um pull request descrevendo o problema e a solução.

## Padrão de commits

Os commits seguem o formato `tipo: descrição curta`, inspirado no [Conventional Commits](https://www.conventionalcommits.org/pt-br/).

| Tipo | Quando usar |
| --- | --- |
| `feat` | Novo recurso (relaciona-se ao MINOR do versionamento semântico). |
| `fix` | Correção de bug (relaciona-se ao PATCH). |
| `docs` | Mudanças apenas na documentação. |
| `test` | Criação ou alteração de testes. |
| `build` | Arquivos de build e dependências (usado também pelo Dependabot). |
| `perf` | Melhorias de performance. |
| `style` | Formatação, lint, espaços. Sem mudança de comportamento. |
| `refactor` | Refatoração que não altera a funcionalidade. |
| `chore` | Tarefas de manutenção e configuração. |
| `ci` | Integração contínua (workflows, Dependabot para actions). |
| `raw` | Arquivos de configuração, dados ou parâmetros. |
| `cleanup` | Remoção de código comentado ou trechos desnecessários. |
| `remove` | Exclusão de arquivos ou funcionalidades obsoletas. |

## Estilo de código

- As regras ficam no [`.editorconfig`](.editorconfig) e são verificadas no build.
- Mensagens exibidas ao usuário ficam em português. Erros de regra de negócio usam `BusinessException`.
- No front-end, prefira os atributos `data-crud`/`data-api-form` do `app.js` em vez de scripts por página.
