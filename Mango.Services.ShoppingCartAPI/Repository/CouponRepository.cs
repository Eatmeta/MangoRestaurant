using Mango.Services.ShoppingCartAPI.Models.Dto;
using Newtonsoft.Json;

namespace Mango.Services.ShoppingCartAPI.Repository;

public class CouponRepository : ICouponRepository
{
    private readonly HttpClient client;

    public CouponRepository(HttpClient client)
    {
        this.client = client;
    }

    public async Task<CouponDto> GetCoupon(string couponName)
    {
        var response = await client.GetAsync($"/api/coupon/{couponName}");
        var apiContent = await response.Content.ReadAsStringAsync();
        var resp = JsonConvert.DeserializeObject<ResponseDto>(apiContent);
        
        return resp.IsSuccess
            ? JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(resp.Result))
            : new CouponDto();
    }
}