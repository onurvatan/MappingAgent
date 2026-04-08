using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MappingAgent.Api.Agents;

public sealed class AccountingMappingWorkflowAgent(IChatClient chatClient)
{
    public const string AgentName = "AccountingMappingWorkflowAgent";

    public const string WorkflowDescription =
        "Sequential accounting mapping workflow: document-classifier -> field-extractor -> category-mapper.";

    private const string ClassifierInstructions = """
You classify accounting documents.

Input contains parsed document text and strict enum values.
Return JSON only with:
- documentKind
- direction
- confidence
- rawText
- parserWarnings

Rules:
- documentKind must be one of: ExpenseInvoice, IncomeInvoice, CreditNote, UnknownAccountingDocument
- direction must be one of: Expense, Income, Unknown
- Preserve rawText and parserWarnings exactly from the input payload
- Do not add markdown or commentary
""";

    private const string ExtractorInstructions = """
You extract invoice fields from the JSON passed from the previous step.

Return JSON only with:
- documentKind
- direction
- confidence
- rawText
- parserWarnings
- counterpartyName
- counterpartyTaxIdentifier
- invoiceNumber
- invoiceDate
- dueDate
- currency
- subtotal
- taxAmount
- totalAmount
- warnings

Rules:
- Keep enum values unchanged
- Use ISO date format yyyy-MM-dd when a date is known
- Use null for unknown fields
- Keep warnings as an array of strings
- Do not add markdown or commentary
""";

    private const string CategorizerInstructions = """
You finalize the structured accounting mapping.

Allowed expense categories:
- Rent
- Utilities
- Software
- OfficeSupplies
- Travel
- Marketing
- ProfessionalServices
- Tax
- OtherExpense

Allowed income categories:
- ProductSales
- ServiceRevenue
- SubscriptionRevenue
- ConsultingRevenue
- OtherIncome

Return JSON only with:
- documentKind
- direction
- counterpartyName
- counterpartyTaxIdentifier
- invoiceNumber
- invoiceDate
- dueDate
- currency
- subtotal
- taxAmount
- totalAmount
- suggestedCategory
- confidence
- warnings

Rules:
- suggestedCategory must come from the allowed list and must match direction
- confidence must be a number between 0 and 1
- Do not add markdown or commentary
""";

    public AIAgent CreateAgent(string name)
    {
        var classifier = chatClient.AsAIAgent(
            instructions: ClassifierInstructions,
            name: "document-classifier");

        var extractor = chatClient.AsAIAgent(
            instructions: ExtractorInstructions,
            name: "field-extractor");

        var categorizer = chatClient.AsAIAgent(
            instructions: CategorizerInstructions,
            name: "category-mapper");

        var workflow = AgentWorkflowBuilder.BuildSequential(name, [classifier, extractor, categorizer]);
        return workflow.AsAIAgent(
            name,
            name,
            WorkflowDescription,
            InProcessExecution.OffThread,
            includeExceptionDetails: false,
            includeWorkflowOutputsInResponse: false);
    }
}
