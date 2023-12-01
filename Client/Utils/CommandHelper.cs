namespace ChatRoom.Utils;

internal static class CommandHelper
{
    internal static Dictionary<string, CommandData> Commands { get; } = new()
    {
        ["HELP"] = new()
        {
            Description = "列出所有命令",
            Action = args =>
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
                foreach ((string command, CommandData commandData) in commands)
                {
                    Console.WriteLine($"  {command.ToLower()}\t{commandData.Description}");
                }
            }
        },
        ["NAME"] = new()
        {
            Description = "用户名相关",
            SubCommands = new()
            {
                ["CHANGE"] = new()
                {
                    Description = "修改用户名",
                    Action = args =>
                    {
                        if (args.Count is not 1)
                        {
                            throw new ArgumentException("参数数量错误");
                        }
                        Client.UserName = args[0];
                    }
                }
            }
        }
    };
    internal static void Process(IList<string> args, Dictionary<string, CommandData> commands, int deep = 1)
    {
        string mainCommand = args[deep - 1].ToUpper();
        if (!commands.TryGetValue(mainCommand, out CommandData command))
        {
            throw new ArgumentException($"未知的命令：{args[deep - 1]}");
        }
        if (command.Action is not null)
        {
            List<string> newArgs = new(args);
            if (args.Count - deep > 0)
            {
                newArgs.RemoveRange(0, deep);
            }
            command.Action(newArgs);
        }
        if (command.SubCommands is not null)
        {
            if (args.Count <= deep)
            {
                foreach ((string commandName, CommandData commandData) in command.SubCommands)
                {
                    Console.WriteLine($"  {commandName.ToLower()}\t{commandData.Description}");
                }
                return;
            }
            Process(args, command.SubCommands, deep + 1);
        }
    }
}
internal record struct CommandData
{
    public string Description { get; set; }
    public Action<List<string>> Action { get; set; }
    public Dictionary<string, CommandData> SubCommands { get; set; }
}
