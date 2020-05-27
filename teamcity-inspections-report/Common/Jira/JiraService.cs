using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ToolKit.Common.Jira
{
    public class JiraService
    {
        private readonly string _login;
        private readonly string _password;
        private const string JiraUrl = "https://jira.shift-technology.com/rest/api/latest";
        public bool IsSetUp { get; }

        public JiraService(string login, string password)
        {
            _login = login;
            _password = password;
            IsSetUp = !string.IsNullOrEmpty(_login) && !string.IsNullOrEmpty(_password);
        }

        public async Task<bool> Noop()
        {
            try
            {
                var response = await SendRequest<JiraRequestBase, JiraSelfResponse>(HttpMethod.Get, "/myself", new JiraRequestBase());
                return response.IsActive;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task CreateTask(string[] strings)
        {
            var request = new JiraIssueRequest
            {

            };
            var response = await SendRequest<JiraIssueRequest, JiraIssueResponse>(HttpMethod.Post, "/issues", request);

            Console.WriteLine($"Issue {response.Id} was created");
        }

        private async Task<TResponse> SendRequest<TRequest, TResponse>(HttpMethod method, string endpoint, TRequest request)
        {
            using (var client = new HttpClient())
            {
                var message = new HttpRequestMessage(method, $"{JiraUrl}{endpoint}");

                var bytes = Encoding.UTF8.GetBytes($"{_login}:{_password}");
                var basicToken = Convert.ToBase64String(bytes);

                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
                message.Headers.Add("Accept", "application/json");
                message.Headers.Add("Content-Type", "application/json");

                if (request != null)
                {
                    var content = JsonConvert.SerializeObject(request);
                    message.Content =
                        new StringContent(content, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(message);

                if (!response.IsSuccessStatusCode)
                    throw new Exception(
                        $"Failed to send {message.Method} request to {message.RequestUri} with content ({message.Content}):"
                        + $" Got {response.StatusCode} ({await response.Content.ReadAsStringAsync()})");

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(responseContent);
            }
        }
    }
}
