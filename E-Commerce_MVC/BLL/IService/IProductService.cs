using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IProductService
    {
        IEnumerable<ProductViewModel> GetAll();
        CreateProductViewModel GetById(int id);
        void Create(CreateProductViewModel model);
        void Update(CreateProductViewModel model);
        void Delete(int id);
    }
}
