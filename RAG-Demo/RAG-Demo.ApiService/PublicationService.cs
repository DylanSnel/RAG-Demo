using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RAG_Demo.Common;

namespace RAG_Demo.ApiService;

public class PublicationService
{
    private readonly PublicationDbContext _db;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IChatClient _chatClient;
    private readonly ILogger<PublicationService> _logger;

    public PublicationService(
        PublicationDbContext db,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IChatClient chatClient,
        ILogger<PublicationService> logger)
    {
        _db = db;
        _embeddingGenerator = embeddingGenerator;
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AddPublicationResult> AddPublicationAsync(Publication publication)
    {
        try
        {
            // Create embedding from publication content
            var textToEmbed = publication.ToString();
            var embeddings = await _embeddingGenerator.GenerateAsync([textToEmbed]);
            var embedding = embeddings[0].Vector.ToArray();

            // Convert to Pgvector.Vector
            publication.Embedding = new Vector(embedding);
            publication.PublishedDate = DateTime.UtcNow;

            _db.Publications.Add(publication);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Added publication {Id} - {Title}", publication.Id, publication.Title);

            return new AddPublicationResult(publication.Id, publication.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding publication");
            throw;
        }
    }

    public async Task<QueryPublicationsResult> QueryPublicationsAsync(string query, int? topK = 5)
    {
        try
        {
            // Generate embedding for the query
            var queryEmbeddings = await _embeddingGenerator.GenerateAsync([query]);
            var queryEmbedding = new Vector(queryEmbeddings[0].Vector.ToArray());

            // Perform vector similarity search using pgvector
            var publicationsWithDistance = await _db.Publications
                .OrderBy(p => p.Embedding!.CosineDistance(queryEmbedding))
                .Take(topK ?? 5)
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
            var context = string.Join("\n\n", publicationsWithDistance.Select(p => p.ToString()));

            // Use chat API to generate response
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are a helpful assistant that answers questions about job publications. " +
                                    "Use the following publication information to answer the user's query. " +
                                    "If the information doesn't contain the answer, say so. Explain why you think each publication is a match"),
                new(ChatRole.User, $"Context:\n{context}\n\nQuestion: {query}")
            };

            var chatResponse = await _chatClient.GetResponseAsync(messages);
            var answer = chatResponse.Text ?? "No answer generated.";

            _logger.LogInformation("Processed query: {Query}, found {Count} publications", query, publicationsWithDistance.Count);

            return new QueryPublicationsResult(answer, publicationsWithDistance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying publications");
            throw;
        }
    }
}

public record AddPublicationResult(Guid Id, string Title);
public record QueryPublicationsResult(string Answer, List<PublicationWithDistance> Publications);
