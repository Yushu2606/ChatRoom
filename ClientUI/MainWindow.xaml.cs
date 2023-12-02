using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace ChatRoom;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private static Socket Client { get; set; }

    public MainWindow()
    {
        using Mutex _ = new(true, Assembly.GetExecutingAssembly().GetName().Name, out bool isNotRunning);
        if (!isNotRunning)
        {
            MessageBox.Show("你只能同时运行一个聊天室实例！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw new EntryPointNotFoundException("你只能同时运行一个聊天室实例！");
        }
        InitializeComponent();
    }

    private async void ConnectAsync(object sender, RoutedEventArgs e)
    {
        string ip = IPBox.Text;
        LoginGrid.IsEnabled = false;
        Client = new(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await Client.ConnectAsync(IPAddress.TryParse(ip, out IPAddress address) ? address : IPAddress.Loopback, 19132);
        }
        catch (SocketException ex)
        {
            MessageBox.Show($"连接失败：{ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoginGrid.IsEnabled = true;
            return;
        }
        LoginGrid.Visibility = Visibility.Hidden;
        RoomGrid.IsEnabled = true;
        RoomGrid.Visibility = Visibility.Visible;
        Dictionary<int, string> lastMessage = [];
        int lastOne = 0;
        ThreadPool.QueueUserWorkItem(async (_) =>
        {
            while (true)
            {
                int uuid;
                long ticks;
                string message, userName;
                try
                {
                    NetworkStream stream = new(Client);
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
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ChatBox.Text += $"{Environment.NewLine}已断开连接，重连中：{ex.Message}";
                        SendButton.IsEnabled = false;
                    });
                    while (!Client.Connected)
                    {
                        Client = new(SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            await Client.ConnectAsync(IPAddress.TryParse(ip, out IPAddress address) ? address : IPAddress.Loopback, 19132);
                            break;
                        }
                        catch (SocketException)
                        {
                        }
                    }
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ChatBox.Text += $"{Environment.NewLine}已重连";
                        SendButton.IsEnabled = true;
                    });
                    continue;
                }
                if (lastMessage.TryGetValue(uuid, out string value) && value == message)
                {
                    continue;
                }
                await Dispatcher.InvokeAsync(() =>
                {
                    bool needScroll = false;
                    if (ChatBox.HorizontalOffset >= ChatBox.ViewportHeight)
                    {
                        needScroll = true;
                    }
                    if (!string.IsNullOrEmpty(ChatBox.Text))
                    {
                        ChatBox.Text += Environment.NewLine;
                    }
                    if (uuid != lastOne)
                    {
                        if (!string.IsNullOrEmpty(ChatBox.Text))
                        {
                            ChatBox.Text += Environment.NewLine;
                        }
                        ChatBox.Text += $"{userName}（{uuid}） ";
                    }
                    ChatBox.Text += $"{new DateTime(ticks)}{Environment.NewLine}{message}";
                    if (needScroll)
                    {
                        ChatBox.ScrollToEnd();
                    }
                });
                lastMessage[uuid] = message;
                lastOne = uuid;
            }
        });
    }

    private void Send(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(InputBox.Text))
        {
            return;
        }
        NetworkStream stream = new(Client);
        if (!stream.CanWrite)
        {
            return;
        }
        BinaryWriter writer = new(stream);
        writer.Write(InputBox.Text);
        writer.Write(NameBox.Text);
        InputBox.Text = string.Empty;
    }

    private void EnterButtonDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter && SendButton.IsEnabled)
        {
            Send(default, default);
        }
    }
}
