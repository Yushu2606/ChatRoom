using ChatRoom.Packet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatRoom
{
    internal class Server
    {
        private static void Main()
        {
            Dictionary<TcpClient, int> clients = new Dictionary<TcpClient, int>();
            int argCount = 0, maxCount = 0;
            System.Timers.Timer timer = new System.Timers.Timer()
            {
                Interval = 1000
            };
            timer.Elapsed += (_, _e) =>
            {
                Console.Title = $"聊天室服务器　　{argCount}/{maxCount}";
                argCount = 0;
            };
            timer.Start();
            TcpListener listenner = new TcpListener(IPAddress.Any, 19132);
            listenner.Start();
            Console.WriteLine($"开始监听{listenner.LocalEndpoint}");
            while (true)
            {
                TcpClient client = listenner.AcceptTcpClient();
                string clientIP = client.Client.RemoteEndPoint.ToString().Substring(0, client.Client.RemoteEndPoint.ToString().LastIndexOf(':'));
                clients.Add(client, clientIP.GetHashCode());
                _ = ThreadPool.QueueUserWorkItem((_) =>
                {
                    while (true)
                    {
                        byte[] bytes = new byte[ushort.MaxValue];
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            if (!stream.CanRead)
                            {
                                continue;
                            }
                            _ = stream.Read(bytes, 0, bytes.Length);
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"{client.Client.RemoteEndPoint}已断开连接：{ex}");
                            _ = clients.Remove(client);
                            return;
                        }
                        string receivedString = Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);
                        if (string.IsNullOrEmpty(receivedString))
                        {
                            continue;
                        }
                        Request data = JsonConvert.DeserializeObject<Request>(receivedString);
                        byte[] packetBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Response()
                        {
                            UUID = clients[client],
                            DateTime = DateTime.Now,
                            Message = data.Message,
                            UserName = data.UserName
                        }));
                        foreach (TcpClient otherClient in clients.Keys)
                        {
                            try
                            {
                                NetworkStream stream = otherClient.GetStream();
                                if (!stream.CanWrite)
                                {
                                    continue;
                                }
                                stream.Write(packetBytes, 0, packetBytes.Length);
                            }
                            catch (IOException ex)
                            {
                                Console.WriteLine($"{client.Client.RemoteEndPoint}已断开连接：{ex}");
                                _ = clients.Remove(client);
                                continue;
                            }
                        }
                        argCount++;
                        if (argCount > maxCount)
                        {
                            maxCount = argCount;
                        }
                    }
                });
                Console.WriteLine($"{client.Client.RemoteEndPoint}已连接");
            }
        }
    }
}