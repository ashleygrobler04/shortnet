using ShortNet.Models;
using ShortNet.Services;

namespace Tests;

public class UrlServiceTest
{
    [Fact]
    public void CreateUrlTest()
    {
        var url = "https://forum.audiogames.net";
        var shortUrl = "forum";
        var service = new UrlService();
        var result = service.CreateUrl(url, "Test", shortUrl);
        Assert.NotNull(result);
        Assert.Equal(shortUrl, result.ShortUrl);
    }

    [Fact]
    public void IsValidTest()
    {
        var url = "https://forum.audiogames.net";
        var service=new UrlService();
        var result=service.IsValid(url);
        Assert.True(result);
    }

    //Test with invalid url
    [Fact]
    public void IsInvalidTestUrl()
    {
        var service=new UrlService();
        var url="forum.audiogames.net"; //still the dot but not any https or http
        var result=service.IsValid(url);
        Assert.False(result);
    }
}
