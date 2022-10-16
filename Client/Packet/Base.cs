namespace ChatRoom.Packet
{
    public struct Base<T>
    {
        public Action Action { get; set; }
        public T Param { get; set; }
    }
    public enum Action
    {
        Unknown,
        Login,
        Message,
        ChangeName
    }
}