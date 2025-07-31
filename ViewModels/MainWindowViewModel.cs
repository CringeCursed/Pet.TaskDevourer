using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;


using Pet.TaskDevourer.Models;
using Pet.TaskDevourer.Helpers;
using System.Windows.Controls;
using Pet.TaskDevourer.Views;

namespace Pet.TaskDevourer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new();

        private string _newTitle = "";
        public string NewTitle
        {
            get => _newTitle;
            set { _newTitle = value; OnPropertyChanged(); }
        }

        private string _newDescription = "";
        public string NewDescription
        {
            get => _newDescription;
            set { _newDescription = value; OnPropertyChanged(); }
        }

        private DateTime? _newDueDate = DateTime.Today;
        public DateTime? NewDueDate
        {
            get => _newDueDate;
            set { _newDueDate = value; OnPropertyChanged(); }
        }

        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand EditTaskCommand { get; }

        public MainWindowViewModel()
        {
            AddTaskCommand = new RelayCommand(_ => AddTask(), _ => !string.IsNullOrWhiteSpace(NewTitle));
            DeleteTaskCommand = new RelayCommand(task => Tasks.Remove((TaskItem)task));
            EditTaskCommand = new RelayCommand(task => EditTask((TaskItem)task));
        }

        private void AddTask()
        {
            Tasks.Add(new TaskItem(NewTitle, NewDescription, NewDueDate ?? DateTime.Today));
            NewTitle = "";
            NewDescription = "";
            NewDueDate = DateTime.Today;
        }

        private void EditTask(TaskItem task)
        {
            var editableTask = new TaskItem(task.Title, task.Description, task.DueDate);
            var window = new EditTaskWindow(editableTask);
            if (window.ShowDialog() == true)
            {

                task.Title = editableTask.Title;
                task.Description = editableTask.Description;
                task.DueDate = editableTask.DueDate;

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
