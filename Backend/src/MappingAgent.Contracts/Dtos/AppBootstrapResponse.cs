using MappingAgent.Contracts.Enums;

namespace MappingAgent.Contracts.Dtos;

public sealed record AppBootstrapResponse(
    string ApplicationName,
    string ApiVersion,
    IReadOnlyList<string> SupportedExtensions,
    IReadOnlyList<DocumentKind> DocumentKinds,
    IReadOnlyList<FileProcessingStatus> FileStatuses,
    IReadOnlyList<ReviewStatus> ReviewStatuses,
    IReadOnlyList<string> ExpenseCategories,
    IReadOnlyList<string> IncomeCategories);
