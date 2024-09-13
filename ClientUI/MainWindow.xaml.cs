using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;

namespace ChatRoom;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private const ushort ListenPort = 0x63DD;
    private readonly ConcurrentDictionary<int, string> _lastMessage = [];
    private readonly UdpClient _listener;
    private readonly ConcurrentQueue<(int, DateTime, string, string)> _messageQueue = [];
    private readonly AutoResetEvent _resetEvent = new(false);
    private int _lastSender;

    public MainWindow()
    {
        try
        {
            _listener = new(ListenPort);
        }
        catch (SocketException e)
        {
            MessageBox.Show(e.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }

        InitializeComponent();
        StartListener();
        StartMessageHandler();
    }

    private void StartListener()
    {
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                ListenAsync().Wait();
            }
        }, TaskCreationOptions.LongRunning);
    }

    private async Task ListenAsync()
    {
        try
        {
            UdpReceiveResult result = await _listener.ReceiveAsync();
            DateTime dateTime = DateTime.Now;
            int uid = result.RemoteEndPoint.Address.GetHashCode();
            using (MemoryStream stream = new(result.Buffer))
            {
                using (BinaryReader reader = new(stream))
                {
                    string message = reader.ReadString();
                    string userName = reader.ReadString();
                    _messageQueue.Enqueue((uid, dateTime, message, userName));
                }
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => ChatBox.AppendText($"{Environment.NewLine}{ex}"));
        }
    }

    private void StartMessageHandler()
    {
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                while (_messageQueue.TryDequeue(
                           out (int Uid, DateTime DateTime, string Message, string UserName) message))
                {
                    HandleMessageAsync(message.Uid, message.DateTime, message.Message, message.UserName).Wait();
                }

                _resetEvent.WaitOne();
            }
        }, TaskCreationOptions.LongRunning);
    }

    private async Task HandleMessageAsync(int uid, DateTime dateTime, string message, string userName)
    {
        if (_lastMessage.TryGetValue(uid, out string? value) && value == message)
        {
            return;
        }

        await Dispatcher.InvokeAsync(() =>
        {
            bool needScroll = ChatBox.HorizontalOffset >= ChatBox.ViewportHeight;
            if (!string.IsNullOrEmpty(ChatBox.Text))
            {
                ChatBox.AppendText(Environment.NewLine);
            }

            if (uid != _lastSender)
            {
                if (!string.IsNullOrEmpty(ChatBox.Text))
                {
                    ChatBox.AppendText(Environment.NewLine);
                }

                ChatBox.AppendText($"{userName}（{uid}） ");
            }

            ChatBox.AppendText($"{dateTime}{Environment.NewLine}{message}");
            if (needScroll)
            {
                ChatBox.ScrollToEnd();
            }
        });
        _lastMessage[uid] = message;
        _lastSender = uid;
    }

    private async Task SendAsync(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(InputBox.Text))
        {
            return;
        }

        IPEndPoint ep = new(IPAddress.None, ListenPort);
        using (MemoryStream stream = new())
        {
            await using (BinaryWriter writer = new(stream))
            {
                writer.Write(InputBox.Text);
                writer.Write(NameBox.Text);
            }

            using (UdpClient sender = new(default))
            {
                var buffer = stream.GetBuffer();
                await sender.SendAsync(buffer, ep);
            }
        }

        await HandleMessageAsync(default, dateTime, InputBox.Text, NameBox.Text);
        InputBox.Clear();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendAsync(DateTime.Now);
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter)
        {
            return;
        }

        await SendAsync(DateTime.Now);
    }
}
