using System.Threading.Tasks;
using teamcity_inspections_report.Options;

namespace teamcity_inspections_report.Common
{
    public class MailTester
    {
        private readonly SoftwareQualityMailNotifier _notifier;
        private readonly string _mailTo;

        public MailTester(MailTestOptions options)
        {
            _notifier = new SoftwareQualityMailNotifier(options.Login, options.Password);
            _mailTo = options.MailTo;
        }

        public async Task SendMail()
        {
            var body = GetBody("Who are you?");
            await _notifier.SendMail("This is a test", _mailTo, "Who are you?", body);
        }

        private string GetBody(string name)
        {
            var files = new []{ "file1","file2","file3"};
            var url =
                "http://teamcity.corp.shift-technology.com/viewLog.html?buildId=538803&buildTypeId=ShiftQuality_ReleaseNotes&tab=report_project117_Release_Note&branch_ShiftQuality=%3Cdefault%3E";
            var body =
                $@"<body style=""margin: 0; padding: 0;"">
 <table border=""1"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
  <tr>
   <td>
    Hello {name},

    New errors have been introduced in the last <a href=""{url}"">daily inspection</a>.
Can you have a look please?
It seems you contributed to the following file(s):
{string.Join("\r\n", files)}

If you received this mail by error, please <a href=""mailto:justin.marshall@shift-technology.com,christophe.guilhou@shift-technology.com&subject=Bad attribution"">notify us</a>.

Thank you,
Best regards,

Shift Quality Team
     </td>
  </tr>
 </table>
</body>";

            return body;
        }

    }
}
