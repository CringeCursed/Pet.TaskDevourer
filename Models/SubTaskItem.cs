using System.ComponentModel;

namespace Pet.TaskDevourer.Models
{
    public class SubTaskItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private bool _isCompleted;

        public string Title
        {
            get => _title;
            set { _title = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title))); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
