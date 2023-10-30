using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using MeccaProducts.Services;
using Microsoft.Extensions.Configuration;
using MeccaProducts.Models;
using System.Text.Json;

namespace MeccaProducts.Test
{
    [TestClass]
    public class ProductServiceTests
    {
        [TestMethod]
        public async Task GetProductsForBrandAsync_BrandIsNullOrEmpty_ReturnsBadRequest()
        {
            // Arrange
            var brand = string.Empty; // Brand is null or empty
            var httpClientFactory = CreateHttpClientFactory(CreateMockedHttpClient());
            var productService = new ProductService(httpClientFactory, GetMockedConfiguration());

            // Act
            var result = await productService.GetProductsForBrandAsync(brand);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        private IHttpClientFactory CreateHttpClientFactory(HttpClient httpClient)
        {
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return httpClientFactory.Object;
        }

        private IConfiguration GetMockedConfiguration()
        {
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c.GetSection(It.IsAny<string>()).Value).Returns("some-value");
            return configuration.Object;
        }

        private HttpClient CreateMockedHttpClient()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handler.Object);

            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            return httpClient;
        }
    }
}
