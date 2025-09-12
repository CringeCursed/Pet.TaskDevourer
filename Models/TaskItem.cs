using System.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Pet.TaskDevourer.Models
{
    [Serializable]
    public class TaskItem : INotifyPropertyChanged
    {
    private int _id;
        private string _title = null!;
        private string _description = null!;
        private DateTime _dueDate = default!;
        private bool _isCompleted;
        private ObservableCollection<string> _tags = new();
        private ObservableCollection<SubTaskItem> _subTasks = new();
        private ObservableCollection<AttachmentItem> _attachments = new();
        private int _subTasksTotalCount;
        private int _subTasksCompletedCount;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public DateTime DueDate
        {
            get => _dueDate;
            set
            {
                _dueDate = value;
                OnPropertyChanged(nameof(DueDate));
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }

        public ObservableCollection<string> Tags
        {
            get => _tags;
            set { _tags = value; OnPropertyChanged(nameof(Tags)); }
        }

        public ObservableCollection<SubTaskItem> SubTasks
        {
            get => _subTasks;
            set
            {
                if (_subTasks != null)
                {
                    UnhookSubTasks(_subTasks);
                }
                _subTasks = value ?? new ObservableCollection<SubTaskItem>();
                HookSubTasks(_subTasks);
                OnPropertyChanged(nameof(SubTasks));
            }
        }

        public ObservableCollection<AttachmentItem> Attachments
        {
            get => _attachments;
            set { _attachments = value; OnPropertyChanged(nameof(Attachments)); }
        }

        public int SubTasksTotalCount
        {
            get => _subTasksTotalCount;
            private set { _subTasksTotalCount = value; OnPropertyChanged(nameof(SubTasksTotalCount)); OnPropertyChanged(nameof(SubTasksProgress)); }
        }

        public int SubTasksCompletedCount
        {
            get => _subTasksCompletedCount;
            private set { _subTasksCompletedCount = value; OnPropertyChanged(nameof(SubTasksCompletedCount)); OnPropertyChanged(nameof(SubTasksProgress)); }
        }

        public string SubTasksProgress => $"{SubTasksCompletedCount}/{SubTasksTotalCount}";

        public TaskItem(string title, string description, DateTime dueDate)
        {
            Title = title;
            Description = description;
            DueDate = dueDate;
            IsCompleted = false;
            HookSubTasks(_subTasks);
        }

        public TaskItem()
        {
            HookSubTasks(_subTasks);
        }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void HookSubTasks(ObservableCollection<SubTaskItem> collection)
        {
            if (collection == null) return;
            collection.CollectionChanged += SubTasks_CollectionChanged;
            foreach (var st in collection)
            {
                st.PropertyChanged += SubTask_PropertyChanged;
            }
            RecalculateSubTaskCounters();
        }

        private void UnhookSubTasks(ObservableCollection<SubTaskItem> collection)
        {
            if (collection == null) return;
            collection.CollectionChanged -= SubTasks_CollectionChanged;
            foreach (var st in collection)
            {
                st.PropertyChanged -= SubTask_PropertyChanged;
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
                foreach (var st in _subTasks)
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
            SubTasksTotalCount = _subTasks?.Count ?? 0;
            SubTasksCompletedCount = _subTasks?.Count(st => st.IsCompleted) ?? 0;
        }
    }
}