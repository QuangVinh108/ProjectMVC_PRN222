using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace E_Commerce_MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        
        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        private int GetCurrentUserId()
        {
            var claimUserId = User.FindFirst("UserId")?.Value ??
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(claimUserId, out int userId))
                return userId;

            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        [HttpGet("count")]
        public async Task<IActionResult> Count()
        {
            try
            {
                var userId = GetCurrentUserId();
                if(userId == 0) return Json(0);

                var count = await _wishlistService.GetWishlistByUserAsync(userId);
                return Json(count);
            }
            catch
            {
                return Json(0);
            }
        }


        //GET: /Whishlist/Index - Show wishlist items
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if(userId == 0)
                return RedirectToAction("Login", "Account");

            var wishlist = await _wishlistService.GetWishlistByUserAsync(userId);
            var wishlists = wishlist != null
        ? new List<Wishlist> { wishlist }
        : new List<Wishlist>();
            return View(wishlists);
        }

        //GET: /Wishlist/Details/5 - Show details of a specific wishlist item
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var wishlist = await _wishlistService.GetWishlistByIdAsync(id.Value);
            if(wishlist == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (wishlist.UserId != userId)
            {
                return Forbid();
            }

            return View(wishlist);
        }


        //GET: /Wishlist/Create - Show form to create a new wishlist item
        [HttpGet("create")]
        public IActionResult Create()
        {
            var userId = GetCurrentUserId();
            if(userId == 0)
                return RedirectToAction("Login", "Account");

            ViewBag.UserId = userId;
            return View(new Wishlist { UserId = userId });
        }

        //POST: /Wishlist/Create - Handle form submission to create a new wishlist item
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Wishlist wishlist)
        {
            if(!ModelState.IsValid) return View(wishlist);

            wishlist.UserId = GetCurrentUserId();
            await _wishlistService.CreateWishlistAsync(wishlist);
            return RedirectToAction(nameof(Index));
        }

        //GET: /Wishlist/Edit/5 - Show form to edit an existing wishlist item
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var wishlist = await _wishlistService.GetWishlistByIdAsync(id.Value);
            if (wishlist == null)
            {
                return NotFound();
            }
            var userId = GetCurrentUserId();
            if (wishlist.UserId != userId)
            {
                return Forbid();
            }
            return View(wishlist);
        }

        //POST: /Wishlist/Edit/5 - Handle form submission to update an existing wishlist item
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Wishlist wishlist)
        {
            if (id != wishlist.WishlistId)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (wishlist.UserId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _wishlistService.UpdateWishlistAsync(wishlist);
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    return View(wishlist);
                }
            }
            return View(wishlist);
        }

        //GET: /Wishlist/Delete/5 - Show confirmation page to delete a wishlist item
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wishlist = await _wishlistService.GetWishlistByIdAsync(id.Value);
            if(wishlist == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (wishlist.UserId != userId)
            {
                return Forbid();
            }

            return View(wishlist);
        }

        //POST: /Wishlist/Delete/5 - Handle confirmation to delete a wishlist item
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var wishlist = await _wishlistService.GetWishlistByIdAsync(id);
            if (wishlist == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            if (wishlist.UserId != userId)
            {
                return Forbid();
            }

            await _wishlistService.DeleteWishlistAsync(id);
            return RedirectToAction(nameof(Index));
        }

        //[HttpPost("add/{productId}")]  // ← 5. Thêm API này
        //public async Task<IActionResult> AddProduct(int productId)
        //{
        //    var userId = GetCurrentUserId();
        //    if (userId == 0) return Unauthorized();

        //    await _wishlistService.AddProductAsync(userId, productId);
        //    return Ok(new { message = "Added to wishlist" });
        //}


    }
}
