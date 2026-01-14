using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_MVC.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
