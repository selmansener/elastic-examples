using Bogus;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.MachineLearning;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ElasticExamples.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace ElasticExamples.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {
        private readonly ElasticsearchClient _elasticsearchClient;

        public MainController(ElasticsearchClient elasticsearchClient)
        {
            _elasticsearchClient = elasticsearchClient;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            TestModel model = new TestModel();

            var response = await _elasticsearchClient.GetAsync<TestModel>(TestModel.IndexName, id);

            //_elasticsearchClient.UpdateAsync(TestModel.IndexName, id: model.i)

            return Ok();
        }

        [HttpPost("PrepareSuggestionsData")]
        public async Task<IActionResult> PrepareSuggestionsData(CancellationToken cancellationToken)
        {
            var existsResponse = await _elasticsearchClient.Indices.ExistsAsync(TestModel.IndexName, cancellationToken);

            if (!existsResponse.IsSuccess())
            {
                throw new Exception("Elastic client check failed.");
            }

            if (!existsResponse.Exists)
            {
                var createIndexResponse = await _elasticsearchClient.Indices.CreateAsync<TestModel>(TestModel.IndexName, r => r.Mappings(m => m
                    .Properties(p => p
                        .SearchAsYouType(c => c.NameSuggestionField)
                    )
                ), cancellationToken).ConfigureAwait(false);

                if (!createIndexResponse.IsSuccess())
                {
                    throw new Exception("Create index failed.");
                }
            }

            var faker = new Faker();
            for (int i = 0; i < 5000; i++)
            {
                var name = faker.Commerce.ProductName();
                var model = new TestModel
                {
                    Id = i + 1,
                    Name = name,
                    NameSuggestionField = name,
                    Price = faker.Random.Decimal(max: 10000)
                };

                await _elasticsearchClient.IndexAsync<TestModel>(model, x => x.Index(TestModel.IndexName));
            }

            return Ok();
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(TestModel model)
        {
            await _elasticsearchClient.IndexAsync<TestModel>(model, x => x
                .Index(TestModel.IndexName));

            return Ok();
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> Update(int id, TestModel model)
        {
            await _elasticsearchClient.UpdateAsync<TestModel, TestModel>(TestModel.IndexName, id, req => req.Doc(model));

            return Ok();
        }


        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert(TestModel model)
        {
            await _elasticsearchClient.IndexAsync<TestModel>(model, x => x.Index(TestModel.IndexName));

            return Ok();
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest(string query)
        {
            var response = await _elasticsearchClient.SearchAsync<ECommerceModel>(s => s.Indices("kibana_sample_data_ecommerce")
                .Query(q => q.Match(m => m.Field("category")
                .Fuzziness(2)
                .MinimumShouldMatch(3)
                .Query(query)
            )));

            return Ok();
        }

        [HttpGet("suggest2")]
        public async Task<IActionResult> Suggest2(string query)
        {
            var response = await _elasticsearchClient.SearchAsync<TestModel>(s => s
                .Indices(TestModel.IndexName)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Fields(Fields.FromFields(["nameSuggestionField", "nameSuggestionField._2gram", "nameSuggestionField._3gram"]))
                        .Query(query)
                        .Fuzziness("AUTO")
                        .Type(TextQueryType.BoolPrefix) // Supports partial tokens
                    )
                )
            );

            return Ok();
        }
    }
}
