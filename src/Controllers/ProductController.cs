using Bogus;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Mvc;
using src.Models;

namespace src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController(ElasticsearchClient client) : ControllerBase
    {
        private readonly ElasticsearchClient _client = client;

        [HttpGet("seed")]
        public async Task<IActionResult> SeedData()
        {
            for (int i = 0; i < 100; i++)
            {
                Faker faker = new();
                CreateProductDto product = new()
                {
                    Name = faker.Commerce.ProductName(),
                    Description = faker.Commerce.ProductDescription(),
                    Price = faker.Random.Decimal(1, 100),
                    Stock = faker.Random.Decimal(1, 30)
                };
                await Create(product, CancellationToken.None);
            }
            return Created();
        }

        [HttpGet]
        public IActionResult Get()
        {
            var response = _client.SearchAsync<Product>(new SearchRequest()).GetAwaiter().GetResult();
            return Ok(response.Documents);
        }

        [HttpGet("filter")]
        public IActionResult Get([FromQuery] string description)
        {
            SearchRequest searchRequest = new("products")
            {
                Size = 100,
                Query = new WildcardQuery(new Field("description"))
                {
                    Wildcard = "*" + description + "*"
                }
            };

            var response = _client.SearchAsync<Product>(searchRequest).GetAwaiter().GetResult();

            return Ok(response.Documents);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto createProductDto, CancellationToken cancellationToken)
        {
            Product product = new()
            {
                Id = Guid.NewGuid(),
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                Stock = createProductDto.Stock
            };

            CreateRequest<Product> request = new(product.Id.ToString())
            {
                Document = product
            };

            CreateResponse createResponse = await _client.CreateAsync(request, cancellationToken);

            return Ok(createResponse.Id);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProductDto updateProductDto, CancellationToken cancellationToken)
        {

            UpdateRequest<Product, UpdateProductDto> request = new("products", updateProductDto.Id.ToString())
            {
                Doc = updateProductDto
            };

            UpdateResponse<Product> updateResponse = await _client.UpdateAsync(request, cancellationToken);

            return Ok(updateResponse.Id);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            DeleteRequest request = new("products", id.ToString());
            DeleteResponse deleteResponse = await _client.DeleteAsync(request, cancellationToken);

            return Ok(deleteResponse.Id);
        }

    }
}