using System.Net;
using System.Net.Sockets;

Dictionary<TcpClient, int> clients = [];
using TcpListener listenner = new(IPAddress.Any, 19132);
listenner.Start();
while (true)
{
    TcpClient client = listenner.AcceptTcpClient();
    string clientIP = client.Client.RemoteEndPoint.ToString()[..client.Client.RemoteEndPoint.ToString().LastIndexOf(':')];
    clients.Add(client, clientIP.GetHashCode());
    ThreadPool.QueueUserWorkItem((_) =>
    {
        while (true)
        {
            string message, userName;
            try
            {
                NetworkStream stream = client.GetStream();
                if (!stream.CanRead)
                {
                    continue;
                }
                BinaryReader reader = new(stream);
                message = reader.ReadString();
                userName = reader.ReadString();
            }
            catch (IOException)
            {
                clients.Remove(client);
                return;
            }
            foreach (TcpClient otherClient in clients.Keys)
            {
                try
                {
                    NetworkStream stream = otherClient.GetStream();
                    if (!stream.CanWrite)
                    {
                        continue;
                    }
                    BinaryWriter writer = new(stream);
                    writer.Write(clients[client]);
                    writer.Write(DateTime.Now.Ticks);
                    writer.Write(message);
                    writer.Write(userName);
                }
                catch (IOException)
                {
                    clients.Remove(client);
                    continue;
                }
            }
        }
    });
}
