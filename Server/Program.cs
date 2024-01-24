using System.Net;
using System.Net.Sockets;

List<Socket> clients = [];
using Socket listener = new(SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Any, 19132));
listener.Listen();
while (true)
{
    Socket client = await listener.AcceptAsync();
    clients.Add(client);
    await Task.Factory.StartNew(_ =>
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
                    if (client.RemoteEndPoint is not null)
                    {
                        string? ip = client.RemoteEndPoint.ToString();
                        if (!string.IsNullOrWhiteSpace(ip))
                        {
                            writer.Write(ip[..ip.LastIndexOf(':')].GetHashCode());
                        }
                        else
                        {
                            writer.Write(Convert.ToInt32(client.Handle).ToString());
                        }
                    }
                    else
                    {
                        writer.Write(Convert.ToInt32(client.Handle).ToString());
                    }

                    writer.Write(DateTime.Now.Ticks);
                    writer.Write(message);
                    writer.Write(userName);
                }
                catch (IOException)
                {
                    clients.Remove(client);
                }
            }
        }
    }, TaskContinuationOptions.LongRunning);
}