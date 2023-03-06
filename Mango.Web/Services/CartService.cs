using Mango.Web.Models;
using Mango.Web.Services.IServices;

namespace Mango.Web.Services;

public class CartService : BaseService, ICartService
{
    private readonly IHttpClientFactory _clientFactory;

    public CartService(IHttpClientFactory clientFactory) : base(clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<T> AddToCartAsync<T>(CartDto cartDto, string token = null)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = Sd.ApiType.Post,
            Data = cartDto,
            Url = Sd.ShoppingCartApiBase + "/api/cart/AddCart",
            AccessToken = token
        });
    }

    public async Task<T> GetCartByUserIdAsnyc<T>(string userId, string token = null)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = Sd.ApiType.Get,
            Url = Sd.ShoppingCartApiBase + "/api/cart/GetCart/" + userId,
            AccessToken = token
        });
    }

    public async Task<T> RemoveFromCartAsync<T>(int cartId, string token = null)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = Sd.ApiType.Post,
            Data = cartId,
            Url = Sd.ShoppingCartApiBase + "/api/cart/RemoveCart",
            AccessToken = token
        });
    }

    public async Task<T> ApplyCoupon<T>(CartDto cartDto, string token = null)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = Sd.ApiType.Post,
            Data = cartDto,
            Url = Sd.ShoppingCartApiBase + "/api/cart/ApplyCoupon",
            AccessToken = token
        });
    }

    public async Task<T> RemoveCoupon<T>(string userId, string token = null)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = Sd.ApiType.Post,
            Data = userId,
            Url = Sd.ShoppingCartApiBase + "/api/cart/RemoveCoupon",
            AccessToken = token
        });
    }

    public async Task<T> UpdateCartAsync<T>(CartDto cartDto, string token = null)
    {
        return await SendAsync<T>(new ApiRequest
        {
            ApiType = Sd.ApiType.Post,
            Data = cartDto,
            Url = Sd.ShoppingCartApiBase + "/api/cart/UpdateCart",
            AccessToken = token
        });
    }
}