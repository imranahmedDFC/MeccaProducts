using MeccaProducts.Interface;
using MeccaProducts.Models;
using MeccaProducts.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MeccaProducts.Controllers
{
    [Route("api/v1/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsForBrand([FromQuery] string brand)
        {
            var response = await _productService.GetProductsForBrandAsync(brand);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<Product>>(content);
                return Ok(products);
            }
            else
            {
                return StatusCode((int)response.StatusCode, response.ReasonPhrase);
            }
        }

    }

}
