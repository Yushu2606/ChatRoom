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
            Dictionary<int, string> beforeOneMessage = new Dictionary<int, string>();
            int beforeOneUser = 0;
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
                    Response data = JsonConvert.DeserializeObject<Response>(receivedString);
                    if (beforeOneMessage.ContainsKey(data.UUID) && beforeOneMessage[data.UUID] == data.Message)
                    {
                        continue;
                    }
                    ConsoleColor temp = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    if (data.UUID != beforeOneUser)
                    {
                        Console.Write($"{data.UserName}（{data.UUID}） ");
                    }
                    Console.WriteLine(data.DateTime);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(data.Message);
                    Console.ForegroundColor = temp;
                    beforeOneMessage[data.UUID] = data.Message;
                    beforeOneUser = data.UUID;
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
                byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Request()
                {
                    Message = line,
                    UserName = UserName
                }));
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}