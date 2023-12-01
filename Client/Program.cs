using ChatRoom.Utils;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ChatRoom;

internal class Client
{
    internal static string UserName { get; set; }

    private static async Task<int> Main(string[] args)
    {
        Option<bool> multiOption = new(name: "--multi", description: "允许重复运行实例。");
        RootCommand rootCommand = [multiOption];
        rootCommand.SetHandler(async (multi) =>
        {
            // 进程互斥
            using Mutex _ = new(true, Assembly.GetExecutingAssembly().GetName().Name, out bool isNotRunning);
            if (!isNotRunning && !multi)
            {
                throw new EntryPointNotFoundException("你只能同时运行一个聊天室实例！");
            }
            await ChatMainAsync();
        }, multiOption);
        return await rootCommand.InvokeAsync(args);
    }

    private static async Task ChatMainAsync()
    {
        // 修复中文输入输出
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.InputEncoding = Encoding.GetEncoding(936);
        Console.OutputEncoding = Encoding.GetEncoding(936);
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger("聊天室");
        Console.Title = "聊天室";
        Socket client;
        string ip;
        while (true)
        {
            client = new(SocketType.Stream, ProtocolType.Tcp);
            Console.Write("请输入服务器地址：");
            ip = Console.ReadLine();
            try
            {
                await client.ConnectAsync(ip, 19132);
            }
            catch (SocketException ex)
            {
                logger.LogError("连接失败：{Message}", ex.Message);
                continue;
            }
            break;
        }
        Console.Title = $"聊天室：{ip}";
        Dictionary<int, string> lastMessage = [];
        int lastOne = 0;
        Console.Clear();
        ThreadPool.QueueUserWorkItem(async (_) =>
        {
            while (true)
            {
                int uuid;
                long ticks;
                string message, userName;
                try
                {
                    NetworkStream stream = new(client);
                    if (!stream.CanRead)
                    {
                        continue;
                    }
                    BinaryReader reader = new(stream);
                    uuid = reader.ReadInt32();
                    ticks = reader.ReadInt64();
                    message = reader.ReadString();
                    userName = reader.ReadString();
                }
                catch (IOException ex)
                {
                    logger.LogError("连接中断，恢复中：{Message}", ex.Message);
                    while (!client.Connected)
                    {
                        client = new(SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            await client.ConnectAsync(ip, 19132);
                        }
                        catch (SocketException)
                        {
                        }
                    }
                    logger.LogInformation("连接已恢复");
                    continue;
                }
                if (lastMessage.TryGetValue(uuid, out string value) && value == message)
                {
                    continue;
                }
                ConsoleColor temp = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                if (uuid != lastOne)
                {
                    Console.Write($"{userName}（{uuid}） ");
                }
                Console.WriteLine(new DateTime(ticks));
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(message);
                Console.ForegroundColor = temp;
                lastMessage[uuid] = message;
                lastOne = uuid;
            }
        });
        logger.LogInformation("连接已建立");
        while (true)
        {
            string line = Console.ReadLine();
            if (string.IsNullOrEmpty(line) || !client.Connected)
            {
                continue;
            }
            if (line.StartsWith('/'))
            {
                try
                {
                    CommandHelper.Process(line[1..].Split(' '), CommandHelper.Commands);
                }
                catch (ArgumentException ex)
                {
                    logger.LogError("命令运行失败：{Message}", ex.Message);
                }
                continue;
            }
            NetworkStream stream = new(client);
            if (!stream.CanWrite)
            {
                continue;
            }
            BinaryWriter writer = new(stream);
            writer.Write(line);
            writer.Write(UserName ?? string.Empty);
        }
    }
}
