using ChatRoom.Packet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ChatRoom.Utils
{
    internal static class Command
    {
        internal static Dictionary<string, CommandData> Commands { get; } = new Dictionary<string, CommandData>()
        {
            ["HELP"] = new CommandData()
            {
                Description = "列出所有命令",
                Action = (args) =>
                {
                    Dictionary<string, CommandData> commands = Commands;
                    foreach (string arg in args)
                    {
                        if ((!commands.ContainsKey(arg.ToUpper())) || (commands[arg.ToUpper()].SubCommands is null))
                        {
                            continue;
                        }
                        commands = commands[arg.ToUpper()].SubCommands;
                    }
                    foreach (KeyValuePair<string, CommandData> command in commands)
                    {
                        Console.WriteLine($"  {command.Key.ToLower()}\t{command.Value.Description}");
                    }
                }
            },
            ["NAME"] = new CommandData()
            {
                Description = "用户名相关",
                SubCommands = new Dictionary<string, CommandData>()
                {
                    ["CHANGE"] = new CommandData()
                    {
                        Description = "修改用户名",
                        Action = (args) =>
                        {
                            if (args.Count != 1)
                            {
                                throw new ArgumentException("参数数量错误");
                            }
                            byte[] bytes = Console.InputEncoding.GetBytes(JsonConvert.SerializeObject(new Base<UserName.Request>()
                            {
                                Action = Packet.Action.SetUserName,
                                Param = new UserName.Request()
                                {
                                    UserName = args[0]
                                }
                            }));
                            NetworkStream stream = Client.TcpClient.GetStream();
                            if (!stream.CanWrite)
                            {
                                return;
                            }
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Flush(); // 刷新缓冲区
                        }
                    }
                }
            }
        };
        internal static void Process(string[] args, Dictionary<string, CommandData> commands, int deep = 1)
        {
            string mainCommand = args[deep - 1].ToUpper();
            if (!commands.ContainsKey(mainCommand))
            {
                throw new ArgumentException($"未知的命令：{args[deep - 1]}");
            }
            if (commands[mainCommand].Action != null)
            {
                List<string> newArgs = new List<string>(args);
                if (args.Length - deep > 0)
                {
                    newArgs.RemoveRange(0, deep);
                }
                commands[mainCommand].Action(newArgs);
            }
            if (commands[mainCommand].SubCommands != null)
            {
                if (args.Length <= deep)
                {
                    foreach (KeyValuePair<string, CommandData> command in commands[mainCommand].SubCommands)
                    {
                        Console.WriteLine($"  {command.Key.ToLower()}\t{command.Value.Description}");
                    }
                    return;
                }
                Process(args, commands[mainCommand].SubCommands, deep + 1);
            }
        }
    }
    internal struct CommandData
    {
        public string Description { get; set; }
        public Action<List<string>> Action { get; set; }
        public Dictionary<string, CommandData> SubCommands { get; set; }
    }
}