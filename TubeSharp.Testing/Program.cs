using System.Text.Json;
using TubeSharp.Json;

namespace TubeSharp.Testing;

public partial class Program
{
    public static async Task Main()
    {
        using YoutubeClient youtube = new();

        var channel = await youtube.Channels.GetVideosRangeAsync("UC_x5XG1OV2P6uZZ5FSM9Ttw").Take(50).ToListAsync();

        var json = JsonSerializer.Serialize(channel, TubeSharpJsonContext.Default.ListYoutubeVideo);

        Console.WriteLine(json);
        Console.WriteLine(channel.Count);
    }
}