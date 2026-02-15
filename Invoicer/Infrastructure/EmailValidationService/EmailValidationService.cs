using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using Invoicer.Infrastructure.EmailValidationService;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Invoicer.Infrastructure.EmailValidationService;

public class EmailValidationService(IMemoryCache _memoryCache, IHttpClientFactory _httpClientFactory) : IEmailValidationService
{
    private const string DisposableDomainsCacheKey = "DisposableEmailDomains";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<bool> IsValidEmail(string email)
    {
        if (IsDuplicateEmail(email))
            return false;
        if (!IsBasicFormatValid(email))
            return false;

        var normalized = NormalizeEmail(email);
        var (_, domain) = SplitEmail(normalized);

        if (string.IsNullOrEmpty(domain))
            return false;

        var disposableDomains = await GetDisposableEmailDomainsAsync();

        if (disposableDomains.Contains(domain))
            return false;

        return await HasMailServerAsync(domain);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool IsBasicFormatValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static (string LocalPart, string Domain) SplitEmail(string normalized)
    {
        var parts = normalized.Split('@');
        return parts.Length == 2
            ? (parts[0], parts[1])
            : (string.Empty, string.Empty);
    }

    private async Task<bool> HasMailServerAsync(string domain)
    {
        try
        {
            // Dns.GetHostEntryAsync resolves A/AAAA records → quick existence + mail likely possible check
            var hostEntry = await Dns.GetHostEntryAsync(domain);
            return hostEntry.AddressList.Length > 0;
        }
        catch (SocketException)
        {
            // Domain doesn't exist or no DNS response
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "MX/A check failed for domain {Domain} - treating as valid (fail-open)", domain);
            return true; // fail-open: don't block on transient DNS issues
        }
    }

    private async Task<HashSet<string>> GetDisposableEmailDomainsAsync()
    {
        if (_memoryCache.TryGetValue(DisposableDomainsCacheKey, out HashSet<string>? cached) &&
            cached is { Count: > 0 })
        {
            return cached;
        }

        var domains = await FetchDisposableDomainsAsync();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        };

        _memoryCache.Set(DisposableDomainsCacheKey, domains, options);

        return domains;
    }

    private async Task<HashSet<string>> FetchDisposableDomainsAsync()
    {
        const string url = "https://raw.githubusercontent.com/disposable-email-domains/disposable-email-domains/main/disposable_email_blocklist.conf";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith("//"))
                    continue;

                domains.Add(trimmed);
            }

            if (domains.Count > 100)
            {
                Log.Information("Loaded {Count} disposable domains from {Url}", domains.Count, url);
                return domains;
            }

            Log.Warning("Disposable list suspiciously small ({Count} entries) from {Url}", domains.Count, url);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch disposable domains from {Url}", url);
        }

        // Fail-open: don't block emails if list fetch fails
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }


    public bool IsDuplicateEmail(string email)
    {
        const string RecentEmailAttemptPrefix = "RecentEmailAttempt:";
        TimeSpan RecentAttemptWindow = TimeSpan.FromMinutes(15);

        if (string.IsNullOrWhiteSpace(email))
            return true; // Treat empty/whitespace as duplicate to prevent abuse

        var normalizedEmail = NormalizeEmail(email);
        var cacheKey = RecentEmailAttemptPrefix + normalizedEmail;

        if (_memoryCache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = RecentAttemptWindow,
        };

        _memoryCache.Set(cacheKey, true, options);

        return false;
    }
}