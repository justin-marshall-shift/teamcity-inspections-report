using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ToolKit.Common.Hangout
{
    public class HangoutService
    {
        private readonly string _webhook;

        public HangoutService(string webhook)
        {
            _webhook = webhook;
        }

        public async Task SendCards(IEnumerable<HangoutCard> cards)
        {
            var content = JsonConvert.SerializeObject(new HangoutCardMessage
            {
                Cards = cards.ToArray()
            }, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            Console.WriteLine($"Sending message to Hangout:\r\n{content}");
            HttpContent httpContent = new StringContent(content);

            using (var httpClient = new HttpClient())
            {
                Console.WriteLine($"Webhook: {_webhook}");
                var response = await httpClient.PostAsync(_webhook, httpContent);

                Console.WriteLine($"Response: {response.StatusCode.ToString()}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
            }
        }
    }
}
