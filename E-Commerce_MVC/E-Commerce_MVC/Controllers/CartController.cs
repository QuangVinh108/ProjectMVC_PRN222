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
            // Try multiple claim types commonly used by Identity / JWT
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("id")?.Value;

            if (!string.IsNullOrEmpty(id) && int.TryParse(id, out var parsedId))
                return parsedId;

            // Fallback: try lookup by username if available
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                // IUserService exposes async; call synchronously as a fallback
                var user = _userService.GetUserByUserName(username).GetAwaiter().GetResult();
                if (user != null) return user.UserId;
            }

            throw new System.Exception("Vui lòng đăng nhập để tiếp tục");
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var userId = GetCurrentUserId();
            var cart = _cartService.GetCart(userId);
            return View(cart);
        }

        // GET: /Cart/Count  -> returns JSON integer
        [HttpGet]
        public IActionResult Count()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = _cartService.GetCart(userId);
                var count = cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
                return Json(count);
            }
            catch
            {
                return Json(0);
            }
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, int quantity = 1)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (quantity < 1) quantity = 1;

                _cartService.AddItem(userId, productId, quantity);

                // If AJAX request, return JSON with updated count
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var cart = _cartService.GetCart(userId);
                    var count = cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
                    return Json(new { success = true, count });
                }

                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng";
                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, error = ex.Message });

                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public IActionResult Update(int cartItemId, int quantity)
        {
            _cartService.UpdateItem(cartItemId, quantity);
            return RedirectToAction("Index");
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int cartItemId)
        {
            _cartService.RemoveItem(cartItemId);
            return RedirectToAction("Index");
        }
    }
}
