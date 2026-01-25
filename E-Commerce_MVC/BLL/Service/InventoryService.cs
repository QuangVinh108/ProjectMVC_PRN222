using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.InventoryDTOs;
using BLL.Helper;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Service
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IProductRepository _productRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IOrderItemRepository _orderItemRepo;
        private readonly ILogger<InventoryService> _logger;
        public InventoryService(IInventoryRepository inventoryRepo, IProductRepository productRepo, IOrderRepository orderRepository, IOrderItemRepository orderItemRepository, ILogger<InventoryService> logger)
        {
            _inventoryRepo = inventoryRepo;
            _productRepo = productRepo;
            _orderRepo = orderRepository;
            _orderItemRepo = orderItemRepository;
            _logger = logger;
        }
        public async Task<GenericResult<InventoryDto?>> GetByProductIdAsync(int productId)
        {
            try
            {
                if(productId <= 0)
                    return GenericResult<InventoryDto?>.Failure("Invalid product ID");

                var inventory = await _inventoryRepo.GetByProductIdAsync(productId);
                if(inventory == null)
                    return GenericResult<InventoryDto?>.Failure("Inventory not found");

                var dto = MapToDto(inventory);
                return GenericResult<InventoryDto?>.Success(dto);
            }
            catch(Exception ex)
            {
                return GenericResult<InventoryDto?>.Failure($"Error getting inventory: {ex.Message}");
            }
        }

        public async Task<GenericResult<PagedResult<InventoryDto>>> GetAllAsync(QueryInventoryDTO query)
        {
            try
            {
                var pageIndex = Math.Max(1, query.PageIndex);
                var pageSize = Math.Min(100, Math.Max(1, query.PageSize));

                var sortBy = query.SortBy?.ToLower() ?? "productname";
                var isDescending = query.SortDirection?.ToLower() == "desc";

                var (items, totalCount) = await _inventoryRepo.GetPagedAsync(
                    query.Search,
                    sortBy,
                    isDescending,
                    pageIndex,
                    pageSize);

                var dtoItems = items.Select(MapToDto).ToList();

                var result = new PagedResult<InventoryDto>
                {
                    Items = dtoItems,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                return GenericResult<PagedResult<InventoryDto>>.Success(result);
            }
            catch(Exception ex)
            {
                return GenericResult<PagedResult<InventoryDto>>.Failure($"Error getting inventories: {ex.Message}");
            }
        }

        public async Task<GenericResult<InventoryDto>> CreateAsync(CreateInventoryDto dto)
        {
            try
            {
                if (dto.ProductId <= 0)
                    return GenericResult<InventoryDto>.Failure("ProductId is required");

                // ✅ KIỂM TRA PRODUCT TỒN TẠI
                var product =  _productRepo.GetProductById(dto.ProductId);
                if (product == null)
                    return GenericResult<InventoryDto>.Failure("Không tìm thấy sản phẩm với ID này");

                // Check duplicate
                var existing = await _inventoryRepo.GetByProductIdAsync(dto.ProductId);
                if (existing != null)
                    return GenericResult<InventoryDto>.Failure("Inventory đã tồn tại");

                var inventory = new Inventory
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    Warehouse = dto.Warehouse ?? string.Empty,
                };

                var createdInventory = await _inventoryRepo.CreateAsync(inventory);

                var createdDto = MapToDto(createdInventory);

                return GenericResult<InventoryDto>.Success(createdDto);
            }
            catch (Exception ex)
            {
                return GenericResult<InventoryDto>.Failure($"Lỗi tạo kho: {ex.Message}");
            }
        }


        public async Task<GenericResult<InventoryDto?>> UpdateAsync(int productId, UpdateInventoryDto dto)
        {
            try
            {
                if(productId <= 0)
                    return GenericResult<InventoryDto?>.Failure("Invalid product ID");

                var existing = await _inventoryRepo.GetByProductIdAsync(productId);
                if(existing == null)
                    return GenericResult<InventoryDto?>.Failure("Inventory not found");

                if (dto.Quantity.HasValue)
                    existing.Quantity = dto.Quantity.Value;

                if (!string.IsNullOrEmpty(dto.Warehouse))
                    existing.Warehouse = dto.Warehouse;
                existing.UpdatedAt = DateTime.UtcNow;

                var updated = await _inventoryRepo.UpdateAsync(existing);
                if (updated == null) return GenericResult<InventoryDto?>.Failure("Update failed");
                var updatedDto = MapToDto(updated);
                return GenericResult<InventoryDto?>.Success(updatedDto, "Inventory updated successfully");
            }
            catch(Exception ex)
            {
                return GenericResult<InventoryDto?>.Failure($"Error updating inventory: {ex.Message}");
            }
        }

        public async Task<GenericResult<bool>> DeleteAsync(int productId)
        {
            try
            {
                var result = await _inventoryRepo.DeleteAsync(productId);
                if(!result)
                    return GenericResult<bool>.Failure("Inventory not found");

                return GenericResult<bool>.Success(true, "Inventory deleted successfully");
            }
            catch(Exception ex)
            {
                return GenericResult<bool>.Failure($"Error deleting inventory: {ex.Message}");
            }
        }


        public async Task<GenericResult<bool>> UpdateQuantityAsync(int productId, int newQuantity)
        {
            try
            {
                if (productId <= 0)
                    return GenericResult<bool>.Failure("Invalid product ID");

                if (newQuantity < 0)
                    return GenericResult<bool>.Failure("Quantity cannot be negative");

                var result = await _inventoryRepo.UpdateQuantityAsync(productId, newQuantity);
                return GenericResult<bool>.Success(result, "Quantity updated successfully");
            }
            catch(Exception ex)
            {
                return GenericResult<bool>.Failure($"Error updating quantity: {ex.Message}");
            }
        }

        public async Task<GenericResult<bool>> HasStockAsync(int productId, int quantity)
        {
            try
            {
                if(productId <= 0)
                    return GenericResult<bool>.Failure("Invalid product ID");

                if (quantity <= 0)
                    return GenericResult<bool>.Success(true);

                var hasStock = await _inventoryRepo.HasStockAsync(productId, quantity);
                return GenericResult<bool>.Success(hasStock);
            }
            catch(Exception ex)
            {
                return GenericResult<bool>.Failure($"Error checking stock: {ex.Message}");
            }
        }

        public async Task<GenericResult<int>> GetAvailableStockAsync(int productId)
        {
            try
            {
                if(productId <= 0)
                    return GenericResult<int>.Failure("Invalid product ID");

                var quantity = await _inventoryRepo.GetQuantityAsync(productId);
                return GenericResult<int>.Success(quantity);
            }
            catch(Exception ex)
            {
                return GenericResult<int>.Failure($"Error getting available stock: {ex.Message}");
            }
        }

        private InventoryDto MapToDto(Inventory inventory)
        {
            return new InventoryDto
            {
                InventoryId = inventory.InventoryId,
                ProductId = inventory.ProductId,
                ProductName = inventory.Product?.ProductName ?? "Unknown",  
                ProductImage = inventory.Product?.Image ?? string.Empty,
                Quantity = inventory.Quantity,
                Warehouse = inventory.Warehouse,
                UpdatedAt = inventory.UpdatedAt
            };
        }

        public async Task<GenericResult<bool>> ProcessPaymentInventoryAsync(int orderId, string paymentStatus)
        {
            try
            {
                var orderItems = await _orderItemRepo.GetByOrderIdAsync(orderId);
                if (orderItems == null || !orderItems.Any())
                    return GenericResult<bool>.Failure("No order items");

                foreach (var item in orderItems)
                {
                    var inventory = await _inventoryRepo.GetByProductIdAsync(item.ProductId);
                    if (inventory == null) continue;

                    int newQty = inventory.Quantity;

                    if (paymentStatus == "Paid")
                    {
                        newQty -= item.Quantity;

                        if (newQty < 0)
                            return GenericResult<bool>.Failure("Insufficient inventory");
                    }
                    else if (paymentStatus == "Cancelled")
                    {
                        newQty += item.Quantity;
                    }

                    await _inventoryRepo.UpdateQuantityAsync(item.ProductId, newQty);
                }

                return GenericResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return GenericResult<bool>.Failure(ex.Message);
            }
        }


        public async Task DeductInventoryAsync(int orderId)
        {
            var items = await _orderItemRepo.GetByOrderIdAsync(orderId);


            foreach (var item in items)
            {
                var inventory = await _inventoryRepo.GetByProductIdAsync(item.ProductId);
                if (inventory == null) continue;


                inventory.Quantity -= item.Quantity;
                await _inventoryRepo.UpdateAsync(inventory);
            }
        }


        public async Task RestoreInventoryAsync(int orderId)
        {
            var items = await _orderItemRepo.GetByOrderIdAsync(orderId);


            foreach (var item in items)
            {
                var inventory = await _inventoryRepo.GetByProductIdAsync(item.ProductId);
                if (inventory == null) continue;


                inventory.Quantity += item.Quantity;
                await _inventoryRepo.UpdateAsync(inventory);
            }
        }

    }
}
