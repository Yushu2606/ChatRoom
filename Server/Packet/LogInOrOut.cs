namespace ChatRoom.Packet
{
    public struct LogInOrOut
    {
        public struct Request
        {
            public string UserName { get; set; }
        }
    }
}