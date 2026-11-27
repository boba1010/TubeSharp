using System.Text.Json;
using TubeSharp.Json;

namespace TubeSharp.Testing;

public partial class Program
{
    public static async Task Main()
    {
        using YoutubeClient youtube = new();

        var playlist = await youtube.Playlists.GetVideosAsync("PLxLYo5_7D3SdgAFYzYETPDDVBt6DRqVE0");

        var json = JsonSerializer.Serialize(playlist, TubeSharpJsonContext.Default.PlaylistPage);

        Console.WriteLine(json);
    }
}