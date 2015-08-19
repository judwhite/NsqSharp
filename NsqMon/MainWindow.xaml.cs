using System;
using System.Collections.ObjectModel;
using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Events.Ux;
using NsqMon.Common.Mvvm;
using NsqMon.Plugin.Interfaces;
using NsqMon.Test;
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

    public class MainWindowViewModel : ViewModelBase, IMainWindowViewModel
    {
        public MainWindowViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            var plugin = new NsqMonLocalhostPlugin();
            Clusters = new ObservableCollection<ICluster>(plugin.GetClusters());

            EnhancedPropertyChanged += MainWindowViewModel_EnhancedPropertyChanged;
        }

        private void MainWindowViewModel_EnhancedPropertyChanged(object sender, EnhancedPropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedCluster))
                Console.WriteLine(e.NewValue);
        }

        public ObservableCollection<ICluster> Clusters
        {
            get { return Get<ObservableCollection<ICluster>>(nameof(Clusters)); }
            set { Set(nameof(Clusters), value); }
        }

        public ICluster SelectedCluster
        {
            get { return Get<ICluster>(nameof(SelectedCluster)); }
            set { Set(nameof(SelectedCluster), value); }
        }
    }
}
