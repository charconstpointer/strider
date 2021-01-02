using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Strider.Messaging;

namespace Strider.Shelter
{
    public class StriderHub : Hub
    {
        private readonly IDictionary<string, TcpClient> _downstreams;
        private readonly ILogger<StriderHub> _logger;
        private readonly IHubContext<StriderHub> _hubContext;

        public StriderHub(ILogger<StriderHub> logger, IDictionary<string, TcpClient> downstreams,
            IHubContext<StriderHub> hubContext)
        {
            _logger = logger;
            _downstreams = downstreams;
            _hubContext = hubContext;
        }

        public Task ClientDisconnected(ClientDisconnected clientDisconnected)
        {
            _logger.LogInformation($"Client {clientDisconnected.Id} disconnected");
            var removed = _downstreams.Remove(clientDisconnected.Id);
            if (!removed)
            {
                _logger.LogCritical($"Could not delete client {clientDisconnected.Id}");
            }

            return Task.CompletedTask;
        }

        public async Task Tick(Tick tick)
        {
            if (!tick.Payload.Any())
            {
                return;
            }

            if (tick.Register)
            {
                _logger.LogInformation($"Registering {tick.Source}");
                await Groups.AddToGroupAsync(Context.ConnectionId, tick.Source);
            }
            var source = tick.Source;
            if (!_downstreams.TryGetValue(source, out var socket))
            {
                _logger.LogCritical($"Downstream not found for {source}, creating one");
                var downstream = new TcpClient();
                await downstream.ConnectAsync(IPAddress.Loopback, 25565);
                _downstreams[source] = downstream;
                socket = downstream;

                _ = Task.Run(async () =>
                {
                    var buffer = new byte[4096];
                    while (true)
                    {
                        var n = await socket.GetStream().ReadAsync(buffer);
                        if (n == 0)
                        {
                            break;
                        }

                        try
                        {
                            var destination = ((IPEndPoint) socket.Client.RemoteEndPoint)?.Address.ToString() +
                                              ((IPEndPoint) socket.Client.RemoteEndPoint)?.Port;
                            await _hubContext.Clients.Group(source).SendAsync("UpTick", new Tick
                            {
                                Destination = source,
                                Source = "Shelter",
                                Payload = buffer.Take(n)
                            });
                        }
                        catch (Exception e)
                        {
                            _logger.LogCritical(e.Message);
                        }
                    }
                });
            }

            if (!socket.Connected)
            {
                _logger.LogCritical($"Downstream connection is dead for {source}");
                return;
            }

            var stream = socket.GetStream();
            try

            {
                await stream.WriteAsync(tick.Payload.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
                return;
            }

            _logger.LogInformation($"Wrote {tick.Payload.Count()} bytes to downstream");
        }
    }
}