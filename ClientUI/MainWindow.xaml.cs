using System.IO;
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
    private static TcpClient TcpClient { get; set; }

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

    private void Connect(object sender, RoutedEventArgs e)
    {
        string ip = IPBox.Text;
        LoginGrid.IsEnabled = false;
        TcpClient = new();
        try
        {
            TcpClient.Connect(ip, 19132);
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
        ThreadPool.QueueUserWorkItem((_) =>
        {
            while (true)
            {
                int uuid;
                long ticks;
                string message, userName;
                try
                {
                    NetworkStream stream = TcpClient.GetStream();
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
                    Dispatcher.Invoke(() =>
                    {
                        ChatBox.Text += $"{Environment.NewLine}已断开连接：{ex.Message}";
                        SendButton.IsEnabled = false;
                    });
                    int count = 0;
                    while (!TcpClient.Connected)
                    {
                        TcpClient = new();
                        Dispatcher.Invoke(() =>
                        {
                            ip = IPBox.Text;
                            ChatBox.Text += $"{Environment.NewLine}重连中：{++count}";
                            ChatBox.ScrollToEnd();
                        });
                        try
                        {
                            TcpClient.Connect(ip, 19132);
                            break;
                        }
                        catch (SocketException ex1)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ChatBox.Text += $"{Environment.NewLine}重连失败：{ex1.Message}";
                                ChatBox.ScrollToEnd();
                            });
                        }
                        Thread.Sleep(1000);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        ChatBox.Text += $"{Environment.NewLine}已重连";
                        ChatBox.ScrollToEnd();
                        SendButton.IsEnabled = true;
                    });
                    continue;
                }
                if (lastMessage.TryGetValue(uuid, out string value) && value == message)
                {
                    continue;
                }
                Dispatcher.Invoke(() =>
                {
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
                    ChatBox.ScrollToEnd();
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
        NetworkStream stream = TcpClient.GetStream();
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

    private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            ChatBox.Width = e.NewSize.Width - 36 < 0 ? 0 : e.NewSize.Width - 36;
            InputBox.Width = e.NewSize.Width - 141 < 0 ? 0 : e.NewSize.Width - 141;
        }
        if (e.HeightChanged)
        {
            ChatBox.Height = e.NewSize.Height - 140 < 0 ? 0 : e.NewSize.Height - 140;
        }
    }
}
