using System.Threading.Tasks;
using System.Windows;
using Pet.TaskDevourer.ViewModels;

namespace Pet.TaskDevourer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (DataContext is null)
            {
                DataContext = new MainWindowViewModel();
            }
            this.Loaded += async (_, __) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            };
        }
    }
}