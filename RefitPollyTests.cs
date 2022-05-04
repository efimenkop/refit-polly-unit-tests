namespace UnitTests
{
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using Polly.Extensions.Http;
    using Refit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Xunit;

    public class RefitPollyTests
    {
        [Theory]
        [InlineData(HttpStatusCode.RequestTimeout)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public async Task WhenHttpCallFails_HttpClientRetries(HttpStatusCode statusCode)
        {
            // Arrange
            var handler = new CannedResponseHandler(statusCode);
            var provider = CreateServiceProvider(handler);
            var client = provider.GetRequiredService<IGitHubApi>();

            // Act
            await Assert.ThrowsAsync<ApiException>(() => client.GetFooAsync());

            // Assert
            Assert.Equal(4, handler.InvocationCount);
        }

        [Fact]
        public async Task WhenHttpCallFailsWithBadRequest_HttpClientDoesNotRetry()
        {
            // Arrange
            var handler = new CannedResponseHandler(HttpStatusCode.BadRequest);
            var provider = CreateServiceProvider(handler);
            var client = provider.GetRequiredService<IGitHubApi>();

            // Act
            await Assert.ThrowsAsync<ApiException>(() => client.GetFooAsync());

            // Assert
            Assert.Equal(1, handler.InvocationCount);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        public async Task WhenHttpCallSucceed_HttpClientDoesNotThrow(HttpStatusCode statusCode)
        {
            // Arrange
            var handler = new CannedResponseHandler(statusCode);
            var provider = CreateServiceProvider(handler);
            var client = provider.GetRequiredService<IGitHubApi>();

            // Act
            await client.GetFooAsync();

            // Assert
            Assert.Equal(1, handler.InvocationCount);
        }

        private static IServiceProvider CreateServiceProvider(CannedResponseHandler handler)
        {
            var services = new ServiceCollection();
            var client = services
                .AddRefitClient<IGitHubApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
                .AddPolicyHandler(GetRetryPolicy());

            // adding a custom behaviour on the top of existing client
            services.AddTransient(_ => handler);
            // this "mutates" http client which works under the hood of IGitHubApi
            services.AddHttpClient(client.Name).AddHttpMessageHandler<CannedResponseHandler>();

            return services.BuildServiceProvider();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying = {
                HttpStatusCode.RequestTimeout, // 408
                HttpStatusCode.InternalServerError, // 500
                HttpStatusCode.BadGateway, // 502
                HttpStatusCode.ServiceUnavailable, // 503
                HttpStatusCode.GatewayTimeout // 504
            };

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(new List<TimeSpan> { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero });
        }
    }
}
