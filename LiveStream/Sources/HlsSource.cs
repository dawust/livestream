using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sources;

public class HlsSource(string playlistUri) : ISource
{
    private const int ReceiveSize = 16384;
    private readonly Logger<HlsSource> logger = new();
    private readonly HttpClient httpClient = new() { Timeout = Timeout.InfiniteTimeSpan, };

    public async Task SourceLoopAsync(AsyncBlockingQueue<IChunk> mediaQueue, CancellationToken cancellationToken)
    {
        var mediaPlaylistUrl = await ResolveMediaPlaylistAsync(playlistUri);

        var downloadedSegments = new HashSet<string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var playlistContent = await httpClient.GetStringAsync(mediaPlaylistUrl, cancellationToken);
                var lines = playlistContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    var segmentUri = trimmed.StartsWith("http")
                        ? trimmed
                        : new Uri(new Uri(mediaPlaylistUrl), trimmed).ToString();

                    if (downloadedSegments.Contains(segmentUri))
                    {
                        continue;
                    }

                    await using var stream = await httpClient.GetStreamAsync(segmentUri, cancellationToken);

                    while (true)
                    {
                        var buffer = new byte[ReceiveSize * 4];
                        
                        var length = await stream.ReadAsync(buffer, 0, ReceiveSize, cancellationToken);
                        if (length == 0)
                        {
                            break;
                        }

                        var chunk = new Chunk(buffer, length);
                        mediaQueue.Enqueue(chunk);
                    }

                    downloadedSegments.Add(segmentUri);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in HLS loop: {ex.Message}");
                await Task.Delay(2000, cancellationToken);
            }
        }
    }

    private async Task<string> ResolveMediaPlaylistAsync(string playlistUrl)
    {
        var content = await httpClient.GetStringAsync(playlistUrl);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("#EXT-X-STREAM-INF"))
            {
                continue; // next line will be the URI
            }

            if (!line.StartsWith("#"))
            {
                // found child playlist URI
                var mediaUri = line.StartsWith("http")
                    ? line
                    : new Uri(new Uri(playlistUrl), line).ToString();

                logger.Info($"Resolved media playlist: {mediaUri}");
                return mediaUri;
            }
        }

        // no variant playlists → already a media playlist
        return playlistUrl;
    }
}