using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AVFramework.Classes
{
    public class MailClass
    {
        private const string SMTP_SERVER = "smtp.mail.ru";
        private const int SMTP_PORT = 465;
        private const string SMTP_USERNAME = "danya.agafonov.228@mail.ru";
        // Замените этот пароль на специальный пароль для приложений из настроек почты
        private const string SMTP_PASSWORD = "ipriwS8s4kMxBjum74Cn";
        private const string RECIPIENT_EMAIL = "agafonova67@icloud.com";

        public async Task SendMailAsync(List<string> filePaths, string description, Action<int> progressCallback)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("AV Framework", SMTP_USERNAME));
                message.To.Add(new MailboxAddress("Security Team", RECIPIENT_EMAIL));
                message.Subject = "Отчет о подозрительной активности";

                var builder = new BodyBuilder();
                builder.TextBody = description;

                // Добавляем файлы
                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        builder.Attachments.Add(filePath);
                    }
                }

                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    try
                    {
                        await client.ConnectAsync(SMTP_SERVER, SMTP_PORT, SecureSocketOptions.SslOnConnect);
                        progressCallback?.Invoke(30);

                        await client.AuthenticateAsync(SMTP_USERNAME, SMTP_PASSWORD);
                        progressCallback?.Invoke(60);

                        // Отправляем сообщение
                        await client.SendAsync(message);
                        progressCallback?.Invoke(90);

                        await client.DisconnectAsync(true);
                        progressCallback?.Invoke(100);
                    }
                    catch (AuthenticationException ex)
                    {
                        throw new Exception($"Ошибка аутентификации: {ex.Message}. Пожалуйста, проверьте настройки почты и убедитесь, что используется специальный пароль для приложений.");
                    }
                    catch (SmtpCommandException ex)
                    {
                        throw new Exception($"Ошибка SMTP: {ex.Message}. Код ошибки: {ex.StatusCode}");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Ошибка при отправке: {ex.Message}");
                    }
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
