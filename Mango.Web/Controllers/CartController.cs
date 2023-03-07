using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers;

public class CartController : Controller
{
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly ICouponService _couponService;

    public CartController(IProductService productService, ICartService cartService, ICouponService couponService)
    {
        _productService = productService;
        _cartService = cartService;
        _couponService = couponService;
    }
    
    public async Task<IActionResult> CartIndex()
    {
        return View(await LoadCartDtoBasedOnLoggedInUser());
    }

    private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
    {
        var userId = User.Claims.FirstOrDefault(u => u.Type == "sub")?.Value;
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var response = await _cartService.GetCartByUserIdAsnyc<ResponseDto>(userId, accessToken);
        var cartDto = new CartDto();

        if (response != null && response.IsSuccess)
        {
            cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
        }

        if (cartDto.CartHeader != null)
        {
            if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
            {
                var coupon = await _couponService.GetCoupon<ResponseDto>(cartDto.CartHeader.CouponCode, accessToken);
                if (coupon != null && coupon.IsSuccess && coupon.Result != null)
                {
                    var couponObj = JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(coupon.Result));
                    cartDto.CartHeader.DiscountTotal = couponObj.DiscountAmount;
                }
            }
            
            foreach (var detail in cartDto.CartDetails)
            {
                cartDto.CartHeader.OrderTotal += detail.Product.Price * detail.Count;
            }
            
            cartDto.CartHeader.OrderTotal -= cartDto.CartHeader.DiscountTotal;
        }

        return cartDto;
    }

    public async Task<IActionResult> Remove(int cartDetailsId)
    {
        var userId = User.Claims.FirstOrDefault(u => u.Type == "sub")?.Value;
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var response = await _cartService.RemoveFromCartAsync<ResponseDto>(cartDetailsId, accessToken);

        return RedirectToAction(nameof(CartIndex));
    }

    [HttpPost]
    [ActionName("ApplyCoupon")]
    public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
    {
        var userId = User.Claims.FirstOrDefault(u => u.Type == "sub")?.Value;
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var response = await _cartService.ApplyCoupon<ResponseDto>(cartDto, accessToken);

        return RedirectToAction(nameof(CartIndex));
    }
    
    [HttpPost]
    [ActionName("RemoveCoupon")]
    public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
    {
        var userId = User.Claims.FirstOrDefault(u => u.Type == "sub")?.Value;
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var response = await _cartService.RemoveCoupon<ResponseDto>(cartDto.CartHeader.UserId, accessToken);

        return RedirectToAction(nameof(CartIndex));
    }
    
    public async Task<IActionResult> Checkout()
    {
        return View(await LoadCartDtoBasedOnLoggedInUser());
    }
    
    [HttpPost]
    public async Task<IActionResult> Checkout(CartDto cartDto) 
    {
        try
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.Checkout<ResponseDto>(cartDto.CartHeader, accessToken);
            
            return RedirectToAction(nameof(Confirmation));
        }
        catch(Exception e)
        {
            return View(cartDto);
        }
    }
    
    public async Task<IActionResult> Confirmation()
    {
        return RedirectToAction(nameof(Checkout));
    }
}