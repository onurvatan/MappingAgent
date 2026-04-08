using MappingAgent.Api.Data;

namespace MappingAgent.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeMappingAgentAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
        await SeedData.InitializeAsync(dbContext, CancellationToken.None);
    }

    public static WebApplication UseMappingAgentApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("frontend");

        return app;
    }
}
