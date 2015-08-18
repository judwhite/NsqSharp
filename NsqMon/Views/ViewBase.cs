using System.Windows.Controls;
using NsqMon.Common.Mvvm;

namespace NsqMon.Views
{
    public class ViewBase : UserControl
    {
        protected ViewBase(IViewModelBase viewModel)
        {
            DataContext = viewModel;
        }

        public ViewBase()
        {
            // Note: only here to support XAML. Do not throw NotImplementedException() here, XAML complains
        }
    }
}
