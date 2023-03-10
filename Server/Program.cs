using ChatRoom.Packet;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

Dictionary<TcpClient, int> clients = new();
int argCount = 0, maxCount = 0;
System.Timers.Timer timer = new(1000);
timer.Elapsed += (_, _) =>
{
    Console.Title = $"聊天室服务器　　{argCount}/{maxCount}";
    argCount = 0;
};
timer.Start();
TcpListener listenner = new(IPAddress.Any, 19132);
listenner.Start();
Console.WriteLine($"开始监听{listenner.LocalEndpoint}");
while (true)
{
    TcpClient client = listenner.AcceptTcpClient();
    string clientIP = client.Client.RemoteEndPoint.ToString()[..client.Client.RemoteEndPoint.ToString().LastIndexOf(':')];
    clients.Add(client, clientIP.GetHashCode());
    ThreadPool.QueueUserWorkItem((_) =>
    {
        while (true)
        {
            byte[] bytes = new byte[ushort.MaxValue];
            try
            {
                NetworkStream stream = client.GetStream();
                if (!stream.CanRead)
                {
                    continue;
                }
                stream.Read(bytes, 0, bytes.Length);
            } catch (IOException ex)
            {
                Console.WriteLine($"{client.Client.RemoteEndPoint}已断开连接：{ex}");
                clients.Remove(client);
                return;
            }
            string receivedString = Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);
            if (string.IsNullOrEmpty(receivedString))
            {
                continue;
            }
            Request data = JsonSerializer.Deserialize<Request>(receivedString);
            byte[] packetBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Response()
            {
                UUID = clients[client],
                DateTime = DateTime.Now,
                Message = data.Message,
                UserName = data.UserName
            }));
            foreach (TcpClient otherClient in clients.Keys)
            {
                try
                {
                    NetworkStream stream = otherClient.GetStream();
                    if (!stream.CanWrite)
                    {
                        continue;
                    }
                    stream.Write(packetBytes, 0, packetBytes.Length);
                } catch (IOException ex)
                {
                    Console.WriteLine($"{client.Client.RemoteEndPoint}已断开连接：{ex}");
                    clients.Remove(client);
                    continue;
                }
            }
            argCount++;
            if (argCount > maxCount)
            {
                maxCount = argCount;
            }
        }
    });
    Console.WriteLine($"{client.Client.RemoteEndPoint}已连接");
}
