using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace E_Commerce_MVC.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IUserService _userService;

        public CartController(ICartService cartService, IUserService userService)
        {
            _cartService = cartService;
            _userService = userService;
        }

        private int GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("id")?.Value;

            if (!string.IsNullOrEmpty(id) && int.TryParse(id, out var userId))
                return userId;

            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var user = _userService.GetUserByUserName(username)
                                       .GetAwaiter().GetResult();
                if (user != null) return user.UserId;
            }

            throw new Exception("Vui lòng đăng nhập");
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart(GetCurrentUserId());
            return View(cart);
        }

        [HttpGet]
        public IActionResult Count()
        {
            var cart = _cartService.GetCart(GetCurrentUserId());
            var count = cart.CartItems.Sum(i => i.Quantity);
            return Json(count);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, int quantity = 1)
        {
            var userId = GetCurrentUserId();
            _cartService.AddItem(userId, productId, quantity);

            var count = _cartService.GetCart(userId)
                                    .CartItems.Sum(i => i.Quantity);

            return Json(new { success = true, count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int cartItemId, int quantity)
        {
            _cartService.UpdateItem(cartItemId, quantity);

            var userId = GetCurrentUserId();
            var count = _cartService.GetCart(userId)
                                    .CartItems.Sum(i => i.Quantity);

            return Json(new
            {
                success = true,
                quantity,
                count
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int cartItemId)
        {
            _cartService.RemoveItem(cartItemId);
            return RedirectToAction(nameof(Index));
        }
    }
}
