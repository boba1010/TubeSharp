using System.Text.Json;
using TubeSharp.Json;
using TubeSharp.Search;

namespace TubeSharp.Testing;

public partial class Program
{
    public static async Task Main()
    {
        using YoutubeClient youtube = new();

        var results = new List<ISearchResult>(30);

        await foreach(var result in youtube.Search.GetSearchResultRangeAsync("Minercaft").Take(30))
            results.Add(result);

        var json = JsonSerializer.Serialize(results, TubeSharpJsonContext.Readable.ListISearchResult);
        
        Console.WriteLine(json);

        await File.WriteAllTextAsync("response.json", json);
    }
}