using System;
using ShortNet.Models;
using ShortNet.Interfaces;

namespace ShortNet.Services;

public class UrlService : IUrlService
{
    public Url CreateUrl(string? Url, string? Name, string? ShortUrl)
    {
        if (string.IsNullOrEmpty(Url))
        {
            throw new ArgumentException("Url field can not be empty");
        }
        if (string.IsNullOrEmpty(ShortUrl))
        {
            throw new ArgumentException("Short url field can not be empty");
        }

        return new Url()
        {
            LongUrl = Url,
            Name = Name ?? $"URL-{DateTime.Now.ToString()}",
            ShortUrl = ShortUrl
        };
    }

    public bool IsValid(string Url)
    {
        bool success = true;
        if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
        {
            success = false;
        }

        //split on "." to see if it contains more than 2 parts...
        if (Url.Split(".").Length<2)
        {
            success=false;
        }
        return success;
    }
}
