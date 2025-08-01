using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Pet.TaskDevourer.Models;
using Pet.TaskDevourer.Helpers;
using Pet.TaskDevourer.Views;

namespace Pet.TaskDevourer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ICollectionView FilteredTasks { get; private set; }
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

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredTasks.Refresh();
            }
        }
        public enum TaskFilter
        {
            All,
            Active,
            Completed
        }
        private TaskFilter _selectedFilter = TaskFilter.All;
        public TaskFilter SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged();
                FilteredTasks.Refresh(); 
            }
        }

        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteCompletedTasksCommand { get; }

        public MainWindowViewModel()
        {
            AddTaskCommand = new RelayCommand(_ => AddTask(), _ => !string.IsNullOrWhiteSpace(NewTitle));
            DeleteTaskCommand = new RelayCommand(task => Tasks.Remove((TaskItem)task));
            EditTaskCommand = new RelayCommand(task => EditTask((TaskItem)task));
            DeleteCompletedTasksCommand = new RelayCommand(_ => DeleteCompletedTasks(), _ => Tasks.Any(t => t.IsCompleted));

            FilteredTasks = CollectionViewSource.GetDefaultView(Tasks);
            FilteredTasks.Filter = taskObj =>
            {
                if (taskObj is TaskItem task)
                {
                    bool matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                        || task.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                        || task.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

                    bool matchesFilter = SelectedFilter switch
                    {
                        TaskFilter.All => true,
                        TaskFilter.Active => !task.IsCompleted,
                        TaskFilter.Completed => task.IsCompleted,
                        _ => true
                    };

                    return matchesSearch && matchesFilter;
                }
                return false;
            };
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

        private void DeleteCompletedTasks()
        {
            var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();
            foreach (var task in completedTasks)
            {
                Tasks.Remove(task);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
