﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace Mango.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly ICartService _cartService;

    public HomeController(ILogger<HomeController> logger, IProductService productService, ICartService cartService)
    {
        _logger = logger;
        _productService = productService;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        var list = new List<ProductDto>();
        var response = await _productService.GetAllProductsAsync<ResponseDto>("");
        if (response != null && response.IsSuccess)
        {
            list = JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response.Result));
        }
        return View(list);
    }

    [Authorize]
    public async Task<IActionResult> Details(int productId)
    {
        var model = new ProductDto();
        var response = await _productService.GetProductByIdAsync<ResponseDto>(productId, "");
        if (response != null && response.IsSuccess)
        {
            model = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
        }
        return View(model);
    }

    [HttpPost]
    [ActionName("Details")]
    [Authorize]
    public async Task<IActionResult> DetailsPost(ProductDto productDto)
    {
        var cartDto = new CartDto
        {
            CartHeader = new CartHeaderDto
            {
                UserId = User.Claims.FirstOrDefault(u => u.Type == "sub")?.Value
            }
        };

        var cartDetails = new CartDetailsDto
        {
            Count = productDto.Count,
            ProductId = productDto.ProductId
        };

        var response = await _productService.GetProductByIdAsync<ResponseDto>(productDto.ProductId, "");

        if (response != null && response.IsSuccess)
        {
            cartDetails.Product = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
        }

        var cartDetailsDtos = new List<CartDetailsDto> {cartDetails};
        cartDto.CartDetails = cartDetailsDtos;

        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var addToCartResponse = await _cartService.AddToCartAsync<ResponseDto>(cartDto, accessToken);

        if (addToCartResponse != null && addToCartResponse.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(productDto);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }

    [Authorize]
    public async Task<IActionResult> Login()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Logout()
    {
        return SignOut("Cookies", "oidc");
    }
}