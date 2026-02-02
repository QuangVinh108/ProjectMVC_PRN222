using BLL.DTOs;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface ICategoryService
    {
        IEnumerable<CategoryDTO> GetAll();
        void Add(CategoryDTO categoryDto);
    }
}
