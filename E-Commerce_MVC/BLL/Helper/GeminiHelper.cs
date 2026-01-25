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

        public async Task<GeminiIdCardDto> AnalyzeIdCardAsync(IFormFile imageFile)
        {
            // Check API Key
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("YOUR_ACTUAL"))
            {
                Console.WriteLine("❌ LỖI: API Key chưa cấu hình.");
                return null;
            }

            try
            {
                // 1. Resize và Convert ảnh sang Base64
                // (Lưu ý: Nếu ảnh CCCD gốc từ điện thoại quá nặng > 4MB, Google sẽ timeout. 
                // Tốt nhất nên resize ảnh xuống chiều ngang khoảng 1024px trước khi gửi - Ở đây tôi làm đơn giản là gửi luôn)
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                var base64Image = Convert.ToBase64String(ms.ToArray());

                // 2. Prompt "Thép" - Ép AI trả về đúng định dạng
                var prompt = @"Bạn là một hệ thống OCR (Nhận dạng ký tự quang học) chuyên nghiệp.
Nhiệm vụ: Trích xuất thông tin từ hình ảnh Căn cước công dân (CCCD) Việt Nam.

Yêu cầu output:
Chỉ trả về duy nhất một chuỗi JSON thuần túy (Raw JSON), không được bao bọc bởi markdown (```json ... ```).
JSON phải có cấu trúc sau:
{
    ""isValid"": true, (chỉ true nếu nhìn rõ số, tên, ngày sinh, ảnh không bị che, không phải photocopy)
    ""reason"": ""null nếu hợp lệ, ghi lý do ngắn gọn nếu không hợp lệ"",
    ""data"": {
        ""idNumber"": ""Số CCCD (dãy số)"",
        ""fullName"": ""Họ và tên (viết IN HOA)"",
        ""dob"": ""Ngày sinh (định dạng dd/MM/yyyy)"",
        ""address"": ""Nơi thường trú (ghi đầy đủ)""
    }
}

Nếu ảnh quá mờ, bị mất góc, hoặc không phải thẻ CCCD, hãy trả về isValid: false.";

                // 3. Cấu hình Request (THÊM SAFETY SETTINGS ĐỂ KHÔNG BỊ CHẶN)
                var requestBody = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = imageFile.ContentType, data = base64Image } }
                    }
                }
            },
                    // 🔥 QUAN TRỌNG: Tắt bộ lọc an toàn để AI chịu đọc CCCD
                    safetySettings = new[]
                    {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
            }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

                // 4. Gọi API
                var response = await _httpClient.PostAsync(url, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API ERROR: {response.StatusCode} - {responseString}");
                    return null;
                }

                // 5. Parse JSON (Xử lý kỹ hơn)
                using var doc = JsonDocument.Parse(responseString);

                // Kiểm tra xem có candidate nào không (Nếu bị chặn, candidates sẽ rỗng và có promptFeedback.blockReason)
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    Console.WriteLine("❌ Lỗi: Google đã chặn ảnh này hoặc không nhận diện được.");
                    // In ra lý do chặn để debug
                    if (doc.RootElement.TryGetProperty("promptFeedback", out var feedback))
                    {
                        Console.WriteLine($"Feedback: {feedback}");
                    }
                    return null;
                }

                var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                // Clean chuỗi JSON (Xóa ```json và khoảng trắng thừa)
                text = text.Replace("```json", "").Replace("```", "").Trim();

                // Tìm điểm bắt đầu và kết thúc của JSON để cắt bỏ rác nếu có
                int startIndex = text.IndexOf('{');
                int endIndex = text.LastIndexOf('}');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    text = text.Substring(startIndex, endIndex - startIndex + 1);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<GeminiIdCardDto>(text, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION: {ex.Message}");
                return null;
            }
        }
    }
}
