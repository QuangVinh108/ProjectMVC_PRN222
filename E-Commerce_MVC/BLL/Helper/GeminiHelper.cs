using BLL.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Helper
{
    public class GeminiHelper
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiHelper(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Lấy API Key từ appsettings.json
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<GeminiProductDto> AnalyzeImageAsync(IFormFile imageFile)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("Gemini API Key chưa được cấu hình trong appsettings.json");
            }

            // 1. Chuyển ảnh sang Base64
            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            // 2. Tạo Request Body gửi lên Gemini
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = "Hãy đóng vai một chuyên gia bán hàng. Hãy phân tích hình ảnh sản phẩm này và trả về kết quả dưới dạng JSON thuần túy (không có markdown ```json) với các trường sau: productName (tên sản phẩm tiếng Việt), sku (tự tạo mã ngắn gọn), price (ước lượng giá VNĐ, số nguyên), description (mô tả ngắn hấp dẫn khoảng 3 câu), category (chọn 1 trong các từ khóa sau nếu giống nhất: 'Điện thoại', 'Laptop', 'Áo Nam', 'Quần Nam', 'Giày')." },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = imageFile.ContentType,
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // 3. Gọi API (Sửa lại chuỗi URL cho gọn và đúng chuẩn C#)
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, jsonContent);

            if (!response.IsSuccessStatusCode) 
                return null;

            var responseString = await response.Content.ReadAsStringAsync();

            // 4. Parse kết quả trả về
            try
            {
                using var doc = JsonDocument.Parse(responseString);

                // Kiểm tra xem có candidate nào không trước khi truy cập index [0]
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0) 
                    return null;

                var text = candidates[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text").GetString();

                // Clean chuỗi json
                text = text.Replace("```json", "").Replace("```", "").Trim();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<GeminiProductDto>(text, options);
            }
            catch
            {
                return null;
            }
        }
    }
}
