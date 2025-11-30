using System.Text.Json.Serialization;
using Pgvector;

namespace RAG_Demo.Common;

public class Publication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime PublishedDate { get; set; }

    public required string Summary { get; set; }
    public string? Requirements { get; set; }
    public string? Benefits { get; set; }
    public required string CompanyDescription { get; set; }
    public required string Brand { get; set; }
    public required  string Function { get; set; }
    public required string EmploymentLevel { get; set; }
    public required string EducationLevel { get; set; }
    public required  string CompanyName { get; set; }
    public required string City { get; set; }
    public decimal SalaryMinimum { get; set; }
    public decimal SalaryMaximum { get; set; }
    public int MinimumWeeklyHours { get; set; }
    public int MaximumWeeklyHours { get; set; }
    [JsonIgnore]
    public Vector? Embedding { get; set; }
}

public class PublicationWithDistance : Publication
{
    public double Distance { get; set; }
}