using ChatRoom.Packet;
using ChatRoom.Utils;
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
            // 修复中文输入输出
            Console.InputEncoding = Encoding.GetEncoding(936);
            Console.OutputEncoding = Encoding.GetEncoding(936);
            Dictionary<TcpClient, UserData> clients = new Dictionary<TcpClient, UserData>();
            TcpListener listenner = new TcpListener(IPAddress.Any, 19132);
            listenner.Start();
            Console.WriteLine($"开始监听{listenner.LocalEndpoint}");
            while (true)
            {
                TcpClient client = listenner.AcceptTcpClient();
                if (client.Connected)
                {
                    string clientIP = client.Client.RemoteEndPoint.ToString().Substring(0, client.Client.RemoteEndPoint.ToString().LastIndexOf(':'));
                    clients.Add(client, new UserData()
                    {
                        UserName = new Random().Next().ToString("x"),   // 随机用户名
                        UUID = clientIP.GetHashCode().ToString("x")
                    });
                    Console.WriteLine($"{client.Client.RemoteEndPoint}已连接");
                    new Thread(() =>
                    {
                        while (true)
                        {
                            byte[] bytes = new byte[ushort.MaxValue];
                            try
                            {
                                _ = client.GetStream().Read(bytes, 0, bytes.Length);
                            }
                            catch (IOException ex)
                            {
                                if (client.Connected)
                                {
                                    throw;
                                }
                                Console.WriteLine($"{client.Client.RemoteEndPoint}已断开连接：{ex.Message}");
                                _ = clients.Remove(client);
                                return;
                            }
                            catch (InvalidOperationException ex)
                            {
                                if (client.Connected)
                                {
                                    throw;
                                }
                                Console.WriteLine($"{client.Client.RemoteEndPoint}已断开连接：{ex.Message}");
                                _ = clients.Remove(client);
                                return;
                            }
                            string receivedData = Console.OutputEncoding.GetString(bytes).Replace("\0", string.Empty);
                            Base<object> data = JsonConvert.DeserializeObject<Base<object>>(receivedData);
                            switch (data.Action)
                            {
                                case Packet.Action.Message:
                                    {
                                        Base<Message.Request> realData = JsonConvert.DeserializeObject<Base<Message.Request>>(receivedData);
                                        byte[] packetBytes = Console.OutputEncoding.GetBytes(JsonConvert.SerializeObject(new Base<Message.Response>()
                                        {
                                            Action = Packet.Action.Message,
                                            Param = new Message.Response()
                                            {
                                                DateTime = DateTime.Now,
                                                Message = realData.Param.Message,
                                                UserName = clients[client].UserName,
                                                UUID = clients[client].UUID
                                            }
                                        }));
                                        foreach (TcpClient otherClient in clients.Keys)
                                        {
                                            if (otherClient == client)
                                            {
                                                continue;   // 跳过自己
                                            }
                                            NetworkStream stream = otherClient.GetStream();
                                            if (!stream.CanWrite)
                                            {
                                                continue;
                                            }
                                            stream.Write(packetBytes, 0, packetBytes.Length);
                                            stream.Flush(); // 刷新缓冲区
                                        }
                                        break;
                                    }
                                case Packet.Action.SetUserName:
                                    {
                                        Base<UserName.Request> realData = JsonConvert.DeserializeObject<Base<UserName.Request>>(receivedData);
                                        clients[client].UserName = realData.Param.UserName;
                                        break;
                                    }
                            }
                        }
                    }).Start();
                }
            }
        }
    }
}