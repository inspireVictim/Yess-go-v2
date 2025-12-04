using System.ComponentModel;

namespace YessGoFront.Models
{
    public class StoryModel : INotifyPropertyChanged
    {
        private bool _isViewed;
        
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";              // иконка превью (круг)
        public List<string> Pages { get; set; } = new();    // картинки внутри сторис (по порядку)
        
        /// <summary>
        /// Показывает, был ли просмотрен этот Story
        /// </summary>
        public bool IsViewed 
        { 
            get => _isViewed;
            set
            {
                if (_isViewed != value)
                {
                    _isViewed = value;
                    OnPropertyChanged(nameof(IsViewed));
                }
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
