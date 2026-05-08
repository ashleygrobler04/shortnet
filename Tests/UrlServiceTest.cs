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
        var result=service.CreateUrl(url,"Test",shortUrl);
        Assert.NotNull(result);
        Assert.Equal(shortUrl,result.ShortUrl);
    }
}
