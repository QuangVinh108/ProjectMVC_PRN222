using BLL.DTOs;
using BLL.Helper;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace E_Commerce_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly GeminiHelper _geminiHelper;

        public ProductController(IProductService productService, ICategoryService categoryService, IWebHostEnvironment webHostEnvironment, GeminiHelper geminiHelper)
        {
            _productService = productService;
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;
            _geminiHelper = geminiHelper;
        }

        public IActionResult Index()
        {
            var products = _productService.GetAll();
            return View(products);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            // GỌI API TỪ CATEGORY SERVICE
            var categories = _categoryService.GetAll();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");

            return View();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model) // Thêm async Task
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 3. XỬ LÝ UPLOAD ẢNH
                    if (model.ImageFile != null)
                    {
                        // Tạo tên file duy nhất để tránh trùng
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;

                        // Đường dẫn lưu file: wwwroot/images/products
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

                        // Tạo thư mục nếu chưa có
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Lưu file vào server
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }

                        // Gán đường dẫn tương đối vào model để lưu xuống DB
                        model.Image = "/images/products/" + uniqueFileName;
                    }

                    // 4. Gọi Service (Service chỉ việc lưu string Image vào DB)
                    _productService.Create(model);

                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            var categories = _categoryService.GetAll();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", model.CategoryId);
            return View(model);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = _productService.GetById(id);
            if (product == null) return NotFound();

            // GỌI API TỪ CATEGORY SERVICE
            var categories = _categoryService.GetAll();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", product.CategoryId);

            return View(product);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateProductViewModel model) // Đổi thành async Task
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Lấy thông tin sản phẩm cũ từ DB để lấy đường dẫn ảnh cũ
                    var existingProduct = _productService.GetById(model.ProductId);

                    // Mặc định gán ảnh cũ trước
                    model.Image = existingProduct.Image;

                    // 2. Kiểm tra nếu người dùng có chọn ảnh mới
                    if (model.ImageFile != null)
                    {
                        // Tạo tên file mới
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Lưu file mới
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }

                        // Gán đường dẫn mới
                        model.Image = "/images/products/" + uniqueFileName;

                        // (Tùy chọn) Xóa file ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingProduct.Image))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.Image.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                    }

                    // 3. Gọi Service cập nhật
                    _productService.Update(model);

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            var categories = _categoryService.GetAll();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", model.CategoryId);
            return View(model);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            _productService.Delete(id);
            return RedirectToAction("Index");
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AnalyzeProductImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn ảnh" });

            var result = await _geminiHelper.AnalyzeImageAsync(file);

            if (result != null)
            {
                return Ok(new { success = true, data = result });
            }

            return BadRequest(new { success = false, message = "Không thể phân tích ảnh" });
        }
    }
}
