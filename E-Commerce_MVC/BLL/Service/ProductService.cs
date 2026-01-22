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
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public IEnumerable<ProductViewModel> GetAll()
        {
            var products = _productRepository.GetAllProducts();

            return products.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Sku = p.Sku,
                Price = p.Price,
                Status = p.Status,
                CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại",
                Image = p.Image
            }).ToList();
        }

        public CreateProductViewModel GetById(int id)
        {
            var p = _productRepository.GetProductById(id);
            if (p == null) return null;

            return new CreateProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Sku = p.Sku,
                Price = p.Price,
                Description = p.Description,
                CategoryId = p.CategoryId,
                Status = p.Status,
                Image = p.Image
            };
        }

        public void Create(CreateProductViewModel model)
        {
            var product = new Product
            {
                ProductName = model.ProductName,
                Sku = model.Sku,
                Price = model.Price,
                Description = model.Description,
                CategoryId = model.CategoryId,
                Status = model.Status, 
                CreatedAt = DateTime.Now,
                Image = model.Image
            };

            _productRepository.AddProduct(product);
        }

        public void Update(CreateProductViewModel model)
        {
            // Lấy sản phẩm từ DB lên
            var product = _productRepository.GetProductById(model.ProductId);

            if (product != null)
            {
                product.ProductName = model.ProductName;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;
                product.Sku = model.Sku;
                product.Description = model.Description;
                product.Status = model.Status;
                product.UpdatedAt = DateTime.Now;

                // Cập nhật ảnh (Nếu model.Image có giá trị mới từ Controller gửi xuống)
                // Lưu ý: Controller đã xử lý logic giữ ảnh cũ nếu không upload mới rồi
                if (!string.IsNullOrEmpty(model.Image))
                {
                    product.Image = model.Image;
                }

                _productRepository.UpdateProduct(product);
            }
        }

        public void Delete(int id)
        {
            _productRepository.DeleteProduct(id);
        }

        public ProductViewModel GetDetail(int id)
        {
            // Gọi Repo lấy Entity (đã bao gồm Category nhờ .Include ở Repo)
            var p = _productRepository.GetProductById(id);

            if (p == null) return null;

            // Map sang ProductViewModel (đúng kiểu View cần)
            return new ProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Sku = p.Sku,
                Price = p.Price,
                Description = p.Description, // Giờ đã có chỗ chứa
                CategoryName = p.Category != null ? p.Category.CategoryName : "N/A",
                Status = p.Status,
                Image = p.Image
            };
        }
    }
}
