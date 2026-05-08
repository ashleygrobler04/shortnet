using System;

namespace ShortNet.Models;

public class Url
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LongUrl { get; set; } = string.Empty; //the actual URL
    public string ShortUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty; //Will be the user name
}
