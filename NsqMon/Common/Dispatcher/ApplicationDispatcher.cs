using System;
using System.Windows;

namespace NsqMon.Common.Dispatcher
{
    /// <summary>
    /// ApplicationDispatcher
    /// </summary>
    public class ApplicationDispatcher : IDispatcher
    {
        /// <summary>
        /// Executes the specified delegate with the specified arguments on the thread the <see cref="System.Windows.Threading.Dispatcher"/> was created on.
        /// </summary>
        /// <param name="method">An <see cref="Action"/> which is pushed onto the <see cref="System.Windows.Threading.Dispatcher"/> event queue.</param>
        public void BeginInvoke(Action method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (Application.Current.Dispatcher.CheckAccess())
                method();
            else
                Application.Current.Dispatcher.BeginInvoke(method);
        }

        /// <summary>
        /// Determines whether the calling thread is the thread associated with this <see cref="System.Windows.Threading.Dispatcher"/>.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the calling thread is the thread associated with this <see cref="System.Windows.Threading.Dispatcher"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CheckAccess()
        {
            return Application.Current.Dispatcher.CheckAccess();
        }
    }
}
