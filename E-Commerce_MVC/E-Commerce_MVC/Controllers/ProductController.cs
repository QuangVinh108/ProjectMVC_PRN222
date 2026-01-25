using BLL.DTOs;
using BLL.Helper;
using BLL.IService;
using DAL.Entities;
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
       
        
        public IActionResult Index(int? parentId, int? categoryId)
        {
            // 1. Lấy danh sách tất cả danh mục để vẽ giao diện (Menu/Tab)
            var allCategories = _categoryService.GetAll();
            ViewBag.Categories = allCategories;

            // 2. Lưu trạng thái hiện tại để View biết đang ở đâu
            ViewBag.CurrentParentId = parentId;
            ViewBag.CurrentCategoryId = categoryId;

            // 3. Gọi Service lấy sản phẩm
            var products = _productService.GetProductsForAdmin(parentId, categoryId);

            return View(products);
        }

        [HttpGet]
        public IActionResult Create(int? parentId = null)
        {
            // 1. Gọi Service 1 lần duy nhất để lấy toàn bộ danh sách DTO
            var allCategories = _categoryService.GetAll();

            // 2. Logic lọc danh mục
            IEnumerable<CategoryDTO> categoriesForDropdown;

            if (parentId.HasValue)
            {
                // Nếu đang đứng ở danh mục cha (VD: Điện thoại), chỉ hiện danh mục con của nó (iPhone, Samsung)
                categoriesForDropdown = allCategories.Where(c => c.ParentId == parentId);
            }
            else
            {
                // Nếu vào trực tiếp, hiện tất cả danh mục con (cấp 2)
                categoriesForDropdown = allCategories.Where(c => c.ParentId != null);
            }

            // 3. Đẩy dữ liệu sang View
            ViewBag.Categories = new SelectList(categoriesForDropdown, "CategoryId", "CategoryName");

            // Lưu lại parentId để View biết đường xử lý nút "Quay lại"
            ViewBag.ReturnParentId = parentId;

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Lưu ý: Tham số returnParentId phải khớp tên với asp-route-returnParentId trong View
        public async Task<IActionResult> Create(CreateProductViewModel model, int? returnParentId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // --- XỬ LÝ UPLOAD ẢNH ---
                    if (model.ImageFile != null)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }
                        model.Image = "/images/products/" + uniqueFileName;
                    }

                    // --- GỌI SERVICE ---
                    _productService.Create(model);

                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";

                    // --- REDIRECT VỀ ĐÚNG CHỖ CŨ ---
                    // Trả về Index với tham số parentId cũ để nó load lại đúng danh mục cha
                    return RedirectToAction("Index", new { parentId = returnParentId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }

            // --- XỬ LÝ KHI CÓ LỖI (Validation Fail) ---
            // (Phải load lại Dropdown giống hệt bên GET để không bị mất dữ liệu)

            var allCats = _categoryService.GetAll(); // Trả về List<CategoryDTO>
            IEnumerable<CategoryDTO> catsForDropdown; // SỬA: Phải dùng DTO, không dùng Entity

            if (returnParentId.HasValue)
            {
                catsForDropdown = allCats.Where(c => c.ParentId == returnParentId);
            }
            else
            {
                catsForDropdown = allCats.Where(c => c.ParentId != null);
            }

            // Gán lại SelectList (quan trọng: tham số thứ 4 là model.CategoryId để giữ giá trị user đã chọn)
            ViewBag.Categories = new SelectList(catsForDropdown, "CategoryId", "CategoryName", model.CategoryId);

            // Gán lại ReturnParentId để form tiếp tục giữ giá trị này nếu user submit lại lần nữa
            ViewBag.ReturnParentId = returnParentId;

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id, int? parentId) // 1. Nhận thêm parentId từ trang Index gửi sang
        {
            var product = _productService.GetById(id);
            if (product == null) return NotFound();

            // Lấy danh sách danh mục
            var categories = _categoryService.GetAll();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", product.CategoryId);

            // 2. Lưu lại ID danh mục cha để View biết đường "Quay lại"
            ViewBag.ReturnParentId = parentId;

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 3. Nhận lại returnParentId từ Form (Action form phải có asp-route-returnParentId)
        public async Task<IActionResult> Edit(CreateProductViewModel model, int? returnParentId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // --- XỬ LÝ ẢNH (Logic cũ của bạn giữ nguyên) ---
                    var existingProduct = _productService.GetById(model.ProductId);

                    // Mặc định giữ ảnh cũ
                    model.Image = existingProduct.Image;

                    if (model.ImageFile != null)
                    {
                        // Upload ảnh mới
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }

                        model.Image = "/images/products/" + uniqueFileName;

                        // Xóa ảnh cũ (Dọn rác)
                        if (!string.IsNullOrEmpty(existingProduct.Image))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.Image.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                    }
                    // ------------------------------------------------

                    // 4. Gọi Service cập nhật
                    _productService.Update(model);

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";

                    // 5. QUAN TRỌNG: Redirect về Index kèm theo parentId cũ
                    // Nếu returnParentId = 1 (Điện thoại), nó sẽ load lại trang Điện thoại
                    return RedirectToAction("Index", new { parentId = returnParentId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            // --- XỬ LÝ KHI LỖI (Validation Fail) ---
            var categories = _categoryService.GetAll();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", model.CategoryId);

            // 6. Phục hồi lại ReturnParentId để form không bị mất trạng thái này
            ViewBag.ReturnParentId = returnParentId;

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

            var categories = _categoryService.GetAll()
                                             .Select(c => c.CategoryName)
                                             .ToList();

            // 2. Truyền danh sách này vào hàm AnalyzeImageAsync
            var result = await _geminiHelper.AnalyzeImageAsync(file, categories);

            if (result != null)
            {
                return Ok(new { success = true, data = result });
            }

            return BadRequest(new { success = false, message = "Không thể phân tích ảnh" });
        }
    }
}
