using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pet.TaskDevourer.Models;
using Pet.TaskDevourer.Helpers;

namespace Pet.TaskDevourer.ViewModels
{
    public class TaskEditViewModel : INotifyPropertyChanged
    {
        private readonly TaskItem _originalTask;

        private string _title;
        private string _description;
        private DateTime? _dueDate;
        private bool _isConfirmed;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(); }
        }

        public bool IsConfirmed
        {
            get => _isConfirmed;
            private set { _isConfirmed = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public Action CloseAction { get; set; }

        public TaskEditViewModel(TaskItem task)
        {
            _originalTask = task;

            Title = task.Title;
            Description = task.Description;
            DueDate = task.DueDate;

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Save(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Title))
                return;

            _originalTask.Title = Title;
            _originalTask.Description = Description;
            _originalTask.DueDate = DueDate ?? DateTime.Now;

            IsConfirmed = true;
            CloseAction?.Invoke();
        }

        private void Cancel(object? obj)
        {
            IsConfirmed = false;
            CloseAction?.Invoke();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
