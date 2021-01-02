using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Strider.Messaging;

namespace Strider.Shelter
{
    public class StriderService : BackgroundService
    {
        private readonly TcpClient _downstream;
        private readonly IHubContext<StriderHub> _context;

        public StriderService(TcpClient downstream, IHubContext<StriderHub> context)
        {
            _downstream = downstream;
            _context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var buffer = new byte[4096];
            while (!stoppingToken.IsCancellationRequested)
            {
                var n = await _downstream.GetStream().ReadAsync(buffer, stoppingToken);
                await _context.Clients.All.SendAsync("UpTick", new Tick
                {
                    Destination = "Tunnel",
                    Source = "Shelter",
                    Payload = buffer.Take(n)
                }, cancellationToken: stoppingToken);
            }
        }
    }
}