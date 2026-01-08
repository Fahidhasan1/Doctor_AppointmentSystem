using Doctor_AppointmentSystem.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Doctor_AppointmentSystem.Services
{
    public class MailKitEmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public MailKitEmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();

            // Gmail: STARTTLS on 587
            var socketOption = _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOption);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
