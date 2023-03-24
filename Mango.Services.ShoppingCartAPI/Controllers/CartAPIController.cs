using Mango.Services.ShoppingCartAPI.Messages;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.RabbitMQSender;
using Mango.Services.ShoppingCartAPI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.ShoppingCartAPI.Controllers;

[ApiController]
[Route("api/cart")]
public class CartAPIController : Controller
{
    private readonly ICartRepository _cartRepository;
    protected ResponseDto _response;
    private readonly ICouponRepository _couponRepository;
    private readonly IRabbitMQCartMessageSender _rabbitMQCartMessageSender;

    public CartAPIController(ICartRepository cartRepository, ICouponRepository couponRepository, IRabbitMQCartMessageSender rabbitMQCartMessageSender)
    {
        _cartRepository = cartRepository;
        _response = new ResponseDto();
        _couponRepository = couponRepository;
        _rabbitMQCartMessageSender = rabbitMQCartMessageSender;
    }

    [HttpGet("GetCart/{userId}")]
    public async Task<object> GetCart(string userId)
    {
        try
        {
            var cartDto = await _cartRepository.GetCartByUserId(userId);
            _response.Result = cartDto;
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }

    [HttpPost("AddCart")]
    public async Task<object> AddCart(CartDto cartDto)
    {
        try
        {
            var cartDt = await _cartRepository.CreateUpdateCart(cartDto);
            _response.Result = cartDt;
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }

    [HttpPost("UpdateCart")]
    public async Task<object> UpdateCart(CartDto cartDto)
    {
        try
        {
            var cartDt = await _cartRepository.CreateUpdateCart(cartDto);
            _response.Result = cartDt;
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }

    [HttpPost("RemoveCart")]
    public async Task<object> RemoveCart([FromBody] int cartId)
    {
        try
        {
            var isSuccess = await _cartRepository.RemoveFromCart(cartId);
            _response.Result = isSuccess;
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }

    [HttpPost("ClearCart")]
    public async Task<object> ClearCart([FromBody] string userId)
    {
        try
        {
            var isSuccess = await _cartRepository.ClearCart(userId);
            _response.Result = isSuccess;
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }

    [HttpPost("ApplyCoupon")]
    public async Task<object> ApplyCoupon([FromBody] CartDto cartDto)
    {
        try
        {
            var isSuccess = await _cartRepository.ApplyCoupon(cartDto.CartHeader.UserId, cartDto.CartHeader.CouponCode);
            _response.Result = isSuccess;
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }

    [HttpPost("RemoveCoupon")]
    public async Task<object> RemoveCoupon([FromBody] string userId)
    {
        try
        {
            var isSuccess = await _cartRepository.RemoveCoupon(userId);
            _response.Result = isSuccess;
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }

    [HttpPost("Checkout")]
    public async Task<object> Checkout(CheckoutHeaderDto checkoutHeader)
    {
        try
        {
            var cartDto = await _cartRepository.GetCartByUserId(checkoutHeader.UserId);
            
            if (cartDto == null) return BadRequest();
            
            if (!string.IsNullOrEmpty(checkoutHeader.CouponCode))
            {
                var coupon = await _couponRepository.GetCoupon(checkoutHeader.CouponCode);
                if (Math.Abs(checkoutHeader.DiscountTotal - coupon.DiscountAmount) > 1e-5)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Coupon Price has changed, please confirm" };
                    _response.DisplayMessage = "Coupon Price has changed, please confirm";
                    return _response;
                }
            }
            
            checkoutHeader.CartDetails = cartDto.CartDetails;
            
            _rabbitMQCartMessageSender.SendMessage(checkoutHeader, "checkoutqueue");
            await _cartRepository.ClearCart(checkoutHeader.UserId);
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string> {ex.ToString()};
        }
        return _response;
    }
}