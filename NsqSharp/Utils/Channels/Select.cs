using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace NsqSharp.Utils.Channels
{
    /// <summary>
    /// Control structure to send or receive from the first available channel. Chain Case methods and end with a
    /// call to Default or NoDefault.
    /// </summary>
    /// <example>
    /// private static void Main()
    /// {
    ///     var c = new Chan&lt;string&gt;();
    ///     var quit = new Chan&lt;bool&gt;();
    ///
    ///     Task.Factory.StartNew(() => GetNumbers(c, "A", quit));
    ///     Task.Factory.StartNew(() => GetNumbers(c, "B", quit));
    ///     Task.Factory.StartNew(() => GetNumbers(c, "C", quit));
    ///
    ///     int count = 0;
    ///     foreach (var msg in c)
    ///     {
    ///         Console.WriteLine(msg);
    ///
    ///         if (count > 100)
    ///         {
    ///             quit.Send(true);
    ///             quit.Send(true);
    ///             quit.Send(true);
    ///         }
    ///
    ///         count++;
    ///     }
    /// }
    /// 
    /// private static void GetNumbers(Chan&lt;string&gt; c, string name, Chan&lt;bool&gt; quit)
    /// {
    ///    for (var i = 1; !breakLoop; i++)
    ///    {
    ///        Select
    ///            .CaseSend(c, string.Format("{0} {1}", name, i), () =>
    ///            {
    ///                if (name == "A")
    ///                {
    ///                    Thread.Sleep(200);
    ///                }
    ///                if (name == "C")
    ///                {
    ///                    Thread.Sleep(100);
    ///                }
    ///                Thread.Sleep(100);
    ///            })
    ///            .CaseReceive(quit, o =>
    ///            {
    ///                breakLoop = true;
    ///            })
    ///           .NoDefault();
    ///    }
    /// 
    ///    c.Close();
    /// }
    /// </example>
    public static class Select
    {
        /// <summary>
        /// Creates a case for receiving from the specific channel.
        /// </summary>
        /// <param name="c">The channel to receive from. Can be <c>null</c>.</param>
        /// <param name="func">The function to execute with the data received from the channel. Can be <c>null</c></param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to 
        /// Default or NoDefault.</returns>
        public static SelectCase CaseReceive<T>(IReceiveOnlyChan<T> c, Action<T> func = null)
        {
            return new SelectCase().CaseReceive(c, func);
        }

        /// <summary>
        /// Creates a case for receiving from the specific channel.
        /// </summary>
        /// <param name="c">The channel to receive from. Can be <c>null</c>.</param>
        /// <param name="func">The function to execute with the data received from the channel.</param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to 
        /// Default or NoDefault.</returns>
        public static SelectCase CaseReceiveOk<T>(IReceiveOnlyChan<T> c, Action<T, bool> func)
        {
            return new SelectCase().CaseReceiveOk(c, func);
        }

        /// <summary>
        /// Creates a case for sending to the specific channel.
        /// </summary>
        /// <param name="c">The channel to send to. Can be <c>null</c>.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="func">The callback function to execute once the message has been sent. Can be <c>null</c>.</param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to
        /// Default or NoDefault.</returns>
        public static SelectCase CaseSend<T>(ISendOnlyChan<T> c, T message, Action func = null)
        {
            return new SelectCase().CaseSend(c, message, func);
        }
    }

    /// <summary>
    /// Control structure to send or receive from the first available channel. Chain Case methods and end with a call to
    /// Default or NoDefault.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class SelectCase : IDisposable
    {
        private class ReceiveCase
        {
            public IReceiveOnlyChan Chan { get; set; }
        }

        private class SendCase
        {
            public ISendOnlyChan Chan { get; set; }
        }

        private readonly Dictionary<ReceiveCase, Action<object, bool>> _receiveFuncs =
            new Dictionary<ReceiveCase, Action<object, bool>>();

        private readonly Dictionary<SendCase, Tuple<Action, object>> _sendFuncs =
            new Dictionary<SendCase, Tuple<Action, object>>();

        private static readonly TimeSpan _trySendTimeout = TimeSpan.FromMilliseconds(20);
        private static readonly TimeSpan _pumpTimeout = TimeSpan.FromSeconds(5); // TODO: Remove

        private Action _default;
        private bool _hasDefault;
        private AutoResetEvent _ready;

        private bool _defer;

        private bool _isExecuteCalled;

        /// <summary>
        /// Creates a case for receiving from the specific channel.
        /// </summary>
        /// <param name="c">The channel to receive from. Can be <c>null</c>.</param>
        /// <param name="func">The function to execute with the data received from the channel.</param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to
        /// Default or NoDefault.</returns>
        public SelectCase CaseReceiveOk<T>(IReceiveOnlyChan<T> c, Action<T, bool> func)
        {
            if (_isExecuteCalled)
                throw new Exception("select already executed");

            if (c != null)
            {
                _receiveFuncs.Add(
                    new ReceiveCase
                    {
                        Chan = c
                    },
                    func == null
                        ? (Action<object, bool>)null
                        : (v, ok) => func((T)v, ok)
                );
            }

            return this;
        }

        /// <summary>
        /// Creates a case for receiving from the specific channel.
        /// </summary>
        /// <param name="c">The channel to receive from. Can be <c>null</c>.</param>
        /// <param name="func">The function to execute with the data received from the channel. Can be <c>null</c></param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to
        /// Default or NoDefault.</returns>
        public SelectCase CaseReceive<T>(IReceiveOnlyChan<T> c, Action<T> func = null)
        {
            if (_isExecuteCalled)
                throw new Exception("select already executed");

            if (c != null)
            {
                _receiveFuncs.Add(
                    new ReceiveCase
                    {
                        Chan = c
                    },
                    func == null
                        ? (Action<object, bool>)null
                        : (v, ok) => func((T)v)
                );
            }

            return this;
        }

        /// <summary>
        /// Creates a case for sending to the specific channel.
        /// </summary>
        /// <param name="c">The channel to send to. Can be <c>null</c>.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="func">The callback function to execute once the message has been sent. Can be <c>null</c>.</param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to
        /// Default or NoDefault.</returns>
        public SelectCase CaseSend<T>(ISendOnlyChan<T> c, T message, Action func = null)
        {
            if (_isExecuteCalled)
                throw new Exception("select already executed");

            if (c != null)
            {
                _sendFuncs.Add(
                    new SendCase
                    {
                        Chan = c
                    }
                    , new Tuple<Action, object>(func, message)
                );
            }

            return this;
        }

        /// <summary>
        /// Executes a default action if no channels are ready.
        /// </summary>
        /// <param name="func">The callback function to execute if no channels are ready. Can be <c>null</c>.</param>
        public void Default(Action func)
        {
            if (_isExecuteCalled)
                throw new Exception("select already executed");

            _default = func;
            _hasDefault = true;
            Execute();
        }

        /// <summary>
        /// Specifies that no action should be taken if no channels are ready. Blocks until one channel is ready and its
        /// callback function has been executed.
        /// </summary>
        /// <param name="defer">Defers execution to use the same build-up in a loop. Call <see cref="Execute"/> inside the
        /// loop, and wrap in a using or manually call <see cref="Dispose"/> when done.</param>
        public SelectCase NoDefault(bool defer = false)
        {
            if (_isExecuteCalled)
                throw new Exception("select already executed");

            _defer = defer;
            if (!_defer)
                Execute();
            return this;
        }

        private bool CheckCases(out Exception exception)
        {
            exception = null;

            foreach (var kvp in _receiveFuncs)
            {
                var c = kvp.Key.Chan;
                var f = kvp.Value;

                if (!c.IsReadyToSend)
                    continue;

                object o = null;
                bool ok = false;
                bool gotValue = false;

                if (c.TryLockReceive())
                {
                    try
                    {
                        if (c.IsReadyToSend)
                        {
                            o = c.ReceiveOk(out ok);
                            gotValue = true;
                        }
                        else
                        {
                            if (c.IsClosed)
                            {
                                gotValue = true;
                            }
                        }
                    }
                    finally
                    {
                        c.UnlockReceive();
                    }
                }

                if (gotValue)
                {
                    if (f != null)
                    {
                        try
                        {
                            f(o, ok);
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    }
                    return true;
                }
            }

            foreach (var kvp in _sendFuncs)
            {
                var c = kvp.Key.Chan;
                var f = kvp.Value.Item1;
                var d = kvp.Value.Item2;

                bool sentValue = false;
                if (c.TryLockSend())
                {
                    try
                    {
                        sentValue = c.TrySend(d, _trySendTimeout);
                    }
                    finally
                    {
                        c.UnlockSend();
                    }
                }

                if (sentValue)
                {
                    if (f != null)
                    {
                        try
                        {
                            f();
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Executes the select. Only necessary if defer = <c>true</c> was passed to <see cref="NoDefault"/>.
        /// </summary>
        public void Execute()
        {
            Exception caseHandlerException = null;

            try
            {
                if (_hasDefault)
                {
                    bool isDone = CheckCases(out caseHandlerException);

                    if (!isDone)
                    {
                        if (_default != null)
                        {
                            try
                            {
                                _default();
                            }
                            catch (Exception ex)
                            {
                                caseHandlerException = ex;
                            }
                        }
                    }

                    _isExecuteCalled = true;
                }
                else
                {
                    bool isDone = false;

                    if (!_isExecuteCalled)
                    {
                        _ready = new AutoResetEvent(initialState: false);

                        foreach (var c in _sendFuncs.Keys)
                        {
                            c.Chan.AddListenForReceive(_ready);
                        }

                        foreach (var c in _receiveFuncs.Keys)
                        {
                            c.Chan.AddListenForSend(_ready);
                        }

                        _isExecuteCalled = true;
                    }

                    isDone = CheckCases(out caseHandlerException);

                    if (!isDone)
                    {
                        bool done;
                        do
                        {
                            _ready.WaitOne(_pumpTimeout);

                            done = CheckCases(out caseHandlerException);
                        } while (!done);
                    }
                }

                if (!_defer)
                    Dispose();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }

            if (caseHandlerException != null)
                throw caseHandlerException;
        }

        /// <summary>
        /// Clean up references. Only necessary if defer = <c>true</c> was passed to <see cref="NoDefault"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            if (_ready != null)
            {
                foreach (var c in _receiveFuncs.Keys)
                {
                    c.Chan.RemoveListenForSend(_ready);
                }

                foreach (var c in _sendFuncs.Keys)
                {
                    c.Chan.RemoveListenForReceive(_ready);
                }

                _ready.Close();
                _ready = null;
            }

            _receiveFuncs.Clear();
            _sendFuncs.Clear();

            _default = null;
        }
    }

#if NETFX_3_5
    internal class Tuple<TItem1, TItem2>
    {
        private readonly TItem1 _item1;
        private readonly TItem2 _item2;

        public Tuple(TItem1 item1, TItem2 item2)
        {
            _item1 = item1;
            _item2 = item2;
        }

        public TItem1 Item1 { get { return _item1; } }
        public TItem2 Item2 { get { return _item2; } }
    }
#endif
}
