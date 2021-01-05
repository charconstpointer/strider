using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Strider.Messaging;

namespace Strider.Tunnel
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var downstreams = new Dictionary<string, TcpClient>();
            var upstream = new HubConnectionBuilder()
                .WithUrl("http://ec2-35-178-211-187.eu-west-2.compute.amazonaws.com:4000/strider")
                // .WithUrl("http://localhost:5000/strider")
                .Build();
            upstream.On<RegisterClient>("ClientJoined", async register =>
            {
                var downstream = new TcpClient();
                await downstream.ConnectAsync(IPAddress.Loopback, 25565);
                downstreams[register.Upstream] = downstream;
                _ = Task.Run(async () =>
                {
                    var stream = downstream.GetStream();
                    var buffer = new byte[4096];
                    while (true)
                    {
                        if (!downstream.Connected)
                        {
                            Console.WriteLine(downstream.Connected);
                            await upstream.SendAsync("ClientDisconnected",
                                new ClientDisconnected
                                {
                                    Id = register.Upstream
                                });
                            break;
                        }

                        var n = await stream.ReadAsync(buffer);
                        if (n <= 0) continue;
                        var tick = new Tick
                        {
                            Destination = register.Upstream,
                            Source = "Tunnel",
                            Payload = buffer.Take(n),
                        };
                        await upstream.SendAsync("UpTick", tick);
                        await Task.Delay(TimeSpan.FromMilliseconds(5));
                    }
                });
            });
            upstream.On<Tick>("Tick", async tick =>
            {
                downstreams.TryGetValue(tick.Source, out var downstream);
                await downstream!.GetStream().WriteAsync(tick.Payload.ToArray());
            });
            await upstream.StartAsync();
            Console.WriteLine(upstream.State);

            Console.ReadKey();
        }
    }
}