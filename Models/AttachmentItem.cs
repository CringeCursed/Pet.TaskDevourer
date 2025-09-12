using System;
using System.ComponentModel;

namespace Pet.TaskDevourer.Models
{
    public class AttachmentItem : INotifyPropertyChanged
    {
        private string _fileName = string.Empty;
        private string _filePath = string.Empty;
        private long _sizeBytes;

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileName))); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath))); }
        }

        public long SizeBytes
        {
            get => _sizeBytes;
            set { _sizeBytes = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SizeBytes))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
