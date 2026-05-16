using System;
using ShortNet.Interfaces;
using ShortNet.Models;

namespace ShortNet.Services;

public class UrlService : IUrlService
{
    public Url CreateUrl(string url, string? name, string shortUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Url field cannot be empty.", nameof(url));
        }

        if (string.IsNullOrWhiteSpace(shortUrl))
        {
            throw new ArgumentException("Short url field cannot be empty.", nameof(shortUrl));
        }

        if (!IsValid(url))
        {
            throw new ArgumentException("Url must be a valid absolute HTTP or HTTPS URL.", nameof(url));
        }

        return new Url
        {
            LongUrl = url.Trim(),
            Name = string.IsNullOrWhiteSpace(name)
                ? $"URL-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : name.Trim(),
            ShortUrl = shortUrl.Trim()
        };
    }

    public bool IsValid(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uriResult))
        {
            return false;
        }

        return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
    }
}
