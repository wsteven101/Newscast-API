namespace NewscastApi.Models
{
    public class Story
    {
        public string? By { get; set; }
        public int Descendants { get; set; }
        public long Id { get; set; }
        public long[]? Kids { get; set; }
        public int Score { get; set; }
        public string? Text { get; set; }
        public long Time { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
    }
}
