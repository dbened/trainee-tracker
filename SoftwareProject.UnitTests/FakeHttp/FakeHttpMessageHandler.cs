using System.Net;
using System.Net.Http;

namespace SoftwareProject.UnitTests.FakeHttp
{
    // Fake HTTP handler used in unit tests to simulate
    // a successful response from the working hours API.

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        // Intercepts outgoing HTTP requests and returns
        // predefined working hour data instead of calling the real API.
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Create dynamic dates so the test remains valid
            // regardless of when it is executed.
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            var dayAfter = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");

            // Simulate the JSON structure returned by the
            // working hours API.
            var json =
            $@"{{
                ""entries"":[
                    {{ ""date"": ""{today}"", ""working_hours"": 8 }},
                    {{ ""date"": ""{tomorrow}"", ""working_hours"": 8 }},
                    {{ ""date"": ""{dayAfter}"", ""working_hours"": 8 }}
                ]
            }}";
            // Return a successful HTTP 200 response containing
            // the fake working hours JSON.
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }
}