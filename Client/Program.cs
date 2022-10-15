using ChatRoom.Packet;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;

namespace ChatRoom
{
    internal class Client
    {
        private static void Main()
        {
            // 进程互斥
            new Mutex(true, Assembly.GetExecutingAssembly().GetName().Name, out bool isNotRunning);
            if (!isNotRunning)
            {
                MessageBox.Show("你只能同时运行一个聊天室实例！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            // 修复中文输入输出
            Console.InputEncoding = Encoding.GetEncoding(936);
            Console.OutputEncoding = Encoding.GetEncoding(936);
        start:
            TcpClient client = new TcpClient();
            {
                Console.Write("请输入服务器地址：");
                string ip = Console.ReadLine();
                try
                {
                    client.Connect(ip, 15743);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"连接至{ip}失败：{ex.Message}");
                    goto start;
                }
                Console.Write("请输入用户名：");
                string userName = Console.ReadLine();
                if (string.IsNullOrEmpty(userName))
                {
                    userName = new Random().Next().ToString("x");   // 随机用户名
                }
                string packet = JsonConvert.SerializeObject(new Base<LogInOrOut.Request>()
                {
                    Action = Packet.Action.Login,
                    Param = new LogInOrOut.Request()
                    {
                        UserName = userName
                    }
                });
                byte[] bytes = Console.InputEncoding.GetBytes(packet);
                NetworkStream stream = client.GetStream();
                if (!stream.CanWrite)
                {
                    Console.WriteLine($"连接至{ip}失败：无法写入数据流");
                    client.Close();
                    goto start;
                }
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(); // 刷新缓冲区
                Console.Clear();
            }
            new Thread(() =>
            {
                while (true)
                {
                    NetworkStream stream = client.GetStream();
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
                        if (client.Available != 0)
                        {
                            throw;
                        }
                        Console.WriteLine($"已断开连接：{ex.Message}");
                        Console.Write("按任意键关闭此窗口. . .");
                        Console.ReadLine();
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
                string packet = JsonConvert.SerializeObject(new Base<Message.Request>()
                {
                    Action = Packet.Action.Message,
                    Param = new Message.Request()
                    {
                        Message = line
                    }
                });
                byte[] bytes = Console.InputEncoding.GetBytes(packet);
                NetworkStream stream = client.GetStream();
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