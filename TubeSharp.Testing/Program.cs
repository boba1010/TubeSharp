using System.Text.Json;
using TubeSharp.Json;

namespace TubeSharp.Testing;

public partial class Program
{
    public static async Task Main()
    {
        using YoutubeClient youtube = new();

        var result = await youtube.Search.SearchAsync("Dotnet");

        var json = JsonSerializer.Serialize(result, TubeSharpJsonContext.Readable.SearchPage);

        Console.WriteLine(json);
    }
}