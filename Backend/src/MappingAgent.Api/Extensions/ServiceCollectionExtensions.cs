using Azure.AI.OpenAI;
using MappingAgent.Api.Agents;
using MappingAgent.Api.Configuration;
using MappingAgent.Api.Data;
using MappingAgent.Api.Options;
using MappingAgent.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using System.ClientModel;

namespace MappingAgent.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMappingAgentApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info ??= new OpenApiInfo();
                document.Info.Title = "MappingAgent API";
                document.Info.Version = "v1";
                document.Info.Description = "Accounting document ingestion, parsing, review, and persistence API.";
                return Task.CompletedTask;
            });
        });
        services.AddSwaggerUI();

        services.Configure<AgentWorkflowOptions>(
            configuration.GetSection(AgentWorkflowOptions.SectionName));
        services.Configure<FoundrySettings>(
            configuration.GetSection(FoundrySettings.SectionName));
        services.Configure<StorageOptions>(
            configuration.GetSection(StorageOptions.SectionName));

        services.AddDbContext<AccountingDbContext>((serviceProvider, options) =>
        {
            var storageOptions = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>()
                .Value;

            options.UseSqlite(storageOptions.ApprovedConnectionString);
        });

        services.AddSingleton<IIngestionStore, InMemoryIngestionStore>();
        services.AddSingleton<IHashingService, HashingService>();
        services.AddSingleton<IFileParsingService, FileParsingService>();
        services.AddSingleton<AccountingMappingWorkflowAgent>();

        var foundrySettings = configuration
            .GetSection(FoundrySettings.SectionName)
            .Get<FoundrySettings>();

        if (foundrySettings?.IsConfigured == true)
        {
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<FoundrySettings>>().Value;
                if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    return new AzureOpenAIClient(new Uri(settings.Endpoint), new ApiKeyCredential(settings.ApiKey));
                }

                return new AzureOpenAIClient(new Uri(settings.Endpoint), new Azure.Identity.DefaultAzureCredential());
            });

            services.AddSingleton<IChatClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<FoundrySettings>>().Value;
                return sp.GetRequiredService<AzureOpenAIClient>()
                    .GetChatClient(settings.Deployment)
                    .AsIChatClient();
            });

            services.AddKeyedSingleton<AIAgent>(AccountingMappingWorkflowAgent.AgentName, (sp, key) =>
                sp.GetRequiredService<AccountingMappingWorkflowAgent>()
                    .CreateAgent(key?.ToString() ?? AccountingMappingWorkflowAgent.AgentName));

            services.AddScoped<IAccountingMappingAgentRunner, AccountingMappingAgentRunner>();
        }
        else
        {
            services.AddScoped<IAccountingMappingAgentRunner, UnavailableAccountingMappingAgentRunner>();
        }

        services.AddScoped<IAccountingDocumentAnalysisService, AccountingDocumentAnalysisService>();
        services.AddSingleton<IFileParser, PdfFileParser>();
        services.AddSingleton<IFileParser, ExcelFileParser>();
        services.AddSingleton<IFileParser, CsvFileParser>();
        services.AddSingleton<IFileParser, TextFileParser>();

        services.AddCors(options =>
        {
            options.AddPolicy("frontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
