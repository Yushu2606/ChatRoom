using System;

namespace ChatRoom.Packet
{
    public struct Message
    {
        public struct Request
        {
            public string UserName { get; set; }
            public string Message { get; set; }
        }
        public struct Response
        {
            public string UserName { get; set; }
            public DateTime DateTime { get; set; }
            public string Message { get; set; }
            public string UUID { get; set; }
        }
    }
}