using RAG_Demo.Common;

namespace RAG_Demo.Web;

public class PublicationApiClient(HttpClient httpClient)
{
    public async Task<Publication?> AddPublicationAsync(Publication publication)
    {
        var response = await httpClient.PostAsJsonAsync("/publications", publication);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Publication>();
    }

    public async Task<QueryResponse?> QueryPublicationsAsync(string query, int topK = 5)
    {
        var request = new { Query = query, TopK = topK };
        var response = await httpClient.PostAsJsonAsync("/publications/query", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<QueryResponse>();
    }
}

public record QueryResponse(string Answer, List<PublicationWithDistance> Publications);
