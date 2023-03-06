using AutoMapper;
using Mango.Services.ShoppingCartAPI.DbContexts;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Repository;

public class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _db;
    private IMapper _mapper;

    public CartRepository(ApplicationDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<CartDto> GetCartByUserId(string userId)
    {
        // создаем Cart и в его CartHeader копируем данные из нашей соответствующей (по UserId) локальной копии
        var cart = new Cart
        {
            CartHeader = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId)
        };

        // в его CartDetails тоже копируем данные из нашей соответствующей (по CartHeaderId) локальной копии
        // а метод Include позволяет подсоединять к объекту связанные данные из другой таблицы 
        cart.CartDetails = _db.CartDetails
            .Where(u => u.CartHeaderId == cart.CartHeader.CartHeaderId)
            .Include(u => u.Product);

        return _mapper.Map<CartDto>(cart);
    }

    public async Task<CartDto> CreateUpdateCart(CartDto cartDto)
    {
        var cart = _mapper.Map<Cart>(cartDto);

        // мы вызываем этот метод когда пользователь переходит на страницу товара, а потом добавляет его в корзину
        // т.к. товар в этом случае всегда один то для удобства создадим переменную cartDetailDto
        var cartDetail = cart.CartDetails.FirstOrDefault();

        // проверяем существует ли такой товар, который мы хотим добавить в корзину, в нашей локальной базе данных товаров
        // мы имеем эту локальную базу, чтобы уменьшить количество обращений за описанием товара к базе товаров сервиса ProductAPI
        var prodInDb =
            await _db.Products.FirstOrDefaultAsync(u => u.ProductId == cartDto.CartDetails.FirstOrDefault().ProductId);

        // если в нашей копии еще нет такого товара, то добавляем этот товар в нашу копию
        if (prodInDb == null)
        {
            _db.Products.Add(cartDetail.Product);
            await _db.SaveChangesAsync();
        }

        // делаем попытку на основе UserId получить CartHeader, хранящийся в нашей локальной базе данных
        // при применении метода AsNoTracking() данные, возвращаемые из запроса, не кэшируются
        var cartHeaderFromDb =
            await _db.CartHeaders.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == cart.CartHeader.UserId);

        if (cartHeaderFromDb == null)
        {
            // если в нашей локальной копии CartHeader'ов еще нет такого CartHeader'a, то добавляем его
            _db.CartHeaders.Add(cart.CartHeader);
            await _db.SaveChangesAsync();
            // после сохранения в _db для CartHeader там будет создан новый CartHeaderId
            // используя этот новый CartHeaderId заполним объект СartDetail и сохраним в локальную копию
            cartDetail.CartHeaderId = cart.CartHeader.CartHeaderId;
            // чтобы в момент выполнения _db.CartDetails.Add(cartDetail) EntityFramework также не пыталась
            // добавить уже добавленный Product с таким же идентификатором, сделаем его null 
            cartDetail.Product = null;
            _db.CartDetails.Add(cartDetail);
            await _db.SaveChangesAsync();
        }
        // если в нашей локальной копии CartHeader'ов уже есть такой CartHeader
        else
        {
            // делаем попытку на основе ProductId получить СartDetail, хранящийся в нашей локальной базе данных
            // при применении метода AsNoTracking() данные, возвращаемые из запроса, не кэшируются
            var cartDetailsFromDb = await _db.CartDetails.AsNoTracking().FirstOrDefaultAsync(u =>
                u.ProductId == cartDetail.ProductId && u.CartHeaderId == cartHeaderFromDb.CartHeaderId);

            if (cartDetailsFromDb == null)
            {
                // если у нас нету СartDetail с таким же ProductId, то добавляем в локальную базу такой CartDetail
                cartDetail.CartHeaderId = cartHeaderFromDb.CartHeaderId;
                // чтобы в момент выполнения _db.CartDetails.Add(cartDetail) EntityFramework также не пыталась
                // добавить уже добавленный Product с таким же идентификатором, сделаем его null
                cartDetail.Product = null;
                _db.CartDetails.Add(cartDetail);
                await _db.SaveChangesAsync();
            }
            else
            {
                // если у нас в локальной базе уже есть CartDetail с нужным Product'ом
                // то нам нужно только добавить к старому его количеству свежепоступившее значение нового количества
                cartDetail.Product = null;
                cartDetail.Count += cartDetailsFromDb.Count;
                cartDetail.CartDetailsId = cartDetailsFromDb.CartDetailsId;
                cartDetail.CartHeaderId = cartDetailsFromDb.CartHeaderId;
                _db.CartDetails.Update(cartDetail);
                await _db.SaveChangesAsync();
            }
        }

        return _mapper.Map<CartDto>(cart);
    }

    public async Task<bool> RemoveFromCart(int cartDetailsId)
    {
        try
        {
            var cartDetails = await _db.CartDetails.FirstOrDefaultAsync(u => u.CartDetailsId == cartDetailsId);
            // когда мы будем удалять товар из корзины по CartDetailsId, то полезно знать, остались ли там еще какие-то товары
            // если там пусто, то надо удалить и заголовок CartHeader, поэтому вычислим totalCountOfCartItems
            var totalCountOfCartItems = _db.CartDetails.Count(u => u.CartHeaderId == cartDetails.CartHeaderId);

            _db.CartDetails.Remove(cartDetails);

            if (totalCountOfCartItems == 1)
            {
                var cartHeaderToRemove =
                    await _db.CartHeaders.FirstOrDefaultAsync(u => u.CartHeaderId == cartDetails.CartHeaderId);

                _db.CartHeaders.Remove(cartHeaderToRemove);
            }
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public async Task<bool> ClearCart(string userId)
    {
        var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
        if (cartHeaderFromDb != null)
        {
            _db.CartDetails
                .RemoveRange(_db.CartDetails.Where(u => u.CartHeaderId == cartHeaderFromDb.CartHeaderId));
            _db.CartHeaders.Remove(cartHeaderFromDb);
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> ApplyCoupon(string userId, string couponCode)
    {
        var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
        cartHeaderFromDb.CouponCode = couponCode;
        _db.CartHeaders.Update(cartHeaderFromDb);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveCoupon(string userId)
    {
        var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
        cartHeaderFromDb.CouponCode = null;
        _db.CartHeaders.Update(cartHeaderFromDb);
        await _db.SaveChangesAsync();
        return true;
    }
}