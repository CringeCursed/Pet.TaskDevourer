using Pet.TaskDevourer.Helpers;
using Pet.TaskDevourer.Models;
using Pet.TaskDevourer.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;

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
            set { _newTitle = value; OnPropertyChanged(); RaiseCommandCanExecutes(); }
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

        private string _searchText = string.Empty;
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
        private const string AllTagsOption = "All";
        public ObservableCollection<string> AllTags { get; } = new();
        private string _selectedTag = AllTagsOption;
        public string SelectedTag
        {
            get => _selectedTag;
            set { _selectedTag = value; OnPropertyChanged(); FilteredTasks.Refresh(); }
        }

        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteCompletedCommand { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _busyMessage = string.Empty;
        public string BusyMessage
        {
            get => _busyMessage;
            private set { _busyMessage = value; OnPropertyChanged(); }
        }

        private ITaskService? _service;

        public bool IsLoading { get; private set; } = true;

        public MainWindowViewModel()
        {
            AddTaskCommand = new AsyncRelayCommand(AddTaskAsync, () => !string.IsNullOrWhiteSpace(NewTitle));
            EditTaskCommand = new AsyncRelayCommand<TaskItem>(EditTaskAsync, t => t != null);
            DeleteTaskCommand = new AsyncRelayCommand<TaskItem>(DeleteTaskAsync, t => t != null);
            DeleteCompletedCommand = new AsyncRelayCommand(DeleteCompletedTasksAsync, () => Tasks.Any(t => t.IsCompleted));

            Tasks.CollectionChanged += Tasks_CollectionChanged;
            FilteredTasks = CollectionViewSource.GetDefaultView(Tasks);
            FilteredTasks.Filter = taskObj =>
            {
                if (taskObj is TaskItem task)
                {
                    bool matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                        || task.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                        || task.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                    bool matchesTag = SelectedTag == AllTagsOption || (task.Tags?.Contains(SelectedTag) ?? false);
                    return matchesSearch && matchesTag;
                }
                return false;
            };
        }

        public async Task InitializeAsync()
        {
            Helpers.DiagnosticsLogger.Log("VM: InitializeAsync start");
            try
            {
                _service = new ApiTaskService("http://localhost:5005/");
                var svc = _service; // local non-null
                var loadedTasks = await svc.LoadAllAsync();
                foreach (var task in loadedTasks)
                {
                    Tasks.Add(task);
                    AttachTaskHandlers(task);
                }
                Helpers.DiagnosticsLogger.Log($"VM: API load ok, tasks={Tasks.Count}");
            }
            catch (Exception exApi)
            {
                Helpers.DiagnosticsLogger.Log("VM: API load failed: " + exApi.Message);
                try
                {
                    var loadedTasks = JsonStorage.Load();
                    foreach (var task in loadedTasks)
                    {
                        Tasks.Add(task);
                        AttachTaskHandlers(task);
                    }
                    Helpers.DiagnosticsLogger.Log($"VM: local JSON load ok, tasks={Tasks.Count}");
                }
                catch (Exception exJson)
                {
                    Helpers.DiagnosticsLogger.Log("VM: local JSON load failed: " + exJson.Message);
                    System.Windows.MessageBox.Show("Failed to load tasks (API + local). See startup.log", "Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            finally
            {
                UpdateAllTags();
                IsLoading = false;
                OnPropertyChanged(nameof(IsLoading));
                Helpers.DiagnosticsLogger.Log("VM: InitializeAsync end");
            }
        }

        private async Task AddTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTitle)) return;
            var newTask = new TaskItem(NewTitle, NewDescription, NewDueDate ?? DateTime.Today);
            bool createdOnServer = false;
            if (_service is ApiTaskService api)
            {
                try
                {
                    SetBusy("Creating task on server...");
                    var created = await api.CreateTaskAsync(newTask);
                    if (created != null) { newTask = created; createdOnServer = true; }
                    Helpers.DiagnosticsLogger.Log($"VM: CreateTask {(createdOnServer ? "server-ok" : "server-null")} title={newTask.Title}");
                }
                catch (Exception ex)
                {
                    Helpers.DiagnosticsLogger.Log("VM: CreateTask server failed: " + ex.Message);
                }
                finally { ClearBusy(); }
            }
            Tasks.Add(newTask);
            AttachTaskHandlers(newTask);
            if (!createdOnServer) await SaveTasksAsync();
            UpdateAllTags();
            FilteredTasks.Refresh();
            NewTitle = string.Empty;
            NewDescription = string.Empty;
            NewDueDate = DateTime.Today;
        }

        public async Task EditTaskAsync(TaskItem? task)
        {
            if (task == null) return;
            var editableTask = new TaskItem(task.Title, task.Description, task.DueDate)
            {
                Tags = new System.Collections.ObjectModel.ObservableCollection<string>(task.Tags),
                SubTasks = new System.Collections.ObjectModel.ObservableCollection<Models.SubTaskItem>(task.SubTasks.Select(st => new Models.SubTaskItem { Title = st.Title, IsCompleted = st.IsCompleted })),
                Attachments = new System.Collections.ObjectModel.ObservableCollection<Models.AttachmentItem>(task.Attachments.Select(a => new Models.AttachmentItem { FileName = a.FileName, FilePath = a.FilePath, SizeBytes = a.SizeBytes }))
            };

            var viewModel = new TaskEditViewModel(editableTask);

            var window = new TaskEditWindow
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            viewModel.CloseAction = () => window.Close();
            viewModel.CloneAction = async cloned =>
            {
                bool createdOnServer = false;
                if (_service is ApiTaskService api)
                {
                    try
                    {
                        SetBusy("Cloning task on server...");
                        var created = await api.CreateTaskAsync(cloned);
                        if (created != null) { cloned = created; createdOnServer = true; }
                        Helpers.DiagnosticsLogger.Log("VM: Clone create server result=" + createdOnServer);
                    }
                    catch (Exception ex)
                    {
                        Helpers.DiagnosticsLogger.Log("VM: Clone server failed: " + ex.Message);
                    }
                    finally { ClearBusy(); }
                }
                Tasks.Add(cloned);
                if (!createdOnServer) await SaveTasksAsync();
                FilteredTasks.Refresh();
            };

            window.ShowDialog();

            if (viewModel.IsConfirmed)
            {
                task.Title = editableTask.Title;
                task.Description = editableTask.Description;
                task.DueDate = editableTask.DueDate;
                task.Tags = editableTask.Tags;
                task.SubTasks = editableTask.SubTasks;
                task.Attachments = editableTask.Attachments;

                await PersistAsync(task);

                UpdateAllTags();
                FilteredTasks.Refresh();
            }
        }

        public async Task DeleteCompletedTasksAsync()
        {
            var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();

            var result = MessageBox.Show("Are you sure, you want to delete all completed tasks?",
                    "Delete confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var task in completedTasks.ToList())
                {
                    await DeleteTaskAsync(task);
                }
            }
        }
        public async Task DeleteTaskAsync(TaskItem? task)
        {
            if (task == null) return;
            bool deletedOnServer = false;
            if (_service is ApiTaskService api && task.Id > 0)
            {
                try { SetBusy("Deleting task..."); await api.DeleteTaskAsync(task.Id); deletedOnServer = true; }
                catch (Exception ex) { Helpers.DiagnosticsLogger.Log("VM: Delete server failed: " + ex.Message); }
                finally { ClearBusy(); }
            }
            Tasks.Remove(task);
            if (!deletedOnServer) await SaveTasksAsync();
            UpdateAllTags();
            if (string.IsNullOrWhiteSpace(SelectedTag))
            {
                SelectedTag = AllTagsOption;
            }
        }

        private async Task SaveTasksAsync()
        {
            if (_service != null)
            {
                try { SetBusy("Saving tasks locally..."); await _service.SaveAllAsync(Tasks.ToList()); }
                catch (Exception ex)
                {
                    Helpers.DiagnosticsLogger.Log("VM: SaveAll to API failed: " + ex.Message + " -> local JSON");
                    JsonStorage.Save(Tasks.ToList());
                }
                finally { ClearBusy(); }
            }
            else
            {
                JsonStorage.Save(Tasks.ToList());
            }
        }

        private void Tasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var obj in e.NewItems)
                {
                    if (obj is TaskItem t) AttachTaskHandlers(t);
                }
            }
            if (e.OldItems != null)
            {
                foreach (var obj in e.OldItems)
                {
                    if (obj is TaskItem t) DetachTaskHandlers(t);
                }
            }
            UpdateAllTags();
            FilteredTasks.Refresh();
        }

        private async Task PersistAsync(TaskItem task)
        {
            if (_service is ApiTaskService api && task.Id > 0)
            {
                try { SetBusy("Updating task..."); await api.UpdateTaskAsync(task); Helpers.DiagnosticsLogger.Log($"VM: UpdateTask server-ok id={task.Id}"); return; }
                catch (Exception ex) { Helpers.DiagnosticsLogger.Log("VM: UpdateTask server failed: " + ex.Message); }
                finally { ClearBusy(); }
            }
            await SaveTasksAsync();
        }

        private void AttachTaskHandlers(TaskItem task)
        {
            task.PropertyChanged += Task_PropertyChanged;
            if (task.Tags != null)
            {
                task.Tags.CollectionChanged += Tags_CollectionChanged;
            }
        }

        private void DetachTaskHandlers(TaskItem task)
        {
            task.PropertyChanged -= Task_PropertyChanged;
            if (task.Tags != null)
            {
                task.Tags.CollectionChanged -= Tags_CollectionChanged;
            }
        }

        private void Tags_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateAllTags();
            // Persist only the owning task if we can find it
            if (sender is System.Collections.ObjectModel.ObservableCollection<string> changed)
            {
                var owner = Tasks.FirstOrDefault(t => ReferenceEquals(t.Tags, changed));
                if (owner != null)
                {
                    _ = PersistAsync(owner);
                }
                else
                {
                    _ = SaveTasksAsync();
                }
            }
            else
            {
                _ = SaveTasksAsync();
            }
            FilteredTasks.Refresh();
        }

        private void Task_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is TaskItem task)
            {
                if (e.PropertyName == nameof(TaskItem.IsCompleted) && task.IsCompleted)
                {
                    if (!task.Tags.Contains("completed"))
                    {
                        task.Tags.Add("completed");
                    }
                }
                else if (e.PropertyName == nameof(TaskItem.Tags))
                {
                    // Tags collection replaced (e.g., after edit window save). Attach handler to new collection.
                    if (task.Tags != null)
                    {
                        task.Tags.CollectionChanged += Tags_CollectionChanged;
                    }
                    UpdateAllTags();
                }
                _ = PersistAsync(task);
                FilteredTasks.Refresh();
            }
        }

        private void UpdateAllTags()
        {
            var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in Tasks)
            {
                if (t.Tags != null)
                {
                    foreach (var tag in t.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag)) current.Add(tag);
                    }
                }
            }
            var selected = SelectedTag;
            AllTags.Clear();
            AllTags.Add(AllTagsOption);
            foreach (var tag in current.OrderBy(s => s))
            {
                AllTags.Add(tag);
            }
            if (!AllTags.Any(t => string.Equals(t, selected, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedTag = AllTagsOption;
            }
            else
            {
                // Normalize selection to the actual item instance to keep ComboBox selection stable
                var normalized = AllTags.First(t => string.Equals(t, selected, StringComparison.OrdinalIgnoreCase));
                if (!ReferenceEquals(normalized, SelectedTag))
                {
                    SelectedTag = normalized;
                }
            }
            OnPropertyChanged(nameof(SelectedTag));
        }

        private void SetBusy(string message)
        {
            BusyMessage = message;
            IsBusy = true;
        }

        private void ClearBusy()
        {
            BusyMessage = string.Empty;
            IsBusy = false;
        }

        private void RaiseCommandCanExecutes()
        {
            (AddTaskCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCompletedCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
