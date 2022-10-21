using ChatRoom.Packet;
using ChatRoom.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
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

        internal static string UserName { get; set; }

        private static void Main(string[] args)
        {
            // 进程互斥
            _ = new Mutex(true, Assembly.GetExecutingAssembly().GetName().Name, out bool isNotRunning);
            if (!isNotRunning && !args.Contains("--multi") && !args.Contains("-m"))
            {
                _ = MessageBox.Show("你只能同时运行一个聊天室实例！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new InstanceNotFoundException("你只能同时运行一个聊天室实例！");
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
            Dictionary<string, string> beforeOne = new Dictionary<string, string>();
            Console.Clear();
            _ = ThreadPool.QueueUserWorkItem((_) =>
            {
                while (true)
                {
                    byte[] bytes = new byte[ushort.MaxValue];
                    try
                    {
                        NetworkStream stream = TcpClient.GetStream();
                        if (!stream.CanRead)
                        {
                            continue;
                        }
                        _ = stream.Read(bytes, 0, bytes.Length);
                    }
                    catch (IOException ex)
                    {
                        SimpleLogger.Error($"已断开连接：{ex.Message}");
                        int count = 0;
                        while (!TcpClient.Connected)
                        {
                            TcpClient = new TcpClient();
                            SimpleLogger.Info($"重连中：{++count}");
                            try
                            {
                                TcpClient.Connect(ip, 19132);
                            }
                            catch (SocketException ex1)
                            {
                                SimpleLogger.Error($"重连至{ip}失败：{ex1.Message}");
                            }
                        }
                        SimpleLogger.Info($"已重连至{ip}");
                        continue;
                    }
                    string receivedString = Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);
                    if (string.IsNullOrEmpty(receivedString))
                    {
                        continue;
                    }
                    switch (JsonConvert.DeserializeObject<Base<object>>(receivedString).Action)
                    {
                        case ActionType.Message:
                            Base<Message.Response> data = JsonConvert.DeserializeObject<Base<Message.Response>>(receivedString);
                            if (beforeOne.ContainsKey(data.Param.UUID) && beforeOne[data.Param.UUID] == data.Param.Message)
                            {
                                continue;
                            }
                            ConsoleColor temp = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Blue;
                            if (!beforeOne.ContainsKey("user") || data.Param.UUID != beforeOne["user"])
                            {
                                Console.Write($"{data.Param.UserName}（{data.Param.UUID}） ");
                            }
                            Console.WriteLine(data.Param.DateTime);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine(data.Param.Message);
                            Console.ForegroundColor = temp;
                            beforeOne[data.Param.UUID] = data.Param.Message;
                            beforeOne["user"] = data.Param.UUID;
                            break;
                    }
                }
            });
            SimpleLogger.Info($"已连接至{ip}");
            while (true)
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line) || !TcpClient.Connected)
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
                NetworkStream stream = TcpClient.GetStream();
                if (!stream.CanWrite)
                {
                    continue;
                }
                byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Base<Message.Request>()
                {
                    Action = ActionType.Message,
                    Param = new Message.Request()
                    {
                        Message = line,
                        UserName = UserName
                    }
                }));
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}