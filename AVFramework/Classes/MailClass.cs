using MimeKit;
using System;
using System.Threading.Tasks;

namespace AVFramework.Classes
{
    public class MailClass
    {
        public async Task SendMail(string subject, string body, string attachment)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Пользователь", "yuzerz@mail.ru"));
                message.To.Add(new MailboxAddress("", "jsuppor@mail.ru"));
                message.Subject = subject;

                var builder = new BodyBuilder();

                // Set the plain-text version of the message text
                builder.TextBody = body;

                // We may also want to attach a calendar event for Monica's party...
                builder.Attachments.Add(attachment);

                // Now we just need to set the message body and we're done
                message.Body = builder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {

                    await client.ConnectAsync("smtp.mail.ru", 25, false);//25
                    await client.AuthenticateAsync("yuzerz@mail.ru", "nfVLR1imcAQbyqKGKpga");
                    await client.SendAsync(message);

                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
