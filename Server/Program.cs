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
            Dictionary<TcpClient, UserData> pairs = new Dictionary<TcpClient, UserData>();
            TcpListener listenner = new TcpListener(IPAddress.Any, 15743);
            listenner.Start();
            while (true)
            {
                TcpClient client = listenner.AcceptTcpClient();
                if (client.Connected)
                {
                    string clientRemoteEndPoint = client.Client.RemoteEndPoint.ToString();
                    string clientIP = clientRemoteEndPoint.Substring(0, clientRemoteEndPoint.LastIndexOf(':'));
                    Console.WriteLine($"{clientIP}已连接");
                    new Thread(() =>
                    {
                        bool isFirstTime = true;
                        while (true)
                        {
                            byte[] bytes = new byte[ushort.MaxValue];
                            try
                            {
                                _ = client.GetStream().Read(bytes, 0, bytes.Length);
                            }
                            catch (IOException ex)
                            {
                                Console.WriteLine($"{clientIP}已断开连接：{ex.Message}");
                                if (!isFirstTime)
                                {
                                    _ = pairs.Remove(client);
                                }
                                return;
                            }
                            catch (InvalidOperationException ex)
                            {
                                if (client.Connected)
                                {
                                    throw;
                                }
                                Console.WriteLine($"{clientIP}已断开连接：{ex.Message}");
                                if (!isFirstTime)
                                {
                                    _ = pairs.Remove(client);
                                }
                                return;
                            }
                            string receivedData = Console.OutputEncoding.GetString(bytes).Replace("\0", string.Empty);
                            Base<object> data = JsonConvert.DeserializeObject<Base<object>>(receivedData);
                            switch (data.Action)
                            {
                                case Packet.Action.Login:
                                    {
                                        if (!isFirstTime)
                                        {
                                            break;
                                        }
                                        Base<LogInOrOut.Request> realData = JsonConvert.DeserializeObject<Base<LogInOrOut.Request>>(receivedData);
                                        pairs.Add(client, new UserData()
                                        {
                                            UserName = realData.Param.UserName,
                                            UUID = clientIP.GetHashCode().ToString("x")
                                        });
                                        isFirstTime = false;
                                        Console.WriteLine($"{realData.Param.UserName}（{clientIP}）已登录");
                                        break;
                                    }
                                case Packet.Action.Message:
                                    {
                                        if (isFirstTime)
                                        {
                                            break;
                                        }
                                        Base<Message.Request> realData = JsonConvert.DeserializeObject<Base<Message.Request>>(receivedData);
                                        string packet = JsonConvert.SerializeObject(new Base<Message.Response>()
                                        {
                                            Action = Packet.Action.Message,
                                            Param = new Message.Response()
                                            {
                                                DateTime = DateTime.Now,
                                                Message = realData.Param.Message,
                                                UserName = pairs[client].UserName,
                                                UUID = pairs[client].UUID
                                            }
                                        });
                                        byte[] packetBytes = Console.OutputEncoding.GetBytes(packet);
                                        foreach (TcpClient otherClient in pairs.Keys)
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
                            }
                        }
                    }).Start();
                }
            }
        }
    }
}