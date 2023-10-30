using MeccaProducts.Interface;
using MeccaProducts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http;

namespace MeccaProducts.Services
{
    public class ProductService : IProductService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ProductService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }
        public async Task<HttpResponseMessage> GetProductsForBrandAsync(string brand)
        {
            if (string.IsNullOrEmpty(brand))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Brand is required."
                };
            }

            if (!_config.GetSection("ApiLinks:ProductLink").Exists() || !_config.GetSection("ApiLinks:BaseUrl").Exists())
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Check API links."
                };
            }

            var productLink = string.Format(_config.GetSection("ApiLinks:ProductLink").Value, brand);
            var baseUrl = _config.GetSection("ApiLinks:BaseUrl").Value;

            var products = new List<Product>();

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            HttpResponseMessage productResponse = await retryPolicy.ExecuteAsync(async () =>
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    var response = await client.GetAsync(productLink);

                    if (!response.IsSuccessStatusCode)
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            ReasonPhrase = "Error occurred while fetching data from the API"
                        };
                    }

                    return response;
                }
            });

            if (!productResponse.IsSuccessStatusCode)
            {
                return productResponse;
            }

            var productData = await productResponse.Content.ReadFromJsonAsync<List<Product>>();

            foreach (var product in productData)
            {
                var priceApiUrl = $"{baseUrl}/{product.Id}/price";

                HttpResponseMessage priceResponse = await retryPolicy.ExecuteAsync(async () =>
                {
                    using (var client = _httpClientFactory.CreateClient())
                    {
                        var response = await client.GetAsync(priceApiUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                            {
                                ReasonPhrase = "Error occurred while fetching price data from the API."
                            };
                        }

                        return response;
                    }
                });

                if (!priceResponse.IsSuccessStatusCode)
                {
                    return priceResponse;
                }

                var updatedPrice = await priceResponse.Content.ReadAsStringAsync();
                var responsePrice = JsonSerializer.Deserialize<PriceResponse>(updatedPrice);
                product.Price = responsePrice.price;
                products.Add(product);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<List<Product>>(products, new JsonMediaTypeFormatter())
            };
        }

    }
}
