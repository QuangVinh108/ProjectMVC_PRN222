using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepository
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> GetAllCategories();
        List<Category> GetAll();

        Category GetById(int id);

        void Add(Category category);

        void Update(Category category);

        void Delete(int id);
    }
}
