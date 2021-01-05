using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Strider.Messaging;

namespace Strider.Shelter
{
    public class ShelterService : BackgroundService
    {
        private readonly IHubContext<StriderHub> _hubContext;
        private readonly ILogger<ShelterService> _logger;
        private readonly IDictionary<string, TcpClient> _upstreams;

        public ShelterService(IHubContext<StriderHub> hubContext, ILogger<ShelterService> logger,
            IDictionary<string, TcpClient> upstreams)
        {
            _hubContext = hubContext;
            _logger = logger;
            _upstreams = upstreams;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.Any, 8133);
            listener.Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                var socket = await listener.AcceptTcpClientAsync();
                var source = ((IPEndPoint) socket.Client.RemoteEndPoint)?.Address.ToString() +
                             ((IPEndPoint) socket.Client.RemoteEndPoint)?.Port;
                _upstreams[source] = socket;
                _logger.LogInformation($"New client {source} joined🦽!");
                await _hubContext.Clients.All.SendAsync("ClientJoined", new RegisterClient
                {
                    Upstream = source
                }, stoppingToken);
                _ = Task.Factory.StartNew(async _ => await HandleClient(socket, source, stoppingToken), stoppingToken,
                    TaskCreationOptions.LongRunning);
            }
        }

        private async Task HandleClient(TcpClient socket, string source, CancellationToken stoppingToken)
        {
            var buffer = new byte[4096 * 32];
            _upstreams[source] = socket;
            while (!stoppingToken.IsCancellationRequested)
            {
                var n = await socket.GetStream().ReadAsync(buffer, stoppingToken);
                if (n == 0)
                {
                    //Client has disconnected
                    break;
                }

                try
                {
                    await _hubContext.Clients.All.SendAsync("Tick", new Tick
                    {
                        Destination = "Tunnel",
                        Source = source,
                        Payload = buffer.Take(n),
                    }, stoppingToken);
                    _logger.LogInformation(n.ToString());
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e.Message);
                }
            }
        }
    }
}