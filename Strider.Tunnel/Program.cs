using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Strider.Messaging;

namespace Strider.Tunnel
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var downstreams = new Dictionary<string, TcpClient>();
            var cancellationToken = CancellationToken.None;
            var upstream = new HubConnectionBuilder()
                .WithUrl("http://ec2-35-178-211-187.eu-west-2.compute.amazonaws.com:4000/strider")
                .Build();
            upstream.On<RegisterClient>("ClientJoined", async register =>
            {
                var downstream = new TcpClient();
                await downstream.ConnectAsync(IPAddress.Loopback, 25565, cancellationToken);
                downstreams[register.Upstream] = downstream;
                _ = Task.Run(async () =>
                {
                    var stream = downstream.GetStream();
                    var buffer = new byte[4096];
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var n = await stream.ReadAsync(buffer, cancellationToken);
                        if (n <= 0) continue;
                        var tick = new Tick
                        {
                            Destination = register.Upstream,
                            Source = "Tunnel",
                            Payload = buffer.Take(n),
                        };
                        await upstream.SendAsync("UpTick", tick, cancellationToken: cancellationToken);
                        await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
                    }
                }, cancellationToken);
            });
            upstream.On<Tick>("Tick", async tick =>
            {
                downstreams.TryGetValue(tick.Source, out var downstream);
                await downstream!.GetStream().WriteAsync(tick.Payload.ToArray(), cancellationToken);
            });
            await upstream.StartAsync(cancellationToken);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}