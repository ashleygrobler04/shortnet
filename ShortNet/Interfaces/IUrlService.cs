using System;
using ShortNet.Models;

namespace ShortNet.Interfaces;

public interface IUrlService
{
    /// <summary>
    /// Check if the given URl is valid.
    /// </summary>
    /// <param name="Url">The give url to check</param>
    /// <returns>True if the given url is valid, false otherwise</returns>
    bool IsValid(string Url);

    /// <summary>
    /// Create and save a url with the give data
    /// </summary>
    /// <param name="Url">The url to shorten</param>
    /// <param name="Name">The name of the url</param>
    /// <param name="ShortUrl">The shortened version of the url</param>
    /// <returns>A URL model that can be saed to the db if successfull</returns>
    Url CreateUrl(string? Url, string? Name, string? ShortUrl);
}
