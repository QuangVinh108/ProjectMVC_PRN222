using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs
{
    public class CreateOrderDto
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Tên thành phố không được vượt quá 100 ký tự")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Tên quốc gia không được vượt quá 100 ký tự")]
        public string? Country { get; set; }

        [StringLength(20, ErrorMessage = "Mã bưu điện không được vượt quá 20 ký tự")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        public string PaymentMethod { get; set; } = "COD"; // COD, CreditCard, BankTransfer

        [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự")]
        public string? Note { get; set; }
    }
}
