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


            while (true)
            {
                var client = await upstream.AcceptTcpClientAsync();
                Console.WriteLine(
                    $"New client connected {((IPEndPoint) client.Client.RemoteEndPoint)?.Address.ToString() + ((IPEndPoint) client.Client.RemoteEndPoint)?.Port}");
                _ = Task.Run(async () =>
                {
                    var msgCount = 0;
                    var downstream = new HubConnectionBuilder()
                        .WithUrl("https://localhost:5001/strider")
                        .Build();

                    await downstream.StartAsync();
                    downstream.On<Tick>("UpTick",
                        async tick => { await client.GetStream().WriteAsync(tick.Payload.ToArray()); });
                    var stream = client.GetStream();
                    var buffer = new byte[4096];
                    while (true)
                    {
                        if (!client.Connected)
                        {
                            Console.WriteLine(client.Connected);
                            await downstream.SendAsync("ClientDisconnected",
                                new ClientDisconnected
                                {
                                    Id = ((IPEndPoint) client.Client.RemoteEndPoint)?.Address.ToString() +
                                         ((IPEndPoint) client.Client.RemoteEndPoint)?.Port
                                });
                            break;
                        }


                        var n = stream.Read(buffer);
                        var tick = new Tick
                        {
                            Destination = "Tunnel",
                            Source = ((IPEndPoint) client.Client.RemoteEndPoint)?.Address.ToString() +
                                     ((IPEndPoint) client.Client.RemoteEndPoint)?.Port,
                            Payload = buffer.Take(n),
                            Register = msgCount == 0
                        };
                        Console.WriteLine(msgCount == 0);
                        msgCount++;
                        await downstream.SendAsync("Tick", tick);
                    }
                });
            }
        }
    }
}