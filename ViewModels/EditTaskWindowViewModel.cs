using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pet.TaskDevourer.Models;
using Pet.TaskDevourer.Helpers;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Linq;

namespace Pet.TaskDevourer.ViewModels
{
    public class TaskEditViewModel : INotifyPropertyChanged
    {
        private readonly TaskItem _originalTask;

        private string _title = string.Empty;
        private string _description = string.Empty;
        private DateTime? _dueDate;
        private bool _isConfirmed;
        private string _newTag = string.Empty;

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

        public ObservableCollection<string> Tags { get; }
        public ObservableCollection<SubTaskItem> SubTasks { get; }
        public ObservableCollection<AttachmentItem> Attachments { get; }

        public string NewTag
        {
            get => _newTag;
            set { _newTag = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand AddSubTaskCommand { get; }
        public ICommand RemoveSubTaskCommand { get; }
        public ICommand AddAttachmentCommand { get; }
        public ICommand RemoveAttachmentCommand { get; }
        public ICommand OpenAttachmentCommand { get; }
        public ICommand SaveAttachmentAsCommand { get; }
        public ICommand CloneCommand { get; }

        public Action? CloseAction { get; set; }
        public Action<TaskItem>? CloneAction { get; set; }

        private int _subTasksTotalCount;
        public int SubTasksTotalCount
        {
            get => _subTasksTotalCount;
            private set { _subTasksTotalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubTasksProgressText)); }
        }

        private int _subTasksCompletedCount;
        public int SubTasksCompletedCount
        {
            get => _subTasksCompletedCount;
            private set { _subTasksCompletedCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubTasksProgressText)); }
        }

        public string SubTasksProgressText => $"{SubTasksCompletedCount}/{SubTasksTotalCount}";

        public TaskEditViewModel(TaskItem task)
        {
            _originalTask = task;

            Title = task.Title;
            Description = task.Description;
            DueDate = task.DueDate;

            Tags = new ObservableCollection<string>(task.Tags ?? new ObservableCollection<string>());
            SubTasks = new ObservableCollection<SubTaskItem>(task.SubTasks ?? new ObservableCollection<SubTaskItem>());
            Attachments = new ObservableCollection<AttachmentItem>(task.Attachments ?? new ObservableCollection<AttachmentItem>());

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            AddTagCommand = new RelayCommand(_ => AddTag(), _ => !string.IsNullOrWhiteSpace(NewTag));
            RemoveTagCommand = new RelayCommand(tag =>
            {
                if (tag is string s && Tags.Contains(s)) Tags.Remove(s);
            });
            AddSubTaskCommand = new RelayCommand(_ => SubTasks.Add(new SubTaskItem { Title = "New sub-task" }));
            RemoveSubTaskCommand = new RelayCommand(st => { if (st is SubTaskItem item) SubTasks.Remove(item); });
            AddAttachmentCommand = new RelayCommand(_ => AddAttachment());
            RemoveAttachmentCommand = new RelayCommand(att => { if (att is AttachmentItem a) Attachments.Remove(a); });
            OpenAttachmentCommand = new RelayCommand(att => { if (att is AttachmentItem a) OpenAttachment(a); });
            SaveAttachmentAsCommand = new RelayCommand(att => { if (att is AttachmentItem a) SaveAttachmentAs(a); });
            CloneCommand = new RelayCommand(_ => CloneTask());

            SubTasks.CollectionChanged += SubTasks_CollectionChanged;
            foreach (var st in SubTasks) st.PropertyChanged += SubTask_PropertyChanged;
            RecalculateSubTaskCounters();
        }

        private void Save(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Title))
                return;

            _originalTask.Title = Title;
            _originalTask.Description = Description;
            _originalTask.DueDate = DueDate ?? DateTime.Now;
            _originalTask.Tags = Tags;
            _originalTask.SubTasks = SubTasks;
            _originalTask.Attachments = Attachments;

            IsConfirmed = true;
            CloseAction?.Invoke();
        }

        private void Cancel(object? obj)
        {
            IsConfirmed = false;
            CloseAction?.Invoke();
        }

        private void AddTag()
        {
            var tag = NewTag.Trim();
            if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
            {
                Tags.Add(tag);
                NewTag = string.Empty;
            }
        }

        private void AddAttachment()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select file to attach",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                var fi = new FileInfo(dlg.FileName);
                Attachments.Add(new AttachmentItem
                {
                    FileName = fi.Name,
                    FilePath = fi.FullName,
                    SizeBytes = fi.Length
                });
            }
        }

        private void SubTasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var obj in e.NewItems)
                {
                    if (obj is SubTaskItem st)
                        st.PropertyChanged += SubTask_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (var obj in e.OldItems)
                {
                    if (obj is SubTaskItem st)
                        st.PropertyChanged -= SubTask_PropertyChanged;
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var st in SubTasks)
                    st.PropertyChanged += SubTask_PropertyChanged;
            }
            RecalculateSubTaskCounters();
        }

        private void SubTask_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubTaskItem.IsCompleted))
            {
                RecalculateSubTaskCounters();
            }
        }

        private void RecalculateSubTaskCounters()
        {
            SubTasksTotalCount = SubTasks?.Count ?? 0;
            SubTasksCompletedCount = SubTasks?.Count(st => st.IsCompleted) ?? 0;
        }

        private void OpenAttachment(AttachmentItem attachment)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(attachment.FilePath) && File.Exists(attachment.FilePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = attachment.FilePath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                else
                {
                    System.Windows.MessageBox.Show("File not found. It may be remote; download via server when REST is ready.", "Open Attachment", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Cannot open file: {ex.Message}", "Open Attachment", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void SaveAttachmentAs(AttachmentItem attachment)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    FileName = attachment.FileName
                };
                if (dlg.ShowDialog() == true)
                {
                    if (!string.IsNullOrWhiteSpace(attachment.FilePath) && File.Exists(attachment.FilePath))
                    {
                        File.Copy(attachment.FilePath, dlg.FileName, overwrite: true);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Source file not available locally. Implement download via REST later.", "Save Attachment As", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Cannot save file: {ex.Message}", "Save Attachment As", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CloneTask()
        {
            var cloned = new TaskItem
            {
                Title = string.IsNullOrWhiteSpace(Title) ? "(Copy)" : $"{Title} (Copy)",
                Description = Description,
                DueDate = DueDate ?? DateTime.Now,
                IsCompleted = false,
                Tags = new ObservableCollection<string>(Tags),
                SubTasks = new ObservableCollection<SubTaskItem>(SubTasks.Select(st => new SubTaskItem { Title = st.Title, IsCompleted = st.IsCompleted })),
                Attachments = new ObservableCollection<AttachmentItem>(Attachments.Select(a => new AttachmentItem { FileName = a.FileName, FilePath = a.FilePath, SizeBytes = a.SizeBytes }))
            };

            CloneAction?.Invoke(cloned);
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name!));
    }
}
