using BLL.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Repositories.IRepository;
using Services.IService;

namespace E_Commerce_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        // Constructor Inject Service
        public UserController(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        public IActionResult Index()
        {
            // Gọi qua Service
            var users = _userService.GetAllUsers();
            return View(users);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            // Gọi qua Service
            _userService.DeleteUser(id);
            return RedirectToAction("Index");
        }

        // 1. GET: Hiển thị form thêm mới
        [HttpGet]
        public IActionResult Create()
        {
            var roles = _roleService.GetAllRoles();
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName"); // "Value", "Text"

            return View();
        }

        // 2. POST: Xử lý thêm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _userService.CreateUser(model);
                    TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            // Nếu lỗi validation, load lại dropdown role và trả về view cũ
            var roles = _roleService.GetAllRoles();
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", model.RoleId);

            return View(model);
        }

        // 1. GET: Hiển thị form sửa
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            // Chuyển đổi từ Entity sang ViewModel để hiển thị lên form
            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Address = user.Address,
                RoleId = user.RoleId,
                IsActive = user.IsActive
            };

            // Load danh sách Role cho dropdown
            var roles = _roleService.GetAllRoles();
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", model.RoleId);

            return View(model);
        }

        // 2. POST: Thực hiện lưu thay đổi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _userService.UpdateUser(model);
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                }
            }

            // Nếu lỗi thì load lại Role để không bị mất dropdown
            var roles = _roleService.GetAllRoles();
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", model.RoleId);

            return View(model);
        }
    }
}
