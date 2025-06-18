using System;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AVFramework.Classes
{
    public static class MailSender
    {
        private const string SenderEmail = "danya.agafonov.228@mail.ru";
        private const string SenderPassword = "mW8uBB5h6ztAgpDq5X7U";
        private const string RecipientEmail = "agafonova67@icloud.com";

        public static async Task SendFileAsync(string filePath, string description)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("YaraScanner", SenderEmail));
                message.To.Add(new MailboxAddress("", RecipientEmail));
                message.Subject = $"Отчет о подозрительном файле: {Path.GetFileName(filePath)}";

                var builder = new BodyBuilder();
                builder.TextBody = $"Отправлен подозрительный файл:\n" +
                                 $"Имя файла: {Path.GetFileName(filePath)}\n" +
                                 $"Путь: {filePath}\n" +
                                 $"MD5: {FileHashCalculator.CalculateFileHash(filePath)}\n" +
                                 $"Дата отправки: {DateTime.Now}\n\n" +
                                 $"Описание проблемы:\n{description}";

                builder.Attachments.Add(filePath);
                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.mail.ru", 587, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(SenderEmail, SenderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Logging.ErrorLog(ex);
                throw;
            }
        }
    }
} 