using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Strider.Messaging;

namespace Strider.Shelter
{
    public class StriderHub : Hub
    {
        private readonly TcpClient _downstream;
        private readonly ILogger<StriderHub> _logger;

        public StriderHub(TcpClient downstream, ILogger<StriderHub> logger)
        {
            _downstream = downstream;
            _logger = logger;
        }

        public async Task Tick(Tick tick)
        {
            _logger.LogInformation(tick.Payload.Count().ToString());
            var stream = _downstream.GetStream();
            _logger.LogInformation(_downstream.Connected.ToString());
            await stream.WriteAsync(tick.Payload.ToArray());
            _logger.LogInformation("donoz");
        }
    }
}