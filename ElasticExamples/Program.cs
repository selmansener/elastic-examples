
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Transport;
using Scalar.AspNetCore;

namespace ElasticExamples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            var url = builder.Configuration["ElasticSearchClient:Url"];
            var apiKey = builder.Configuration["ElasticSearchClient:ApiKey"];

            var elasticSearchClientSettings = new ElasticsearchClientSettings(new Uri(url));
            elasticSearchClientSettings.Authentication(new ApiKey(apiKey));

            var client = new ElasticsearchClient(elasticSearchClientSettings);
            builder.Services.AddSingleton(client);


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
