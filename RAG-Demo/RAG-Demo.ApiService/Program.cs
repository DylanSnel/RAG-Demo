using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using Pgvector.EntityFrameworkCore;
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
builder.AddNpgsqlDbContext<PublicationDbContext>("rag-publications",configureDbContextOptions: o => o.UseNpgsql(b => b.UseVector()));

// Add OpenAI clients
builder.AddOpenAIClient("chat").AddChatClient();
builder.AddOpenAIClient("embeddings").AddEmbeddingGenerator();

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
app.MapPost("/publications", async (Publication publication, PublicationDbContext db, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) =>
{
    // Create embedding from publication content
    var textToEmbed = $"{publication.Title} {publication.Description} {publication.Summary} " +
                     $"{publication.CompanyDescription} {publication.Function} {publication.EmploymentLevel} {publication.City}";

    var embeddings = await embeddingGenerator.GenerateAsync([textToEmbed]);
    var embedding = embeddings[0].Vector.ToArray();

    // Convert to Pgvector.Vector
    publication.Embedding = new Vector(embedding);
    publication.PublishedDate = DateTime.UtcNow;

    db.Publications.Add(publication);
    await db.SaveChangesAsync();

    return Results.Created($"/publications/{publication.Id}", new { id = publication.Id, title = publication.Title });
})
.WithName("AddPublication");

// Query Publication endpoint
app.MapPost("/publications/query", async (QueryRequest request, PublicationDbContext db,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IChatClient chatClient) =>
{
    // Generate embedding for the query
    var queryEmbeddings = await embeddingGenerator.GenerateAsync([request.Query]);
    var queryEmbedding = new Vector(queryEmbeddings[0].Vector.ToArray());

    // Perform vector similarity search using pgvector
    var publicationsWithDistance = await db.Publications
        .OrderBy(p => p.Embedding!.CosineDistance(queryEmbedding))
        .Take(request.TopK ?? 5)
        .Select(p => new PublicationWithDistance
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Summary = p.Summary,
            Requirements = p.Requirements,
            Benefits = p.Benefits,
            CompanyDescription = p.CompanyDescription,
            Brand = p.Brand,
            Function = p.Function,
            EmploymentLevel = p.EmploymentLevel,
            EducationLevel = p.EducationLevel,
            CompanyName = p.CompanyName,
            City = p.City,
            SalaryMinimum = p.SalaryMinimum,
            SalaryMaximum = p.SalaryMaximum,
            MinimumWeeklyHours = p.MinimumWeeklyHours,
            MaximumWeeklyHours = p.MaximumWeeklyHours,
            PublishedDate = p.PublishedDate,
            Distance = p.Embedding!.CosineDistance(queryEmbedding)
        })
        .ToListAsync();

    // Create context for chat from top results
    var context = string.Join("\n\n", publicationsWithDistance.Select(p =>
        $"Title: {p.Title}\nCompany: {p.CompanyName}\nLocation: {p.City}\nDescription: {p.Description}\nSummary: {p.Summary}"));

    // Use chat API to generate response
    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, "You are a helpful assistant that answers questions about job publications. " +
                            "Use the following publication information to answer the user's query. " +
                            "If the information doesn't contain the answer, say so."),
        new(ChatRole.User, $"Context:\n{context}\n\nQuestion: {request.Query}")
    };

    var chatResponse = await chatClient.GetResponseAsync(messages);
    var answer = chatResponse.Text ?? "No answer generated.";

    return Results.Ok(new
    {
        answer,
        publications = publicationsWithDistance
    });
})
.WithName("QueryPublications");

app.MapDefaultEndpoints();

app.Run();

record QueryRequest(string Query, int? TopK);
