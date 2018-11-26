
using System;
using System.Net;
using System.Threading.Tasks;
using MealsService.Infrastructure;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MealsService.Email
{
    public class EmailService
    {
        private const string FROM_EMAIL = "info@greenerplate.com";
        private const string FROM_NAME = "Greener Plate";

        private SendGridClient _client;
        private IViewRenderService _viewRenderer;

        public EmailService(IViewRenderService viewRenderer)
        {
            _viewRenderer = viewRenderer;

            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            _client = new SendGridClient(apiKey);
        }

        public async Task<bool> SendEmail(string template, string email, string subject, string name = null, object data = null)
        {
            
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(FROM_EMAIL, FROM_NAME),
                Subject = subject,
                PlainTextContent = await _viewRenderer.RenderToStringAsync("Emails/Plain/" + template, data),
                HtmlContent = await _viewRenderer.RenderToStringAsync("Emails/Html/" + template, data)
            };
            msg.AddTo(new EmailAddress(email, name));
            var response = await _client.SendEmailAsync(msg);

            return response.StatusCode == HttpStatusCode.Accepted;
        }
    }
}
