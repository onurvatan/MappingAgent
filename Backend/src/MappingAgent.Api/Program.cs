using MappingAgent.Api.Endpoints;
using MappingAgent.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMappingAgentApi(builder.Configuration);

var app = builder.Build();

await app.InitializeMappingAgentAsync();
app.UseMappingAgentApi();
app.MapMappingAgentEndpoints();

app.Run();
