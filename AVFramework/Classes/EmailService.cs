using System;
using System.Net;
using System.Net.Mail;
using System.Windows;

namespace AVFramework.Classes
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enableSsl;

        public EmailService(string smtpServer, int port, string username, string password, bool enableSsl = true)
        {
            _smtpServer = smtpServer;
            _port = port;
            _username = username;
            _password = password;
            _enableSsl = enableSsl;
        }

        public bool SendReport(string to, string subject, string body, string attachmentPath = null)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _port))
                {
                    client.EnableSsl = _enableSsl;
                    client.Credentials = new NetworkCredential(_username, _password);

                    var message = new MailMessage
                    {
                        From = new MailAddress(_username),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    message.To.Add(to);

                    if (!string.IsNullOrEmpty(attachmentPath))
                    {
                        message.Attachments.Add(new Attachment(attachmentPath));
                    }

                    client.Send(message);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке письма: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
} 