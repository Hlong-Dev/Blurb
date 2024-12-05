namespace DoAnCoSo2.Models
{
    public class ChatMessage
    {
        public string Id { get; set; }
        public string SenderUserId { get; set; }
        public string ReceiverUserId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
