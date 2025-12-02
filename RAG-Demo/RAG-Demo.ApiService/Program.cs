using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using RAG_Demo.ApiService;
using RAG_Demo.Common;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add DbContext
builder.AddNpgsqlDbContext<PublicationDbContext>("rag-publications", configureDbContextOptions: o => o.UseNpgsql(b => b.UseVector()));

// Add OpenAI clients
builder.AddOpenAIClient("chat").AddChatClient();
builder.AddOpenAIClient("embeddings").AddEmbeddingGenerator();

// Add PublicationService
builder.Services.AddScoped<PublicationService>();

// Add MCP server
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PublicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.MapGet("/", () => "RAG Demo API is running. Use /publications endpoints.");

// Add Publication endpoint
app.MapPost("/publications", async (Publication publication, PublicationService service) =>
{
    var result = await service.AddPublicationAsync(publication);
    return Results.Created($"/publications/{result.Id}", result);
})
.WithName("AddPublication");

// Query Publication endpoint
app.MapPost("/publications/query", async (QueryRequest request, PublicationService service) =>
{
    var result = await service.QueryPublicationsAsync(request.Query, request.TopK);
    return Results.Ok(new
    {
        result.Answer,
        Publications = result.Publications
    });
})
.WithName("QueryPublications");

app.MapDefaultEndpoints();

// Map MCP server endpoint
app.MapMcp("/mcp");

app.Run();

record QueryRequest(string Query, int? TopK);
