using System.Net;
using Cheesarr.Settings;

namespace Cheesarr.Services.Download;

public class QBTAuthHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CookieContainer _cookieContainer;
    private readonly HttpClient _authHttpClient;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private bool _isAuthenticated = false;

    public QBTAuthHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler { CookieContainer = _cookieContainer };
        _authHttpClient = new HttpClient(handler) { BaseAddress = GetBaseUri() };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_isAuthenticated)
        {
            await AuthenticateAsync();
        }

        // Ensure the cookie is applied to the outgoing request
        request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(GetBaseUri()));

        var response = await base.SendAsync(request, cancellationToken);

        // If authentication fails, retry once
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _isAuthenticated = false;
            await AuthenticateAsync();
            request.Headers.Remove("Cookie"); // Remove old cookies
            request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(GetBaseUri())); // Add updated cookies
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    private async Task AuthenticateAsync()
    {
        await _authLock.WaitAsync();
        try
        {
            if (_isAuthenticated) return;

            var settings = _serviceProvider.GetRequiredService<SettingsService>().GetSettings<QBTSettingsData>();

            var credentials = new Dictionary<string, string>
            {
                { "username", settings.Username },
                { "password", settings.Password }
            };

            var content = new FormUrlEncodedContent(credentials);
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/login")
            {
                Content = content
            };

            request.Headers.Referrer = GetBaseUri();

            var response = await _authHttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _isAuthenticated = true;
            }
            else
            {
                throw new Exception("Failed to authenticate with QBT.");
            }
        }
        finally
        {
            _authLock.Release();
        }
    }

    private Uri GetBaseUri()
    {
        var settings = _serviceProvider.GetRequiredService<SettingsService>().GetSettings<QBTSettingsData>();
        return new Uri($"{(settings.UseSSL ? "https" : "http")}://{settings.Host}:{settings.Port}");
    }
}