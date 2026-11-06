using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;
using Xunit.Abstractions;
using LoanEngine.Api.Controllers;
using LoanEngine.Api.Services.Interfaces;

namespace LoanEngine.Tests
{
    public class SwaggerGenerationTests
    {
        private readonly ITestOutputHelper _output;

        public SwaggerGenerationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SwaggerDoc_SerializeAsV2_ShouldGenerateSwagger20()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddControllers().AddApplicationPart(typeof(LoanProcessingController).Assembly);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "AI Loan Recommendation API",
                    Version = "v1"
                });
            });

            builder.Services.AddScoped<ILoanRecommendationService>(_ => null!);
            builder.Services.AddScoped<IFinancialAnalysisEngine>(_ => null!);
            builder.Services.AddScoped<ILoanAffordabilityEngine>(_ => null!);

            var app = builder.Build();
            var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();

            var doc = swaggerProvider.GetSwagger("v1");

            using var stringWriter = new StringWriter();
            var jsonWriter = new OpenApiJsonWriter(stringWriter);
            doc.SerializeAsV2(jsonWriter);
            var json = stringWriter.ToString();

            _output.WriteLine("First 300 characters of V2 swagger.json:");
            _output.WriteLine(json.Length > 300 ? json.Substring(0, 300) : json);

            Assert.Contains("\"swagger\": \"2.0\"", json);
        }
    }
}
