var builder = DistributedApplication.CreateBuilder(args);

var db = builder
    .AddPostgres("postgres", port: 5432)
    .WithImage("pgvector/pgvector")
    .WithImageTag("pg16")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("rag-publications");


var apiKey = builder.AddParameter("openai-api-key", secret: true);
var openai = builder.AddOpenAI("openai").WithApiKey(apiKey);

var chat = openai.AddModel("chat", "gpt-4o-mini");
var embeddings = openai.AddModel("embeddings", "text-embedding-3-small");

var apiService = builder.AddProject<Projects.RAG_Demo_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(chat)
    .WithReference(embeddings)
    .WithReference(db);

builder.AddProject<Projects.RAG_Demo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
