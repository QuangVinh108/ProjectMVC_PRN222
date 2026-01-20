using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.IRepository
{
    public interface IRoleRepository
    {
        IEnumerable<Role> GetAllRoles();
    }
}
