using System.Net;
using System.Net.Http;

namespace SoftwareProject.UnitTests.FakeHttp
{
    // Fake HTTP handler used in unit tests to simulate
    // a failed API request returning HTTP 404 Not Found.
    public class Fake404Handler : HttpMessageHandler
    {
        /// Intercepts outgoing HTTP requests and returns
        /// a predefined 404 response instead of calling a real API.
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Simulate an unavailable or invalid API response
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}