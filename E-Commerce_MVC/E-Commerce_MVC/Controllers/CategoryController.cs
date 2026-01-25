using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_MVC.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: Admin/Category/Create
        public IActionResult Create(int? parentId)
        {
            ViewBag.ParentId = parentId;
            if (parentId != null)
            {
                // Lấy tên cha để hiển thị cho user yên tâm
                var parentCat = _categoryService.GetAll().FirstOrDefault(c => c.CategoryId == parentId);
                ViewBag.ParentName = parentCat?.CategoryName;
            }
            return View();
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryDTO model)
        {
            if (ModelState.IsValid)
            {
                _categoryService.Add(model); // Gọi Service thêm mới

                TempData["SuccessMessage"] = "Thêm danh mục thành công!";

                // Nếu là danh mục gốc -> Về trang quản lý sản phẩm gốc
                if (model.ParentId == null)
                {
                    return RedirectToAction("Index", "Product");
                }

                // Nếu là danh mục con -> Về trang cha của nó
                return RedirectToAction("Index", "Product", new { parentId = model.ParentId });
            }
            return View(model);
        }
    }
}
