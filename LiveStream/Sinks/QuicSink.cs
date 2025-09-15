#pragma warning disable CA1416
using System;
using System.Net.Quic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sinks;

public class QuicSink(int port) : ISink
{
    private QuicListener listener;
    private readonly Logger<QuicSink> logger = new();

    public async Task SinkLoopAsync(IConnectionManager connectionManager, CancellationToken cancellationToken)
    {
        logger.Info($"Start QUIC sink on {port}");
        var serverConnectionOptions = new QuicServerConnectionOptions
        {
            // Used to abort stream if it's not properly closed by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultStreamErrorCode = 0x0A, // Protocol-dependent error code.

            // Used to close the connection if it's not done by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultCloseErrorCode = 0x0B, // Protocol-dependent error code.

            // Same options as for server side SslStream.
            ServerAuthenticationOptions = new SslServerAuthenticationOptions
            {
                // Specify the application protocols that the server supports. This list must be a subset of the protocols specified in QuicListenerOptions.ApplicationProtocols.
                ApplicationProtocols = [new SslApplicationProtocol("protocol-name")],
                // Server certificate, it can also be provided via ServerCertificateContext or ServerCertificateSelectionCallback.
                ServerCertificate = new X509Certificate2("./quic.pfx", "test123",
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.Exportable |
                    X509KeyStorageFlags.EphemeralKeySet),
                ClientCertificateRequired = false,
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
            }
        };

        try
        {
            listener = await QuicListener.ListenAsync(
                options: new QuicListenerOptions
                {
                    ListenEndPoint = new IPEndPoint(IPAddress.Any, port),
                    ApplicationProtocols = [new SslApplicationProtocol("protocol-name")],
                    ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(serverConnectionOptions),
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.Error($"QUIC sink failed {e.Message}");
            throw;
        }

        logger.Info($"QUIC sink listening on {port}");

        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await listener.AcceptConnectionAsync(cancellationToken);
            _ = HandleClientAsync(client, connectionManager, cancellationToken);
        }
    }

    private async Task HandleClientAsync(QuicConnection client, IConnectionManager connectionManager, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = connectionManager.CreateConnection();
            await using var stream = await client.OpenOutboundStreamAsync(QuicStreamType.Unidirectional, cancellationToken);

            var counter = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var chunk = await connection.ReadBlockingAsync();

                await stream.WriteAsync(chunk.Buffer, 0, chunk.Length, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                
                counter++;
                if (counter == 50)
                {
                    logger.Info($"{client.RemoteEndPoint}; Sent {chunk.Length} Bytes; Queue {connection.Size}");
                    counter = 0;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning($"Client disconnected: {ex.Message}");
        }
    }
}