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

        public List<ProductViewModel> GetFilteredProducts(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder)
        {
            var query = _productRepository.GetAllQueryable();

            query = query.Where(p => p.Status == 1);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            switch (sortOrder)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price); 
                    break;

                case "price_desc":
                default: 
                    query = query.OrderByDescending(p => p.Price);
                    break;
            }

            var result = query.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                Image = p.Image,
                CategoryName = p.Category.CategoryName,
                Sku = p.Sku
            }).ToList();

            return result;
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

        public List<ProductViewModel> GetProductsForAdmin(int? parentId, int? categoryId)
        {
            // 1. Khởi tạo truy vấn
            var query = _productRepository.GetAllQueryable();
            // Lưu ý: Repository nên trả về IQueryable<Product> để chưa chạy SQL ngay

            // 2. LOGIC LỌC
            if (categoryId.HasValue)
            {
                // Trường hợp 1: Đã chọn cụ thể danh mục con (VD: iPhone)
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
            else if (parentId.HasValue)
            {
                // Trường hợp 2: Mới chọn danh mục Cha (VD: Điện thoại)
                // -> Lấy tất cả sản phẩm thuộc các danh mục con của Cha này

                // Cần inject thêm ICategoryRepository hoặc dùng logic join
                // Cách đơn giản nhất nếu có Navigation Property:
                query = query.Where(p => p.Category.ParentId == parentId.Value);
            }
            else
            {
                // Trường hợp 3: Chưa chọn gì (Ở màn hình chính admin)
                // Bạn có thể trả về Rỗng để giao diện chỉ hiện 4 ô danh mục cho gọn
                return new List<ProductViewModel>();

                // Hoặc trả về tất cả nếu muốn:
                // (Không làm gì cả)
            }

            // 3. Map sang ViewModel & Execute
            return query.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                Image = p.Image,
                Sku = p.Sku,
                Status = p.Status,
                CategoryName = p.Category.CategoryName
            }).ToList();
        }
    }
}
