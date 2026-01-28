using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_MVC.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        
        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _wishlistService.GetUserWishlistAsync();
            var model = result.IsSuccess ? result.Data : new List<WishlistProductDTO>();

            if (!result.IsSuccess)
                TempData["Error"] = result.Message;

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Count()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(0);
            var result = await _wishlistService.GetWishlistCountAsync();
            return Json(result.IsSuccess ? result.Data : 0);
        }

        [HttpGet("Wishlist/Add/{productId}")]
        public async Task<IActionResult> Add(int productId, string? note = null)
        {
            var result = await _wishlistService.AddToWishlistAsync(productId, note);

            string message = result.IsSuccess
                ? (result.Message ?? "Success")
                : (result.Message ?? string.Join("; ", result.Errors));

            return Json(new
            {
                success = result.IsSuccess,
                message = result.Message ?? string.Join(", ", result.Errors)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int wishlistProductId)
        {
            var result = await _wishlistService.RemoveFromWishlistAsync(wishlistProductId);
            if (!result.IsSuccess) return BadRequest();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            await _wishlistService.ClearWishlistAsync();
            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        [HttpGet("/Wishlist/Check/{productId}")]
        public async Task<IActionResult> Check(int productId)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Json(new { success = true, isInWishlist = false });

            bool isInWishlist = await _wishlistService.IsProductInWishlistAsync(productId);
            return Json(new { success = true, isInWishlist });
        }



        [HttpPost("/Wishlist/Toggle/{productId}")]
        public async Task<IActionResult> Toggle(int productId)
        {
            var result = await _wishlistService.ToggleWishlistAsync(productId);
            return Json(new
            {
                success = result.IsSuccess,
                isAdded = result.Data,  // true=ADD, false=REMOVE
                message = result.Message
            });
        }

    }
}
