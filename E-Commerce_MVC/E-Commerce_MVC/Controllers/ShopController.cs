using BLL.IService;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_MVC.Controllers
{
    public class ShopController : Controller
    {
        private readonly IProductService _productService;

        public ShopController(IProductService productService)
        {
            _productService = productService;
        }

        // Khách hàng xem dạng Lưới (Grid)
        public IActionResult Index(string searchTerm)
        {
            // 1. Lấy tất cả sản phẩm Active
            var products = _productService.GetAll().Where(p => p.Status == 1);

            // 2. Xử lý tìm kiếm (Nếu có từ khóa)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Chuyển về chữ thường để tìm không phân biệt hoa thường
                products = products.Where(p => p.ProductName.ToLower().Contains(searchTerm.ToLower()));

                // Lưu lại từ khóa để hiển thị lại trên thanh tìm kiếm
                ViewBag.CurrentFilter = searchTerm;
            }

            // 3. Trả về list đã lọc
            return View(products.ToList());
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
