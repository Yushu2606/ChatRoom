using ChatRoom.Packet;
using ChatRoom.Utils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;

namespace ChatRoom
{
    internal class Client
    {
        internal static TcpClient TcpClient { get; private set; }
        private static void Main(string[] args)
        {
            // 进程互斥
            _ = new Mutex(true, Assembly.GetExecutingAssembly().GetName().Name, out bool isNotRunning);
            if (!isNotRunning && !args.Contains("--multi") && !args.Contains("-m"))
            {
                _ = MessageBox.Show("你只能同时运行一个聊天室实例！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            // 修复中文输入输出
            Console.InputEncoding = Encoding.GetEncoding(936);
            Console.OutputEncoding = Encoding.GetEncoding(936);
        start:
            Console.Title = "聊天室";
            TcpClient = new TcpClient();
            Console.Write("请输入服务器地址：");
            string ip = Console.ReadLine();
            try
            {
                TcpClient.Connect(ip, 19132);
            }
            catch (SocketException ex)
            {
                SimpleLogger.Error($"连接至{ip}失败：{ex.Message}");
                goto start;
            }
            Console.Title = $"聊天室：{ip}";
            new Thread(() =>
            {
                Console.Clear();
                SimpleLogger.Info($"已连接至{ip}");
                while (true)
                {
                    NetworkStream stream = TcpClient.GetStream();
                    if (!stream.CanRead)
                    {
                        continue;
                    }
                    byte[] bytes = new byte[ushort.MaxValue];
                    try
                    {
                        _ = stream.Read(bytes, 0, bytes.Length);
                    }
                    catch (IOException ex)
                    {
                        if (TcpClient.Connected)
                        {
                            throw;
                        }
                        SimpleLogger.Error($"已断开连接：{ex.Message}");
                        int count = 0;
                        while (!TcpClient.Connected)
                        {
                            TcpClient = new TcpClient();
                            try
                            {
                                SimpleLogger.Info($"重连中：{++count}");
                                TcpClient.Connect(ip, 15743);
                            }
                            catch (SocketException ex1)
                            {
                                SimpleLogger.Error($"重连至{ip}失败：{ex1.Message}");
                            }
                        }
                        SimpleLogger.Info($"已重连至{ip}");
                        return;
                    }
                    string receivedData = Console.OutputEncoding.GetString(bytes).Replace("\0", string.Empty);
                    Base<object> data = JsonConvert.DeserializeObject<Base<object>>(receivedData);
                    switch (data.Action)
                    {
                        case Packet.Action.Message:
                            {
                                Base<Message.Response> realData = JsonConvert.DeserializeObject<Base<Message.Response>>(receivedData);
                                ConsoleColor temp = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine($"{realData.Param.UserName}（{realData.Param.UUID}） {realData.Param.DateTime}");
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($"{realData.Param.Message}");
                                Console.ForegroundColor = temp;
                                break;
                            }
                    }
                }
            }).Start();
            while (true)
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                if (!TcpClient.Connected)
                {
                    continue;
                }
                if (line.StartsWith("/"))
                {
                    try
                    {
                        Command.Process(line.Substring(1).Split(' '), Command.Commands);
                    }
                    catch (ArgumentException ex)
                    {
                        SimpleLogger.Error($"命令运行失败：{ex.Message}");
                    }
                    continue;
                }
                byte[] bytes = Console.InputEncoding.GetBytes(JsonConvert.SerializeObject(new Base<Message.Request>()
                {
                    Action = Packet.Action.Message,
                    Param = new Message.Request()
                    {
                        Message = line
                    }
                }));
                NetworkStream stream = TcpClient.GetStream();
                if (!stream.CanWrite)
                {
                    continue;
                }
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(); // 刷新缓冲区
            }
        }
    }
}