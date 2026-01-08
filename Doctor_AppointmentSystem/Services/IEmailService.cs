using System.Threading.Tasks;

namespace Doctor_AppointmentSystem.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}
