using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public IEnumerable<CategoryDTO> GetAll()
        {
            var entities = _categoryRepository.GetAllCategories();

            // 2. Chuyển đổi sang DTO (Mapping)
            // Dùng Linq .Select() để map từng phần tử
            var dtos = entities.Select(c => new CategoryDTO
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ParentId = c.ParentId,
                Description = c.Description
                // Map thêm các trường khác nếu DTO cần
            });

            return dtos.ToList(); // Trả về List DTO
        }

        public void Add(CategoryDTO dto)
        {
            // 1. Chuyển đổi dữ liệu từ DTO (Giao diện gửi xuống) sang Entity (Database cần)
            var categoryEntity = new Category
            {
                CategoryName = dto.CategoryName,
                Description = dto.Description,
                ParentId = dto.ParentId
                // Các trường khác như IsActive, CreatedAt có thể gán mặc định tại đây hoặc trong DB
            };

            // 2. Gọi Repository để lưu
            _categoryRepository.Add(categoryEntity);
        }
    }
}
