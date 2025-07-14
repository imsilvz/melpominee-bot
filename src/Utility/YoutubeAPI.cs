using Melpominee.Models;
using Melpominee.Services;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace Melpominee.Utility;

public class YoutubeAPI
{
    private readonly HttpClient _client;
    private readonly string _baseUrl = "https://www.googleapis.com/youtube/v3";

    public YoutubeAPI(HttpClient client)
    {
        _client = client;
    }

    public async Task<YoutubeMetadata?> GetVideoDetails(string videoId)
    {
        // build querystring
        Dictionary<string, string> queryParams = new();
        queryParams["id"] = videoId;
        queryParams["part"] = "snippet";
        queryParams["key"] = SecretStore.Instance.GetSecret("YOUTUBE_API_KEY");

        // get api url
        string fullUrl = $"{_baseUrl}/videos?{string.Join(
            "&", 
            queryParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"
        ))}";

        // make request, return null if not found or rate limited
        var result = await _client.GetAsync(fullUrl);
        if (result.StatusCode != HttpStatusCode.OK)
            return null;

        // traverse content
        var jsonResult = await result.Content.ReadAsStringAsync();  
        var videoData = JsonNode.Parse(jsonResult)?
            ["items"]?.AsArray()
            .FirstOrDefault();
        if (videoData == null)
            return null;
        var videoSnippet = videoData["snippet"];

        // return metadata we care about
        return new()
        {
            VideoId = videoData["id"]?.ToString(),
            Title = videoSnippet?["title"]?.ToString(),
            Description = videoSnippet?["description"]?.ToString()
        };
    }
}
