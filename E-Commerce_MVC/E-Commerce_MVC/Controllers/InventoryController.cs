using DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using BLL.IService;
using BLL.DTOs.InventoryDTOs;

namespace E_Commerce_MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }
        //public IActionResult Index()
        //{
        //    return View();
        //}

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryInventoryDTO query)
        {
            var result = await _inventoryService.GetAllAsync(query);
            if(!result.IsSuccess)
                return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var result = await _inventoryService.GetByProductIdAsync(productId);
            if (!result.IsSuccess)
            {
                return result.Errors.Contains("Inventory not found") 
                    ? NotFound(result)       // 404
                    : BadRequest(result);    // 400
            }
            return Ok(result.Data);
        }

        [HttpGet("stock/{productId}")]
        public async Task<IActionResult> GetStock(int productId)
        {
            var result = await _inventoryService.GetAvailableStockAsync(productId);
            if(!result.IsSuccess)
                return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpGet("has-stock/{productId}")]
        public async Task<ActionResult> HasStock(int productId, [FromQuery] int quantity = 1)
        {
            var result = await _inventoryService.HasStockAsync(productId, quantity);
            if(!result.IsSuccess)
                return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInventoryDto inventory)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _inventoryService.CreateAsync(inventory);
            if(!result.IsSuccess)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetByProductId), new { productId = result.Data!.ProductId }, result.Data);
        }

        [HttpPut("{productId}")]
        public async Task<IActionResult> Update(int productId, [FromBody] UpdateInventoryDto inventory)
        {
            if(!ModelState.IsValid)
                return BadRequest("Productid mismatch or invalid data");

            var result = await _inventoryService.UpdateAsync(productId, inventory);
            if (!result.IsSuccess)
            {
                return result.Errors.Contains("Inventory not found")
                    ? NotFound(result)       // 404
                    : BadRequest(result);    // 400
            }

            return Ok(result.Data);
        }

        //[HttpPatch("{productId}/quantity")]
        //public async Task<IActionResult> UpdateQuantity(int productId, [FromBody] int quantity)
        //{
        //    var result = await _inventoryService.UpdateQuantityAsync(productId, quantity);
        //    if(!result.IsSuccess)
        //        return BadRequest(result);

        //    return NoContent();
        //}

        [HttpDelete("{productId}")]
        public async Task<IActionResult> Delete(int productId)
        {
            var result = await _inventoryService.DeleteAsync(productId);
            if (!result.IsSuccess)
            {
                return result.Errors.Contains("Inventory not found")
                    ? NotFound(result)       // 404
                    : BadRequest(result);    // 400
            }

            return Ok(result.Data);
        }
    }
}
