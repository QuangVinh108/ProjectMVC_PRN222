using BLL.IService;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_MVC.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        public IActionResult Index()
        {
            int userId = 1;
            var cart = _cartService.GetCart(userId);
            return View(cart);
        }

        [HttpPost]
        public IActionResult Add(int productId, int quantity = 1)
        {
            int userId = int.Parse(User.FindFirst("UserId")!.Value);
            _cartService.AddItem(userId, productId, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(int cartItemId, int quantity)
        {
            _cartService.UpdateItem(cartItemId, quantity);
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int cartItemId)
        {
            _cartService.RemoveItem(cartItemId);
            return RedirectToAction("Index");
        }
    }
}
