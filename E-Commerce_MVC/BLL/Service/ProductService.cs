using BLL.DTOs;
using DAL.Entities;
using Repositories.IRepository;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
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
                CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại"
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
                Status = p.Status
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
                CreatedAt = DateTime.Now 
            };

            _productRepository.AddProduct(product);
        }

        public void Update(CreateProductViewModel model)
        {
            var product = _productRepository.GetProductById(model.ProductId);
            if (product != null)
            {
                product.ProductName = model.ProductName;
                product.Sku = model.Sku;
                product.Price = model.Price;
                product.Description = model.Description;
                product.CategoryId = model.CategoryId;
                product.Status = model.Status;

                product.UpdatedAt = DateTime.Now; 

                _productRepository.UpdateProduct(product);
            }
        }

        public void Delete(int id)
        {
            _productRepository.DeleteProduct(id);
        }
    }
}
