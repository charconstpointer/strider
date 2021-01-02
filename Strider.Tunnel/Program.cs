using System;
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
            var upstream = new TcpListener(IPAddress.Loopback, 7777);
            upstream.Start();
            var downstream = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/strider")
                .Build();
            
            await downstream.StartAsync();
            
            while (true)
            {
                var client = await upstream.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    downstream.On<Tick>("UpTick", async tick =>
                    {
                        await client.GetStream().WriteAsync(tick.Payload.ToArray());
                    });
                    var stream = client.GetStream();
                    while (true)
                    {
                        var buffer = new byte[4096];
                        var n = stream.Read(buffer);
                        var tick = new Tick
                        {
                            Destination = "Tunnel",
                            Source = ((IPEndPoint) client.Client.RemoteEndPoint)?.Address.ToString(),
                            Payload = buffer.Take(n)
                        };
                        await downstream.SendAsync("Tick", tick);
                    }
                });
            }
        }
    }
}