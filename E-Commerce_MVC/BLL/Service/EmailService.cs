using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.IService;
namespace BLL.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
        {
            try
            {
                // ✅ ĐỌC HTML TEMPLATE TỪ FILE
                string templatePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Templates",
                    "OtpEmail.html"
                );

                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"❌ Template not found: {templatePath}");
                    return false;
                }

                string htmlBody = await File.ReadAllTextAsync(templatePath);

                // ✅ THAY THẾ PLACEHOLDER BẰNG OTP THẬT
                htmlBody = htmlBody.Replace("{{OTP_CODE}}", otpCode);

                // ✅ TẠO EMAIL MESSAGE
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:FromEmail"]));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Mã xác thực đăng ký tài khoản - E-Commerce";

                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = htmlBody
                };

                // ✅ GỬI EMAIL QUA SMTP
                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
                    _configuration["EmailSettings:SmtpHost"],
                    int.Parse(_configuration["EmailSettings:SmtpPort"]),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    _configuration["EmailSettings:SmtpUser"],
                    _configuration["EmailSettings:SmtpPass"]
                );

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                Console.WriteLine($"✅ OTP email sent successfully to {toEmail}");
                Console.WriteLine($"   OTP Code: {otpCode}");
                return true;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"❌ Email template file not found: {ex.Message}");
                return false;
            }
            catch (SmtpCommandException ex)
            {
                Console.WriteLine($"❌ SMTP error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send OTP email: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
