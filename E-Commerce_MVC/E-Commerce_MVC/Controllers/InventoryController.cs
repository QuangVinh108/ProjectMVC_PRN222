using DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using BLL.IService;
using BLL.DTOs.InventoryDTOs;
using Microsoft.AspNetCore.Authorization;

namespace E_Commerce_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public async Task<IActionResult> Index(int pageIndex = 1, int pageSize = 10, string? search = null)
        {
            try
            {
                var query = new QueryInventoryDTO
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Search = search ?? ""
                };
                var result = await _inventoryService.GetAllAsync(query);

                var model = result.IsSuccess ? result.Data : new PagedResult<InventoryDto>();
                ViewBag.Search = search;
                ViewBag.PageSize = pageSize;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new PagedResult<InventoryDto>());
            }
        }

        public IActionResult Create()
        {
            return View(new CreateInventoryDto());
        }

        //[HttpGet("api/getall")]
        //public async Task<IActionResult> GetAll([FromQuery] QueryInventoryDTO query)
        //{
        //    var result = await _inventoryService.GetAllAsync(query);
        //    if(!result.IsSuccess)
        //        return BadRequest(result);

        //    return Ok(result.Data);
        //}

        //public async Task<IActionResult> GetByProductId(int productId)
        //{
        //    var result = await _inventoryService.GetByProductIdAsync(productId);
        //    if (!result.IsSuccess)
        //    {
        //        return result.Errors.Contains("Inventory not found") 
        //            ? NotFound(result)       // 404
        //            : BadRequest(result);    // 400
        //    }
        //    return Ok(result.Data);
        //}

        //[HttpGet("stock/{productId}")]
        //public async Task<IActionResult> GetStock(int productId)
        //{
        //    var result = await _inventoryService.GetAvailableStockAsync(productId);
        //    if(!result.IsSuccess)
        //        return BadRequest(result);

        //    return Ok(result.Data);
        //}

        //[HttpGet("has-stock/{productId}")]
        //public async Task<ActionResult> HasStock(int productId, [FromQuery] int quantity = 1)
        //{
        //    var result = await _inventoryService.HasStockAsync(productId, quantity);
        //    if(!result.IsSuccess)
        //        return BadRequest(result);

        //    return Ok(result.Data);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInventoryDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _inventoryService.CreateAsync(dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Message ?? string.Join(", ", result.Errors));
                return View(dto);
            }

            TempData["Success"] = "Tạo kho hàng thành công!";
            return RedirectToAction(nameof(Index));
        }

        //[HttpPut("{productId}")]
        //public async Task<IActionResult> Update(int productId, [FromBody] UpdateInventoryDto inventory)
        //{
        //    if(!ModelState.IsValid)
        //        return BadRequest("Productid mismatch or invalid data");

        //    var result = await _inventoryService.UpdateAsync(productId, inventory);
        //    if (!result.IsSuccess)
        //    {
        //        return result.Errors.Contains("Inventory not found")
        //            ? NotFound(result)       // 404
        //            : BadRequest(result);    // 400
        //    }

        //    return Ok(result.Data);
        //}

        public async Task<IActionResult> Edit(int productId)
        {
            var result = await _inventoryService.GetByProductIdAsync(productId);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["Error"] = "Không tìm thấy kho hàng";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ProductName = result.Data.ProductName;
            ViewBag.ProductId = productId; // Đã có

            var editDto = new UpdateInventoryDto
            {
                ProductId = productId,  // Thêm dòng này
                Quantity = result.Data.Quantity,
                Warehouse = result.Data.Warehouse
            };

            return View(editDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int productId, UpdateInventoryDto dto)
        {
            // Set lại ProductName khi có lỗi
            var invResult = await _inventoryService.GetByProductIdAsync(productId);
            ViewBag.ProductName = invResult.IsSuccess && invResult.Data != null
                ? invResult.Data.ProductName
                : $"Product #{productId}";

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _inventoryService.UpdateAsync(productId, dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Message ?? string.Join(", ", result.Errors));
                return View(dto);
            }

            TempData["Success"] = "Cập nhật kho hàng thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int productId)
        {
            var result = await _inventoryService.DeleteAsync(productId);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message ?? "Xóa kho hàng thất bại.";
            }
            else
            {
                TempData["Success"] = "Xóa kho hàng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
