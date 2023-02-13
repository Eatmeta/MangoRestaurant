using Mango.Web.Models;
using Mango.Web.Services.IServices;
using static Mango.Web.SD;

namespace Mango.Web.Services;

public class ProductService : BaseService, IProductService
{
    private readonly IHttpClientFactory _clientFactory;

    public ProductService(IHttpClientFactory clientFactory) : base(clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<T> CreateProductAsync<T>(ProductDto productDto)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.POST,
            Data = productDto,
            Url = ProductAPIBase + "/api/products",
            AccessToken = ""
        });
    }

    public async Task<T> DeleteProductAsync<T>(int id)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.DELETE,
            Url = ProductAPIBase + "/api/products/" + id,
            AccessToken = ""
        });
    }

    public async Task<T> GetAllProductsAsync<T>()
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.GET,
            Url = ProductAPIBase + "/api/products",
            AccessToken = ""
        });
    }

    public async Task<T> GetProductByIdAsync<T>(int id)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.GET,
            Url = ProductAPIBase + "/api/products/" + id,
        });
    }

    public async Task<T> UpdateProductAsync<T>(ProductDto productDto)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = ApiType.PUT,
            Data = productDto,
            Url = ProductAPIBase + "/api/products",
            AccessToken = ""
        });
    }
}