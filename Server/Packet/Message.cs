namespace ChatRoom.Packet;

public record struct Request
{
    public string UserName { get; set; }
    public string Message { get; set; }
}
public record struct Response
{
    public int UUID { get; set; }
    public DateTime DateTime { get; set; }
    public string UserName { get; set; }
    public string Message { get; set; }
}