using BLL.IService;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_MVC.Controllers
{
    public class ShopController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ShopController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        public IActionResult Index(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder)
        {
            if (string.IsNullOrEmpty(sortOrder))
            {
                sortOrder = "price_desc";
            }

            var products = _productService.GetFilteredProducts(searchTerm, categoryId, minPrice, maxPrice, sortOrder);

            var categories = _categoryService.GetAll();
            ViewBag.Categories = categories;

            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentSort = sortOrder; // Để dropdown biết đang sort theo kiểu nào

            return View(products);
        }

        // Xem chi tiết sản phẩm
        public IActionResult Details(int id)
        {
            // SỬA: Gọi hàm GetDetail thay vì GetById
            var product = _productService.GetDetail(id);

            // Kiểm tra null hoặc sản phẩm bị ẩn
            if (product == null || product.Status != 1)
            {
                return NotFound();
            }

            // Bây giờ biến 'product' là kiểu ProductViewModel -> Khớp với View
            return View(product);
        }
    }
}
