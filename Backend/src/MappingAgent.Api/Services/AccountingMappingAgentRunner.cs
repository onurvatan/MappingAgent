using MappingAgent.Api.Agents;
using MappingAgent.Domain.Entities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MappingAgent.Api.Services;

public sealed class AccountingMappingAgentRunner(
    [FromKeyedServices(AccountingMappingWorkflowAgent.AgentName)] AIAgent agent) : IAccountingMappingAgentRunner
{
    public async Task<string> RunAsync(
        SourceFile sourceFile,
        ExtractedDocument extractedDocument,
        CancellationToken cancellationToken)
    {
        var prompt = $$"""
        Analyze this parsed accounting document and return structured JSON only.

        Supported document kinds:
        - ExpenseInvoice
        - IncomeInvoice
        - CreditNote
        - UnknownAccountingDocument

        Supported directions:
        - Expense
        - Income
        - Unknown

        Parsed input:
        {
          "sourceFileName": "{{sourceFile.FileName}}",
          "extension": "{{sourceFile.Extension}}",
          "parserName": "{{extractedDocument.ParserName}}",
          "parserWarningsJson": {{extractedDocument.ParserWarningsJson}},
          "tablesJson": {{extractedDocument.TablesJson}},
          "rawText": {{System.Text.Json.JsonSerializer.Serialize(extractedDocument.RawText)}}
        }
        """;

        var result = await agent.RunAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            null,
            null,
            cancellationToken);

        return result.Text ?? string.Empty;
    }
}
