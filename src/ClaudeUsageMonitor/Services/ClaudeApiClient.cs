using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeUsageMonitor.Models;
using static ClaudeUsageMonitor.Services.Logger;

namespace ClaudeUsageMonitor.Services;

/// <summary>
/// Client for Claude.ai internal API
/// </summary>
public class ClaudeApiClient : IDisposable
{
    private const string BaseUrl = "https://claude.ai/api";
    private readonly HttpClient _httpClient;
    private string? _sessionKey;
    private string? _organizationId;

    public ClaudeApiClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = false // We manage cookies manually
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,ja;q=0.8");
    }

    public void SetCredentials(string sessionKey, string organizationId)
    {
        _sessionKey = sessionKey;
        _organizationId = organizationId;
    }

    public bool HasCredentials => !string.IsNullOrEmpty(_sessionKey) && !string.IsNullOrEmpty(_organizationId);

    /// <summary>
    /// Get current usage data
    /// </summary>
    public async Task<UsageData?> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasCredentials)
        {
            Log("API", "GetUsageAsync: No credentials");
            throw new InvalidOperationException("Credentials not set");
        }

        var endpoint = $"/organizations/{_organizationId}/usage";
        Log("API", $"GetUsageAsync: {endpoint}");
        var request = CreateRequest(endpoint);
        
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            Log("API", $"GetUsageAsync response: {(int)response.StatusCode} - {json.Substring(0, Math.Min(200, json.Length))}");
            
            response.EnsureSuccessStatusCode();

            var apiResponse = JsonSerializer.Deserialize<UsageApiResponse>(json);

            if (apiResponse?.FiveHour == null)
            {
                Log("API", "GetUsageAsync: No five_hour data in response");
                return null;
            }

            Log("API", $"GetUsageAsync success: {apiResponse.FiveHour.Utilization}%");
            return new UsageData
            {
                Utilization = apiResponse.FiveHour.Utilization,
                ResetsAt = ParseDateTime(apiResponse.FiveHour.ResetsAt),
                FetchedAt = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            Log("API", $"GetUsageAsync error: {ex.Message}");
            throw new ClaudeApiException("Failed to fetch usage data", ex);
        }
    }

    /// <summary>
    /// Get list of organizations
    /// </summary>
    public async Task<List<Organization>> GetOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_sessionKey))
            throw new InvalidOperationException("Session key not set");

        var request = CreateRequest("/organizations");
        
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new ClaudeApiException($"API error {(int)response.StatusCode}: {json}");
            }

            var apiResponse = JsonSerializer.Deserialize<List<OrganizationApiResponse>>(json);

            return apiResponse?.Select(o => new Organization
            {
                Uuid = o.Uuid,
                Name = string.IsNullOrEmpty(o.Name) ? o.Uuid : o.Name,
                RateLimitTier = o.RateLimitTier
            }).ToList() ?? new List<Organization>();
        }
        catch (HttpRequestException ex)
        {
            throw new ClaudeApiException($"Network error: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new ClaudeApiException($"Parse error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get subscription information
    /// </summary>
    public async Task<SubscriptionInfo?> GetSubscriptionInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!HasCredentials)
            throw new InvalidOperationException("Credentials not set");

        var request = CreateRequest($"/bootstrap/{_organizationId}/statsig");
        
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<StatsigApiResponse>(json);

            return new SubscriptionInfo
            {
                PlanType = apiResponse?.User?.Custom?.OrgType ?? "free",
                IsRaven = apiResponse?.User?.Custom?.IsRaven ?? false
            };
        }
        catch (HttpRequestException ex)
        {
            throw new ClaudeApiException("Failed to fetch subscription info", ex);
        }
    }

    /// <summary>
    /// Validate session key by attempting to fetch organizations
    /// </summary>
    public async Task<bool> ValidateSessionKeyAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        var originalKey = _sessionKey;
        try
        {
            _sessionKey = sessionKey;
            var orgs = await GetOrganizationsAsync(cancellationToken);
            return orgs.Count > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            _sessionKey = originalKey;
        }
    }

    private HttpRequestMessage CreateRequest(string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{endpoint}");
        request.Headers.Add("Cookie", $"sessionKey={_sessionKey}");
        request.Headers.Add("Origin", "https://claude.ai");
        request.Headers.Add("Referer", "https://claude.ai/");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        request.Headers.Add("Sec-Ch-Ua", "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
        request.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
        request.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
        return request;
    }

    private static DateTime ParseDateTime(string? dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return DateTime.UtcNow.AddHours(5);

        if (DateTime.TryParse(dateTimeString, out var result))
            return result.ToUniversalTime();

        return DateTime.UtcNow.AddHours(5);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public class ClaudeApiException : Exception
{
    public ClaudeApiException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
