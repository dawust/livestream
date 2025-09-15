#pragma warning disable CA1416
using System;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sources;

public class QuicSource(string host, int port) : ISource
{
    private const int ReceiveSize = 16384;
    private readonly Logger<QuicSource> logger = new();

    public async Task SourceLoopAsync(AsyncBlockingQueue<IChunk> mediaQueue, CancellationToken cancellationToken)
    {
        var clientConnectionOptions = new QuicClientConnectionOptions
        {
            // End point of the server to connect to.
            RemoteEndPoint = new DnsEndPoint(host, port),

            // Used to abort stream if it's not properly closed by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultStreamErrorCode = 0x0A, // Protocol-dependent error code.

            // Used to close the connection if it's not done by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultCloseErrorCode = 0x0B, // Protocol-dependent error code.

            // Optionally set limits for inbound streams.
            MaxInboundUnidirectionalStreams = 100,
            MaxInboundBidirectionalStreams = 100,

            // Same options as for client side SslStream.
            ClientAuthenticationOptions = new SslClientAuthenticationOptions
            {
                // List of supported application protocols.
                ApplicationProtocols = [new SslApplicationProtocol("protocol-name")],
                // The name of the server the client is trying to connect to. Used for server certificate validation.
                ClientCertificates = [
                    new X509Certificate2("./quic.pfx", "test123",
                        X509KeyStorageFlags.MachineKeySet |
                        X509KeyStorageFlags.Exportable |
                        X509KeyStorageFlags.EphemeralKeySet)
                ],
                TargetHost = host,
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
            }
        };
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var connection = await QuicConnection.ConnectAsync(
                    options: clientConnectionOptions,
                    cancellationToken: cancellationToken);

                logger.Info($"Connected to QUIC server {host}:{port}");

                // Accept streams from server
                var stream = await connection.AcceptInboundStreamAsync(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var buffer = new byte[ReceiveSize];

                    var length = await stream.ReadAsync(buffer, cancellationToken);
                    if (length == 0)
                    {
                        break;
                    }

                    var chunk = new Chunk(buffer, length);
                    mediaQueue.Enqueue(chunk);
                }
            }
            catch (Exception e)
            {
                logger.Error($"QUIC source failed {e.Message}");
            }
        }
    }
}