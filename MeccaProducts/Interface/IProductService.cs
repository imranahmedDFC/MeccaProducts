using MeccaProducts.Models;

namespace MeccaProducts.Interface
{
    public interface IProductService
    {
        Task<HttpResponseMessage> GetProductsForBrandAsync(string brand);
    }
}
