namespace ChatRoom.Packet
{
    public struct Base<T>
    {
        public ActionType Action { get; set; }
        public T Param { get; set; }
    }
    public enum ActionType
    {
        Unknown,
        Message,
        SetUserName
    }
}