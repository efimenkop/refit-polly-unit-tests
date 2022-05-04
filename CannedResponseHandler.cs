namespace UnitTests;

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class CannedResponseHandler : DelegatingHandler
{
    private readonly HttpStatusCode _statusCode;
    public int InvocationCount { get; private set; }

    public CannedResponseHandler(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        InvocationCount++;

        var response = new HttpResponseMessage(_statusCode)
        {
            RequestMessage = request
        };

        return response;
    }
}