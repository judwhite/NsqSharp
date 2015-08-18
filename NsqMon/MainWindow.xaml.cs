using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Mvvm;
using NsqMon.Views.CDTag.Views;

namespace NsqMon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowViewBase
    {
        private readonly IMainWindowViewModel _viewModel;

        public MainWindow(IMainWindowViewModel viewModel)
            : base(viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            HandleEscape = false;
        }
    }

    public interface IMainWindowViewModel : IViewModelBase
    {
    }

    public class MainWindowViewModel : ViewModelBase<MainWindowViewModel>, IMainWindowViewModel
    {
        public MainWindowViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
        }
    }
}
