using ModelContextProtocol.Server;
using RAG_Demo.Common;
using System.ComponentModel;

namespace RAG_Demo.ApiService;

[McpServerToolType]
public static class PublicationTools
{
    [McpServerTool(Name = "add_publication"), Description("Add a new job publication to the database")]
    public static async Task<string> AddPublication(
        PublicationService service,
        [Description("The job title")] string title,
        [Description("The job description")] string description,
        [Description("A brief summary of the job")] string summary,
        [Description("Description of the company")] string companyDescription,
        [Description("Company brand")] string brand,
        [Description("Job function/category")] string function,
        [Description("Employment level (e.g., Junior, Senior)")] string employmentLevel,
        [Description("Required education level")] string educationLevel,
        [Description("Name of the company")] string companyName,
        [Description("Job location city")] string city,
        [Description("Minimum salary")] decimal salaryMinimum,
        [Description("Maximum salary")] decimal salaryMaximum,
        [Description("Minimum weekly hours")] int minimumWeeklyHours,
        [Description("Maximum weekly hours")] int maximumWeeklyHours,
        [Description("Job requirements (optional)")] string? requirements = null,
        [Description("Job benefits (optional)")] string? benefits = null)
    {
        var publication = new Publication
        {
            Title = title,
            Description = description,
            Summary = summary,
            Requirements = requirements,
            Benefits = benefits,
            CompanyDescription = companyDescription,
            Brand = brand,
            Function = function,
            EmploymentLevel = employmentLevel,
            EducationLevel = educationLevel,
            CompanyName = companyName,
            City = city,
            SalaryMinimum = salaryMinimum,
            SalaryMaximum = salaryMaximum,
            MinimumWeeklyHours = minimumWeeklyHours,
            MaximumWeeklyHours = maximumWeeklyHours
        };

        var result = await service.AddPublicationAsync(publication);
        return $"Successfully added publication '{result.Title}' with ID: {result.Id}";
    }

    [McpServerTool(Name = "query_publications"), Description("Search and query job publications using natural language")]
    public static async Task<string> QueryPublications(
        PublicationService service,
        [Description("The search query in natural language")] string query,
        [Description("Number of top results to return (default: 5)")] int? topK = 5)
    {
        var result = await service.QueryPublicationsAsync(query, topK);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(result.Answer);
        sb.AppendLine();
        sb.AppendLine($"Found {result.Publications.Count} matching publications:");
        sb.AppendLine();
        
        foreach (var pub in result.Publications)
        {
            sb.AppendLine($"- {pub.Title} at {pub.CompanyName} ({pub.City})");
            sb.AppendLine($"  Salary: {pub.SalaryMinimum:C} - {pub.SalaryMaximum:C}");
            sb.AppendLine($"  Match Distance: {pub.Distance:F4}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
