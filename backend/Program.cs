using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using LoanEngine.Api.Models.Data;
using LoanEngine.Api.Services.Implementations;
using LoanEngine.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Configure Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Loan Recommendation API",
        Version = "v1",
        Description = "AI-Powered Loan Eligibility & Pre-Approved Loan Recommendation Engine"
    });
});

// Database Context (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LoanDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: System.TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    });
});

// Configure CORS for Angular application
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register Domain Services (SRP)
var geminiTimeoutSeconds = builder.Configuration.GetValue<int>("Gemini:TimeoutSeconds", 180);
builder.Services.AddHttpClient<IGeminiDocumentExtractorService, GeminiDocumentExtractorService>(client =>
{
    client.Timeout = System.TimeSpan.FromSeconds(geminiTimeoutSeconds);
});
builder.Services.AddScoped<IStatementValidationService, StatementValidationService>();
builder.Services.AddScoped<IFinancialAnalysisEngine, FinancialAnalysisEngine>();
builder.Services.AddScoped<ILoanAffordabilityEngine, LoanAffordabilityEngine>();
builder.Services.AddScoped<ILoanRecommendationService, LoanRecommendationService>();

var app = builder.Build();

// Ensure Database schema is created safely
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("SQL Server database schema verified/created successfully.");
    }
    catch (System.Exception ex)
    {
        logger.LogWarning(ex, "Could not automatically create/connect to SQL Server. Calculations will continue normally using in-memory response.");
    }
}

// Enable Swagger with Swagger 2.0 serialization for 100% Swagger UI version compatibility
app.UseSwagger(c =>
{
    c.SerializeAsV2 = true;
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loan Recommendation Engine v1");
    c.RoutePrefix = "swagger";
});

// Redirect root to swagger for convenience
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
