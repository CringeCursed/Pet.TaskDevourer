using Pet.TaskDevourer.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Pet.TaskDevourer.Views
{
    public partial class EditTaskWindow : Window
    {
        public TaskItem TaskItem { get; private set; }
        public bool IsConfirmed { get; private set; }

        public EditTaskWindow(TaskItem taskItem)
        {
            InitializeComponent();
            TaskItem = taskItem;
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            TitleTextBox.Text = TaskItem.Title;
            DescriptionTextBox.Text = TaskItem.Description;
            DueDatePicker.SelectedDate = TaskItem.DueDate;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Название задачи не может быть пустым!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TaskItem.Title = TitleTextBox.Text.Trim();
            TaskItem.Description = DescriptionTextBox.Text.Trim();
            TaskItem.DueDate = DueDatePicker.SelectedDate ?? DateTime.Now;

            DialogResult = true; // ✅ обязательно!
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}