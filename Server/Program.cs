using System.Net;
using System.Net.Sockets;

List<Socket> clients = [];
using Socket listenner = new(SocketType.Stream, ProtocolType.Tcp);
listenner.Bind(new IPEndPoint(IPAddress.Any, 19132));
listenner.Listen();
while (true)
{
    Socket client = await listenner.AcceptAsync();
    clients.Add(client);
    ThreadPool.QueueUserWorkItem((_) =>
    {
        while (true)
        {
            string message, userName;
            try
            {
                NetworkStream stream = new(client);
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
            foreach (Socket otherClient in clients)
            {
                try
                {
                    NetworkStream stream = new(otherClient);
                    if (!stream.CanWrite)
                    {
                        continue;
                    }
                    BinaryWriter writer = new(stream);
                    writer.Write(client.RemoteEndPoint.ToString()[..client.RemoteEndPoint.ToString().LastIndexOf(':')].GetHashCode());
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
