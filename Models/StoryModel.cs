namespace YessGoFront.Models
{
    public class StoryModel
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";            // круглый превью
        public List<string> Pages { get; set; } = new();   // список картинок внутри сторис (по порядку)
    }
}
