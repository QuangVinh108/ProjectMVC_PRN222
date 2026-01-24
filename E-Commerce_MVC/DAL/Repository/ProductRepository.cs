using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ShopDbContext _context;

        public ProductRepository(ShopDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Product> GetAllProducts()
        {
            return _context.Products.Include(p => p.Category).ToList();
        }

        public Product GetProductById(int id)
        {
            return _context.Products.Include(p => p.Category)
                                    .FirstOrDefault(p => p.ProductId == id);
        }

        public void AddProduct(Product product)
        {
            _context.Products.Add(product);
            _context.SaveChanges();
        }

        public void UpdateProduct(Product product)
        {
            _context.Products.Update(product);
            _context.SaveChanges();
        }

        public void DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                product.Status = 0; 
                product.UpdatedAt = DateTime.Now; 

                _context.Products.Update(product); 
                _context.SaveChanges(); 
            }
        }
        // ==== DASHBOARD ==== 
        public async Task<int> GetTotalProductCountAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<List<Product>> GetActiveProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == 1) // Status = 1 là Active
                .ToListAsync();
        }

        public async Task<List<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

    }
}
