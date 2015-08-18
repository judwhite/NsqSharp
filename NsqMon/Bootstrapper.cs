using System.Windows;
using NsqMon.Common;
using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Dispatcher;
using NsqMon.Common.Events.Ux;

namespace NsqMon
{
    public class Bootstrapper
    {
        public void Run()
        {
            ConfigureContainer();
            CreateShell();
        }

        protected void ConfigureContainer()
        {
            // Instances
            IoC.RegisterInstance<IDispatcher>(new ApplicationDispatcher());
            IoC.RegisterInstance<IDialogService>(new DialogService());

            // Views
            
            // View Models
            IoC.RegisterType<IMainWindowViewModel, MainWindowViewModel>();
            
            // Events
            IoC.Resolve<IEventAggregator>().Subscribe<MessageBoxEvent>(ShowMessageBox);
        }

        private static void ShowMessageBox(MessageBoxEvent messageBox)
        {
            var result = MessageBox.Show(
                owner: messageBox.Owner as Window,
                messageBoxText: messageBox.MessageBoxText,
                caption: messageBox.Caption,
                button: messageBox.MessageBoxButton,
                icon: messageBox.MessageBoxImage
            );

            messageBox.Result = result;
        }

        protected DependencyObject CreateShell()
        {
            var shell = IoC.Resolve<MainWindow>();
            shell.Show();

            return shell;
        }
    }
}
