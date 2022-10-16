using ChatRoom.Packet;
using ChatRoom.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private static void Main()
        {
            // 进程互斥
            _ = new Mutex(true, Assembly.GetExecutingAssembly().GetName().Name, out bool isNotRunning);
            if (!isNotRunning)
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
            {
                Console.Write("请输入服务器地址：");
                string ip = Console.ReadLine();
                try
                {
                    TcpClient.Connect(ip, 15743);
                }
                catch (SocketException ex)
                {
                    SimpleLogger.Error($"连接至{ip}失败：{ex.Message}");
                    goto start;
                }
                Console.Title = $"聊天室 - {ip}";
                Console.Write("请输入用户名：");
                string userName = Console.ReadLine();
                if (string.IsNullOrEmpty(userName))
                {
                    userName = new Random().Next().ToString("x");   // 随机用户名
                }
                string packet = JsonConvert.SerializeObject(new Base<LogIn.Request>()
                {
                    Action = Packet.Action.Login,
                    Param = new LogIn.Request()
                    {
                        UserName = userName
                    }
                });
                byte[] bytes = Console.InputEncoding.GetBytes(packet);
                NetworkStream stream = TcpClient.GetStream();
                if (!stream.CanWrite)
                {
                    SimpleLogger.Error($"连接至{ip}失败：无法写入数据流");
                    TcpClient.Close();
                    goto start;
                }
                Console.Title = $"聊天室 - {ip}:{userName}";
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(); // 刷新缓冲区
                Console.Clear();
            }
            new Thread(() =>
            {
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
                        if (TcpClient.Available != 0)
                        {
                            throw;
                        }
                        SimpleLogger.Error($"已断开连接：{ex.Message}");
                        Console.Write("按任意键关闭此窗口. . .");
                        _ = Console.ReadLine();
                        Environment.Exit(0);
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
                if (line.StartsWith("/"))
                {
                    int deep = 1;
                    List<string> args = line.ToUpper().Substring(1).Split(' ').ToList();
                    try
                    {
                        Command.Processing(deep, args, Command.Commands);
                    }
                    catch (ArgumentException ex)
                    {
                        SimpleLogger.Error($"命令运行失败：{ex.Message}");
                    }
                    continue;
                }
                string packet = JsonConvert.SerializeObject(new Base<Message.Request>()
                {
                    Action = Packet.Action.Message,
                    Param = new Message.Request()
                    {
                        Message = line
                    }
                });
                byte[] bytes = Console.InputEncoding.GetBytes(packet);
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