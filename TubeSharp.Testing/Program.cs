using System.Text.Json;
using TubeSharp.Json;

namespace TubeSharp.Testing;

public partial class Program
{
    public static async Task Main()
    {
        using YoutubeClient youtube = new();

        var channel = await youtube.Channels.GetHomeAsync("UC_x5XG1OV2P6uZZ5FSM9Ttw");

        var json = JsonSerializer.Serialize(channel, TubeSharpJsonContext.Default.YoutubeChannelHome);

        Console.WriteLine(json);
    }
}