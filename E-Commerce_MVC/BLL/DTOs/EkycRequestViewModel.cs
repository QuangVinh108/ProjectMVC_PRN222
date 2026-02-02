using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class EkycRequestViewModel
    {
        // Phần thông tin người dùng nhập/xác nhận
        [Required(ErrorMessage = "Vui lòng nhập Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số CCCD")]
        public string CccdNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ")]
        public string Address { get; set; }

        // Phần ảnh để đối chiếu
        [Required(ErrorMessage = "Vui lòng tải lên ảnh mặt trước CCCD")]
        public IFormFile FrontImage { get; set; }
    }
}
