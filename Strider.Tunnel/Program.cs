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
                // .WithUrl("http/ec2-35-178-211-187.eu-west-2.compute.amazonaws.com:5001/strider")
                .WithUrl("https://localhost:5001/strider")
                .Build();
            upstream.On<Tick>("Tick", async tick =>
            {
                Console.WriteLine("Tick");
                TcpClient downstream;
                if (tick.Register)
                {
                    Console.WriteLine("Register");
                    downstream = new TcpClient();
                    await downstream.ConnectAsync(IPAddress.Loopback, 25565);
                    downstreams[tick.Source] = downstream;
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
                                        Id = tick.Source
                                    });
                                break;
                            }

                            Console.WriteLine("Reading");
                            var n = stream.Read(buffer);
                            var tickk = new Tick
                            {
                                Destination = tick.Source,
                                Source = "Tunnel",
                                Payload = buffer.Take(n),
                            };
                            await upstream.SendAsync("UpTick", tickk);
                        }
                    });
                }
                else
                {
                    downstreams.TryGetValue(tick.Source, out downstream);
                }

                await downstream!.GetStream().WriteAsync(tick.Payload.ToArray());
            });
            await upstream.StartAsync();
            Console.WriteLine(upstream.State);

            Console.ReadKey();
        }
    }
}