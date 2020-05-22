using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace teamcity_inspections_report.Common
{
    public class SoftwareQualityMailNotifier
    {
        private readonly string _login;
        private readonly string _password;
        public bool IsSetUp { get; }

        public SoftwareQualityMailNotifier(string login, string password)
        {
            _login = login;
            _password = password;
            IsSetUp = !string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password);
        }

        public async Task SendMail(string subject, string mailAddress, string name, string body)
        {
            var fromAddress = new MailAddress("teamcity@shift-technology.com", "TeamCity");
            var toAddress = new MailAddress(mailAddress, name);

            using (var smtp = new SmtpClient
            {
                Host = "email-smtp.eu-west-1.amazonaws.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_login, _password),
            })
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                await smtp.SendMailAsync(message);
            }
        }
    }
}
