using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NsqSharp.Channels
{
    /// <summary>
    /// Control structure to send or receive from the first available channel. Chain Case methods and end with a call to Default or NoDefault.
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
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to Default or NoDefault.</returns>
        public static SelectInstance CaseReceive<T>(Chan<T> c, Action<T> func)
        {
            return new SelectInstance().CaseReceive<T>(c, func);
        }

        /// <summary>
        /// Creates a case for sending to the specific channel.
        /// </summary>
        /// <param name="c">The channel to send to. Can be <c>null</c>.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="func">The callback function to execute once the message has been sent. Can be <c>null</c>.</param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to Default or NoDefault.</returns>
        public static SelectInstance CaseSend<T>(Chan<T> c, T message, Action func)
        {
            return new SelectInstance().CaseSend(c, message, func);
        }
    }

    /// <summary>
    /// Control structure to send or receive from the first available channel. Chain Case methods and end with a call to Default or NoDefault.
    /// </summary>
    public class SelectInstance
    {
        private readonly Dictionary<IChan, Action<object>> _receiveFuncs = new Dictionary<IChan, Action<object>>();
        private readonly Dictionary<IChan, Tuple<Action, object>> _sendFuncs = new Dictionary<IChan, Tuple<Action, object>>();
        
        private Action _default;
        private bool _hasDefault;

        private bool _isExecuteCalled;

        /// <summary>
        /// Creates a case for receiving from the specific channel.
        /// </summary>
        /// <param name="c">The channel to receive from. Can be <c>null</c>.</param>
        /// <param name="func">The function to execute with the data received from the channel. Can be <c>null</c></param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to Default or NoDefault.</returns>
        public SelectInstance CaseReceive<T>(Chan<T> c, Action<T> func)
        {
            if (c != null)
                _receiveFuncs.Add(c, func == null ? (Action<object>)null : o => func((T)o));
            return this;
        }

        /// <summary>
        /// Creates a case for sending to the specific channel.
        /// </summary>
        /// <param name="c">The channel to send to. Can be <c>null</c>.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="func">The callback function to execute once the message has been sent. Can be <c>null</c>.</param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to Default or NoDefault.</returns>
        public SelectInstance CaseSend<T>(Chan<T> c, T message, Action func)
        {
            if (c != null)
                _sendFuncs.Add(c, new Tuple<Action, object>(func, message));
            return this;
        }

        /// <summary>
        /// Executes a default action if no channels are ready.
        /// </summary>
        /// <param name="func">The callback function to execute if no channels are ready. Can be <c>null</c>.</param>
        public void Default(Action func)
        {
            _default = func;
            _hasDefault = true;
            Execute();
        }

        /// <summary>
        /// Specifies that no action should be taken if no channels are ready. Blocks until one channel is ready and its callback function has been executed.
        /// </summary>
        public void NoDefault()
        {
            Execute();
        }

        private bool CheckCases()
        {
            foreach (var kvp in _receiveFuncs)
            {
                var c = kvp.Key;
                var f = kvp.Value;

                if (c.IsClosed)
                    continue;

                object o = null;
                bool gotValue = false;
                if (c.TryLockReceive())
                {
                    try
                    {
                        if (c.IsReadyToSend)
                        {
                            o = c.Receive();
                            gotValue = true;
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
                        f(o);
                    return true;
                }
            }

            foreach (var kvp in _sendFuncs)
            {
                var c = kvp.Key;
                var f = kvp.Value.Item1;
                var d = kvp.Value.Item2;

                if (c.IsClosed)
                    continue;

                bool sentValue = false;
                if (c.TryLockSend())
                {
                    try
                    {
                        if (c.IsReadyToReceive)
                        {
                            c.Send(d);
                            sentValue = true;
                        }
                    }
                    finally
                    {
                        c.UnlockSend();    
                    }
                }

                if (sentValue)
                {
                    if (f != null)
                        f();

                    return true;
                }
            }

            return false;
        }

        private void Execute()
        {
            if (_isExecuteCalled)
                throw new Exception("Default/NoDefault can only be called once per select");

            _isExecuteCalled = true;

            AutoResetEvent ready = null;

            if (_hasDefault)
            {
                bool isDone = CheckCases();

                if (!isDone)
                {
                    if (_default != null)
                        _default();
                }
            }
            else
            {
                ready = new AutoResetEvent(initialState: false);

                foreach (var c in _sendFuncs.Keys)
                {
                    c.AddListener(ready);
                }

                foreach (var c in _receiveFuncs.Keys)
                {
                    c.AddListener(ready);
                }

                bool isDone = CheckCases();

                if (!isDone)
                {
                    bool done;
                    do
                    {
                        bool signaled = ready.WaitOne(TimeSpan.FromMilliseconds(3000));
                        if (!signaled)
                        {
                            Debug.WriteLine("signaled not received");
                        }
                        done = CheckCases();
                    } while (!done);
                }
            }

            CleanUp(ready);
        }

        private void CleanUp(AutoResetEvent ready)
        {
            if (ready != null)
            {
                foreach (var c in _receiveFuncs.Keys)
                {
                    c.RemoveListener(ready);
                }

                foreach (var c in _sendFuncs.Keys)
                {
                    c.RemoveListener(ready);
                }

                ready.Dispose();
            }

            _receiveFuncs.Clear();
            _sendFuncs.Clear();

            _default = null;
        }
    }
}
