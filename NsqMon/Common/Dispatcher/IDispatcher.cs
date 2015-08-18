using System;

namespace NsqMon.Common.Dispatcher
{
    /// <summary>
    /// IDispatcher
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Executes the specified delegate with the specified arguments on the thread the <see cref="System.Windows.Threading.Dispatcher" /> was created on.
        /// </summary>
        /// <param name="method">An <see cref="Action" /> which is pushed onto the <see cref="System.Windows.Threading.Dispatcher" /> event queue.</param>
        void BeginInvoke(Action method);

        /// <summary>Determines whether the calling thread is the thread associated with this <see cref="System.Windows.Threading.Dispatcher" />.</summary>
        /// <returns><c>true</c> if the calling thread is the thread associated with this <see cref="System.Windows.Threading.Dispatcher" />; otherwise, <c>false</c>.</returns>
        bool CheckAccess();
    }
}
