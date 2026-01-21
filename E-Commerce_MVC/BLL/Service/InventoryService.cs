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

namespace BLL.Service
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IProductRepository _productRepo;
        public InventoryService(IInventoryRepository inventoryRepo, IProductRepository productRepo)
        {
            _inventoryRepo = inventoryRepo;
            _productRepo = productRepo;
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

                IQueryable<Inventory> queryable = _inventoryRepo.GetAllQueryable("Product");

                // Search
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    queryable = queryable.Where(i => EF.Functions.Like(i.Product.ProductName, $"%{query.Search}%") ||
                                                     EF.Functions.Like(i.Warehouse, $"%{query.Search}%"));
                }

                // Sorting
                var sortBy = query.SortBy?.ToLower() ?? "productname";
                var isDescending = query.SortDirection?.ToLower() == "desc";
                queryable = ApplySorting(queryable, sortBy, isDescending);

                // Count total
                var totalCount = await queryable.CountAsync();


                // Paging 
                var items = await queryable
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(i => new InventoryDto
                    {
                        InventoryId = i.InventoryId,
                        ProductId = i.ProductId,
                        ProductName = i.Product.ProductName,
                        Quantity = i.Quantity,
                        Warehouse = i.Warehouse,
                        UpdatedAt = i.UpdatedAt
                    })
                    .ToListAsync();

                var result = new PagedResult<InventoryDto>
                {
                    Items = items,
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

        private static IQueryable<Inventory> ApplySorting(IQueryable<Inventory> queryable, string sortBy, bool isDescending)
        {
            return sortBy switch
            {
                "quantity" => isDescending
                    ? queryable.OrderByDescending(i => i.Quantity)
                    : queryable.OrderBy(i => i.Quantity),
                "warehouse" => isDescending
                    ? queryable.OrderByDescending(i => i.Warehouse)
                    : queryable.OrderBy(i => i.Warehouse),
                "updatedat" => isDescending
                    ? queryable.OrderByDescending(i => i.UpdatedAt)
                    : queryable.OrderBy(i => i.UpdatedAt),
                _ => isDescending
                    ? queryable.OrderByDescending(i => i.Product.ProductName)
                    : queryable.OrderBy(i => i.Product.ProductName)
            };
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

                // ✅ RELOAD với Product để tránh NULL
                var inventoryWithProduct = await _inventoryRepo.GetByProductIdAsync(dto.ProductId);
                var createdDto = MapToDto(inventoryWithProduct!);

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
                if(newQuantity < 0)
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
                if (quantity <= 0)
                    return GenericResult<bool>.Success(true); // 0 quantity luôn có stock

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
                var inventory = await _inventoryRepo.GetByProductIdAsync(productId);
                var stock = inventory?.Quantity ?? 0;
                return GenericResult<int>.Success(stock);
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
                ProductName = inventory.Product?.ProductName ?? "Unknown",  // ✅ NULL SAFE
                Quantity = inventory.Quantity,
                Warehouse = inventory.Warehouse,
                UpdatedAt = inventory.UpdatedAt
            };
        }

    }
}
