using Mango.Web.Models;
using Mango.Web.Services.IServices;
using static Mango.Web.Sd;

namespace Mango.Web.Services;

public class ProductService : BaseService, IProductService
{
    public ProductService(IHttpClientFactory clientFactory) : base(clientFactory)
    {
    }
    
    public async Task<T> CreateProductAsync<T>(ProductDto productDto)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.Post,
            Data = productDto,
            Url = ProductApiBase + "/api/products",
            AccessToken = ""
        });
    }

    public async Task<T> DeleteProductAsync<T>(int id)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.Delete,
            Url = ProductApiBase + "/api/products/" + id,
            AccessToken = ""
        });
    }

    public async Task<T> GetAllProductsAsync<T>()
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.Get,
            Url = ProductApiBase + "/api/products",
            AccessToken = ""
        });
    }

    public async Task<T> GetProductByIdAsync<T>(int id)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.Get,
            Url = ProductApiBase + "/api/products/" + id,
            AccessToken = ""
        });
    }

    public async Task<T> UpdateProductAsync<T>(ProductDto productDto)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.Put,
            Data = productDto,
            Url = ProductApiBase + "/api/products",
            AccessToken = ""
        });
    }
}