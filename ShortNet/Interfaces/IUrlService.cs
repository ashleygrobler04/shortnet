using ShortNet.Models;

namespace ShortNet.Interfaces;

public interface IUrlService
{
    /// <summary>
    /// Check whether the given URL is a valid absolute HTTP or HTTPS URI.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the URL is valid; otherwise false.</returns>
    bool IsValid(string url);

    /// <summary>
    /// Create a validated Url model from the provided user input.
    /// </summary>
    /// <param name="url">The long URL to shorten.</param>
    /// <param name="name">The optional display name for the URL.</param>
    /// <param name="shortUrl">The shortened URL key.</param>
    /// <returns>A validated Url model ready to persist.</returns>
    Url CreateUrl(string url, string? name, string shortUrl);
}
