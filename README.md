# E-Commerce MVC Shop (3-Layer Architecture)

Dự án Website Thương mại điện tử được xây dựng trên nền tảng ASP.NET Core MVC, áp dụng Mô hình 3 lớp (Three-Layer Architecture) để đảm bảo tính tách biệt, bảo mật và dễ dàng bảo trì. Hệ thống sử dụng SQL Server làm hệ quản trị cơ sở dữ liệu.

## 🚀 Công nghệ & Kiến trúc

### Công nghệ
* Backend: ASP.NET Core (.NET 8)
* Database: SQL Server
* Frontend: Razor Views, Bootstrap 5, jQuery, HTML5/CSS3.
* Design Pattern: MVC (Model-View-Controller) kết hợp 3-Layer.

### 🏗️ Mô hình 3 lớp (Architecture)
Dự án được chia thành 3 tầng xử lý riêng biệt:

1.  Presentation Layer (GUI/Web):
    * Chứa các Controllers và Views.
    * Tiếp nhận yêu cầu từ người dùng và hiển thị dữ liệu.
    * Sử dụng ViewModel để trao đổi dữ liệu với View.
2.  Business Logic Layer (BLL/Service):
    * Xử lý các nghiệp vụ chính như: Tính toán giỏ hàng, Quy trình đặt hàng.  
3.  Data Access Layer (DAL/Repository):
    * Làm việc trực tiếp với Database (SQL Server).
    * Thực thi các câu lệnh truy vấn, thêm/xóa/sửa dữ liệu.
    * Dữ liệu khởi tạo từ `CREATE DATABASE ShopDB.txt`.

## ✨ Tính năng chính

* Xác thực & Phân quyền:
    * Đăng ký, Đăng nhập hệ thống.
    * Xác thực tài khoản qua Email OTP.
* Quản lý Sản phẩm: Xem danh sách, chi tiết sản phẩm.
* Chức năng Đặt hàng (Order):
    * Thêm sản phẩm vào giỏ hàng.
    * Thanh toán và chọn địa chỉ nhận hàng (Tích hợp Dropdown Tỉnh/Thành phố động).
* Quản trị (Admin): Quản lý đơn hàng và dữ liệu hệ thống.

## 📂 Cấu trúc thư mục

* E-Commerce_MVC: Source code chính của ứng dụng (Chứa Controller, Views...).
* CREATE DATABASE ShopDB.txt: Script SQL để tạo cấu trúc Database chuẩn.
* Data.txt: Dữ liệu mẫu (Seed Data) để import ban đầu.
* README.md: Tài liệu hướng dẫn này.

## 🛠️ Hướng dẫn cài đặt (Localhost)

### Bước 1: Chuẩn bị Database
1.  Mở SQL Server Management Studio (SSMS).
2.  Chạy file script `CREATE DATABASE ShopDB.txt` để tạo CSDL.
3.  Import dữ liệu từ file `Data.txt` nếu cần dữ liệu mẫu.

### Bước 2: Cấu hình kết nối
Mở file `appsettings.json` trong project và sửa chuỗi kết nối (`ConnectionString`):

```json
"ConnectionStrings": {
  "DefaultConnection": "server=(local); database=ShopDB; uid=sa; pwd=12345; TrustServerCertificate=True; Trusted_Connection=True;"
}
