using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class CategoryDTO
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên danh mục không quá 100 ký tự")]
        public string CategoryName { get; set; }

        public string Description { get; set; }

        // Nếu ParentId = null => Là danh mục gốc (Cấp 1)
        // Nếu ParentId có giá trị => Là danh mục con (Cấp 2)
        public int? ParentId { get; set; }

        // Thuộc tính hiển thị tên danh mục cha (dùng khi cần)
        public string ParentName { get; set; }
    }
}
