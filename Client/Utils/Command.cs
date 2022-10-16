using ChatRoom.Packet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ChatRoom.Utils
{
    internal class Command
    {
        internal static Dictionary<string, CommandData> Commands { get; set; } = new Dictionary<string, CommandData>()
        {
            ["HELP"] = new CommandData()
            {
                Description = "列出所有命令",
                Action = (args) =>
                {
                    Dictionary<string, CommandData> commands = Commands;
                    foreach (string arg in args)
                    {
                        if (!commands.ContainsKey(arg))
                        {
                            continue;
                        }
                        commands = commands[arg].SubCommands;
                    }
                    foreach (KeyValuePair<string, CommandData> command in commands)
                    {
                        Console.WriteLine($"{command.Key.ToLower()} - {command.Value.Description}");
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
                            string packet = JsonConvert.SerializeObject(new Base<ChangeName.Request>()
                            {
                                Action = Packet.Action.ChangeName,
                                Param = new ChangeName.Request()
                                {
                                    NewName = args[0]
                                }
                            });
                            byte[] bytes = Console.InputEncoding.GetBytes(packet);
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
        internal static void Processing(int deep, List<string> args, Dictionary<string, CommandData> commands)
        {
            if (!commands.ContainsKey(args[deep - 1]))
            {
                throw new ArgumentException($"未知的命令：{args[deep - 1]}");
            }
            if (commands[args[deep - 1]].Action != null)
            {
                List<string> newArgs = new List<string>(args);
                if (args.Count - deep > 0)
                {
                    newArgs.RemoveRange(0, deep);
                }
                commands[args[deep - 1]].Action(newArgs);
            }
            if (commands[args[deep - 1]].SubCommands != null)
            {
                if (args.Count <= deep)
                {
                    foreach (KeyValuePair<string, CommandData> command in commands[args[deep - 1]].SubCommands)
                    {
                        Console.WriteLine($"{command.Key.ToLower()} - {command.Value.Description}");
                    }
                    return;
                }
                Processing(deep + 1, args, commands[args[deep - 1]].SubCommands);
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