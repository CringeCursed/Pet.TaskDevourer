using Pet.TaskDevourer.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
                MessageBox.Show("Task name cannot be empty", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TaskItem.Title = TitleTextBox.Text.Trim();
            TaskItem.Description = DescriptionTextBox.Text.Trim();
            TaskItem.DueDate = DueDatePicker.SelectedDate ?? DateTime.Now;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}