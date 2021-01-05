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

        public async Task UpTick(Tick tick)
        {
            if (!_downstreams.TryGetValue(tick.Destination, out var socket))
            {
                _logger.LogCritical($"could not find upstream for {tick.Destination}");
                foreach (var keyValuePair in _downstreams)
                {
                    Console.WriteLine(keyValuePair.Key);
                }

                return;
            }

            await socket.GetStream().WriteAsync(tick.Payload.ToArray());
        }
    }
}