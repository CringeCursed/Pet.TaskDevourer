using Pet.TaskDevourer.ViewModels;
using Pet.TaskDevourer;
using System.ComponentModel;
using System.Windows;
namespace Pet.TaskDevourer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}