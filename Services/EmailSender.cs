using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace Matchletic.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Log the email or implement actual email sending
            // For now, just return a completed task
            return Task.CompletedTask;
        }
    }
}
