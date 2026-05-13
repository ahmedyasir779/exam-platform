using ExamPlatform.Api.Endpoints;
using ExamPlatform.Application.Embedding;
using ExamPlatform.Application.Export;
using ExamPlatform.Application.ExamGeneration;
using ExamPlatform.Application.Grading;
using ExamPlatform.Application.PdfProcessing;
using ExamPlatform.Domain.Interfaces;
using ExamPlatform.Infrastructure.Persistence;
using ExamPlatform.Infrastructure.Storage;
using ExamPlatform.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();

// File storage
var storagePath = builder.Configuration["Storage:BasePath"] ?? "C:/tmp/examplatform/files";
builder.Services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(storagePath));

// Vector store
var vectorPath = builder.Configuration["VectorStore:BasePath"] ?? "C:/tmp/examplatform/faiss";
builder.Services.AddSingleton<IVectorStore>(_ => new LocalVectorStore(vectorPath));

// PDF processing
builder.Services.AddScoped<ChunkingStrategy>();
builder.Services.AddScoped<PdfProcessingService>();

// Embedding HTTP client
builder.Services.AddHttpClient<EmbeddingService>((_, client) =>
{
    var apiKey = builder.Configuration["Embedding:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
});
builder.Services.AddScoped<EmbeddingService>();

// Grok HTTP client
builder.Services.AddHttpClient<GrokClient>((_, client) =>
{
    var apiKey = builder.Configuration["Grok:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
});

// Application services
builder.Services.AddScoped<ExamGenerationService>();
builder.Services.AddScoped<GradingService>();
builder.Services.AddSingleton<PdfExportService>();
builder.Services.AddSingleton<DocxExportService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDocumentEndpoints();
app.MapExamEndpoints();
app.MapSubmissionEndpoints();
app.MapExportEndpoints();

app.Run();
