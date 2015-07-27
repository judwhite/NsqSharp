using System;

namespace NsqSharp
{
  using System.Collections.Generic;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;
  using NsqSharp.Core;
  using NsqSharp.Logging;
  using NsqSharp.Utils;
  using NsqSharp.Utils.Channels;

  // https://github.com/bitly/go-nsq/blob/master/producer.go

  /// <summary>
  /// IConn interface
  /// </summary>
  internal interface IConn
  {
    /// <summary>
    /// Connect dials and bootstraps the nsqd connection
    /// (including IDENTIFY) and returns the IdentifyResponse
    /// </summary>
    IdentifyResponse Connect();

    /// <summary>
    /// Close idempotently initiates connection close
    /// </summary>
    void Close();

    /// <summary>
    /// WriteCommand is a thread safe method to write a Command
    /// to this connection, and flush.
    /// </summary>
    void WriteCommand(Command command);
  }

  /// <summary>
  /// <para>Producer is a high-level type to publish to NSQ.</para>
  ///
  /// <para>A Producer instance is 1:1 with a destination nsqd
  /// and will lazily connect to that instance (and re-connect)
  /// when Publish commands are executed.</para>
  /// <seealso cref="Publish(string, string)"/>
  /// <seealso cref="Publish(string, byte[])"/>
  /// <seealso cref="Stop"/>
  /// </summary>
  public sealed partial class Producer
  {
    internal long _id;
    private readonly string _addr;
    private IConn _conn;
    private readonly Config _config;

    private readonly ILog _logger;

    private readonly Chan<byte[]> _responseChan;
    private readonly Chan<byte[]> _errorChan;
    private Chan<int> _closeChan;

    private readonly Chan<ProducerResponse> _transactionChan;
    private readonly Queue<ProducerResponse> _transactions = new Queue<ProducerResponse>();
    private int _state;

    private int _concurrentProducers;
    private int _stopFlag;
    private readonly Chan<int> _exitChan;
    private readonly WaitGroup _wg = new WaitGroup();
    private readonly object _guard = new object();

    private readonly Func<Producer, IConn> _connFactory;
  }

  /// <summary>
  /// ProducerResponse is returned by the async publish methods
  /// to retrieve metadata about the command after the
  /// response is received.
  /// </summary>
  public class ProducerResponse
  {
    internal Command _cmd;
    internal Chan<ProducerResponse> _doneChan;

    /// <summary>
    /// the error (or nil) of the publish command
    /// </summary>
    public Exception Error { get; set; }

    /// <summary>
    /// the slice of variadic arguments passed to PublishAsync or MultiPublishAsync
    /// </summary>
    public object[] Args { get; set; }

    internal void finish()
    {
      if (_doneChan != null)
      {
        _doneChan.Send(this);
      }
    }
  }

  public sealed partial class Producer : IConnDelegate
  {
    /// <summary>
    /// Initializes a new instance of the Producer class.
    /// </summary>
    /// <param name="nsqdAddress">The nsqd address.</param>
    public Producer(string nsqdAddress)
      : this(nsqdAddress, new Config())
    {
    }

    /// <summary>
    /// Initializes a new instance of the Producer class.
    /// </summary>
    /// <param name="nsqdAddress">The nsqd address.</param>
    /// <param name="config">The config. After Config is passed into NewProducer the values are
    /// no longer mutable (they are copied).</param>
    public Producer(string nsqdAddress, Config config)
      : this(nsqdAddress, config, null)
    {
    }

    private Producer(string addr, Config config, Func<Producer, IConn> connFactory)
    {
      if (string.IsNullOrEmpty(addr))
      {
        throw new ArgumentNullException("addr");
      }
      if (config == null)
      {
        throw new ArgumentNullException("config");
      }

      _id = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds%10000; // TODO: Remove

      config.Validate();

      _addr = addr;
      _config = config.Clone();

      _logger = LogProvider.For<Producer>();

      _transactionChan = new Chan<ProducerResponse>();
      _exitChan = new Chan<int>();
      _responseChan = new Chan<byte[]>();
      _errorChan = new Chan<byte[]>();

      if (connFactory == null)
      {
        connFactory = p => new Conn(_addr, _config, p);
      }

      _connFactory = connFactory;
    }

    /// <summary>Returns the address of the Producer.</summary>
    /// <returns>The address of the Producer.</returns>
    public override string ToString()
    {
      return _addr;
    }

    /// <summary>
    /// <para>Stop initiates a graceful stop of the Producer (permanent).</para>
    ///
    /// <para>NOTE: this blocks until completion</para>
    /// </summary>
    public void Stop()
    {
      lock (_guard)
      {
        if (Interlocked.CompareExchange(ref _stopFlag, 1, 0) != 0)
        {
          // already closed
          return;
        }
        _logger.Info("stopping");
        _exitChan.Close();
        close();
        _wg.Wait();
        Thread.Sleep(500);
      }
    }

    /// <summary>
    ///     <para>Publishes a message <paramref name="body"/> to the specified <paramref name="topic"/>
    ///     but does not wait for the response from nsqd.</para>
    ///     
    ///     <para>When the Producer eventually receives the response from nsqd, the Task will return a
    ///     <see cref="ProducerResponse"/> instance with the supplied <paramref name="args"/> and the response error if
    ///     present.</para>
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="body">The message body.</param>
    /// <param name="args">A variable-length parameters list containing arguments. These arguments will be returned on
    ///     <see cref="ProducerResponse.Args"/>.
    /// </param>
    /// <returns>A Task&lt;ProducerResponse&gt; which can be awaited.</returns>
    public Task<ProducerResponse> PublishAsync(string topic, byte[] body, params object[] args)
    {
      var doneChan = new Chan<ProducerResponse>();
      sendCommandAsync(Command.Publish(topic, body), doneChan, args);
      return Task.Factory.StartNew(() => doneChan.Receive());
    }

    /// <summary>
    ///     <para>Publishes a string <paramref name="value"/> message to the specified <paramref name="topic"/>
    ///     but does not wait for the response from nsqd.</para>
    ///     
    ///     <para>When the Producer eventually receives the response from nsqd, the Task will return a
    ///     <see cref="ProducerResponse"/> instance with the supplied <paramref name="args"/> and the response error if
    ///     present.</para>
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="value">The message body.</param>
    /// <param name="args">A variable-length parameters list containing arguments. These arguments will be returned on
    ///     <see cref="ProducerResponse.Args"/>.
    /// </param>
    /// <returns>A Task&lt;ProducerResponse&gt; which can be awaited.</returns>
    public Task<ProducerResponse> PublishAsync(string topic, string value, params object[] args)
    {
      return PublishAsync(topic, Encoding.UTF8.GetBytes(value), args);
    }

    /// <summary>
    ///     <para>Publishes a collection of message <paramref name="bodies"/> to the specified <paramref name="topic"/>
    ///     but does not wait for the response from nsqd.</para>
    ///     
    ///     <para>When the Producer eventually receives the response from nsqd, the Task will return a
    ///     <see cref="ProducerResponse"/> instance with the supplied <paramref name="args"/> and the response error if
    ///     present.</para>
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="bodies">The collection of message bodies.</param>
    /// <param name="args">A variable-length parameters list containing arguments. These arguments will be returned on
    ///     <see cref="ProducerResponse.Args"/>.
    /// </param>
    /// <returns>A Task&lt;ProducerResponse&gt; which can be awaited.</returns>
    public Task<ProducerResponse> MultiPublishAsync(string topic, ICollection<byte[]> bodies, params object[] args)
    {
      var doneChan = new Chan<ProducerResponse>();
      var cmd = Command.MultiPublish(topic, bodies);
      sendCommandAsync(cmd, doneChan, args);
      return Task.Factory.StartNew(() => doneChan.Receive());
    }

    /// <summary>
    ///     Synchronously publishes a message <paramref name="body"/> to the specified <paramref name="topic"/>, throwing
    ///     an exception if publish failed.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="body">The message body.</param>
    public void Publish(string topic, byte[] body)
    {
      sendCommand(Command.Publish(topic, body));
    }

    /// <summary>
    ///     Synchronously publishes string <paramref name="value"/> message to the specified <paramref name="topic"/>,
    ///     throwing an exception if publish failed.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="value">The message body.</param>
    public void Publish(string topic, string value)
    {
      Publish(topic, Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    ///     Synchronously publishes a collection of message <paramref name="bodies"/> to the specified
    ///     <paramref name="topic"/>, throwing an exception if publish failed.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="bodies">The collection of message bodies.</param>
    public void MultiPublish(string topic, ICollection<byte[]> bodies)
    {
      var cmd = Command.MultiPublish(topic, bodies);
      sendCommand(cmd);
    }

    private void sendCommand(Command cmd)
    {
      var doneChan = new Chan<ProducerResponse>();

      try
      {
        sendCommandAsync(cmd, doneChan);
      }
      catch (Exception)
      {
        doneChan.Close();
        throw;
      }

      var t = doneChan.Receive();
      if (t.Error != null)
      {
        throw t.Error;
      }
    }

    private readonly Action _noopAction = () => { };
    private readonly Action<int> _throwErrStoppedAction = b => { throw new ErrStopped(); };

    private void sendCommandAsync(Command cmd, Chan<ProducerResponse> doneChan, params object[] args)
    {
      Interlocked.Increment(ref _concurrentProducers);

      var t = new ProducerResponse
      {
        _cmd = cmd,
        _doneChan = doneChan,
        Args = args
      };

      try
      {
        if (_state != (int) State.Connected)
        {
          Connect();
        }

        Select
          .CaseSend(_transactionChan, t, _noopAction)
          .CaseReceive(_exitChan, _throwErrStoppedAction)
          .NoDefault();
      }
      catch (Exception ex)
      {
        t.Error = ex;
        GoFunc.Run(() => t.finish());
      }
      finally
      {
        Interlocked.Decrement(ref _concurrentProducers);
      }
    }

    /// <summary>
    ///     Connects to nsqd. Calling this method is optional; otherwise, Connect will be lazy invoked when Publish is
    ///     called.
    /// </summary>
    /// <exception cref="ErrStopped">Thrown if the Producer has been stopped.</exception>
    /// <exception cref="ErrNotConnected">Thrown if the Producer is currently waiting to close and reconnect.</exception>
    public void Connect()
    {
      lock (_guard)
      {
        if (_stopFlag == 1)
        {
          throw new ErrStopped();
        }

        switch (_state)
        {
          case (int) State.Init:
            break;
          case (int) State.Connected:
            return;
          default:
            throw new ErrNotConnected();
        }

        _logger.Info(string.Format("{0} connecting to nsqd", _addr));

        _conn = _connFactory(this);
        try
        {
          _conn.Connect();
        }
        catch (Exception ex)
        {
          _conn.Close();
          _logger.ErrorException(string.Format("({0}) error connecting to nsqd", _addr), ex);
          throw;
        }

        _state = (int) State.Connected;
        _closeChan = new Chan<int>();
        _wg.Add(1);
        GoFunc.Run(router, string.Format("Producer:router P{0}", _id));
      }
    }

    private void close()
    {
      const int newValue = (int) State.Disconnected;
      const int comparand = (int) State.Connected;
      if (Interlocked.CompareExchange(ref _state, newValue, comparand) != comparand)
      {
        return;
      }

      _conn.Close();

      GoFunc.Run(() =>
      {
        // we need to handle this in a goroutine so we don't
        // block the caller from making progress
        _wg.Wait();
        _state = (int) State.Init;
      }, string.Format("Producer:close P{0}", _id));
    }

    private void router()
    {
      bool doLoop = true;

      using (var select =
        Select
          .CaseReceive(_transactionChan, t =>
          {
            _transactions.Enqueue(t);
            try
            {
              _conn.WriteCommand(t._cmd);
            }
            catch (Exception ex)
            {
              _logger.ErrorException(string.Format("({0}) sending command", _conn), ex);
              close();
            }
          })
          .CaseReceive(_responseChan, data =>
            popTransaction(FrameType.Response, data)
          )
          .CaseReceive(_errorChan, data =>
            popTransaction(FrameType.Error, data)
          )
          .CaseReceive(_closeChan, o => { doLoop = false; })
          .CaseReceive(_exitChan, o => { doLoop = false; })
          .NoDefault(true))
      {
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (doLoop)
        {
          select.Execute();
        }
      }

      transactionCleanup();
      _wg.Done();
      _logger.Info("exiting router");
    }

    private void popTransaction(FrameType frameType, byte[] data)
    {
      var t = _transactions.Dequeue();
      if (frameType == FrameType.Error)
      {
        t.Error = new ErrProtocol(Encoding.UTF8.GetString(data));
      }
      t.finish();
    }

    private void transactionCleanup()
    {
      // clean up transactions we can easily account for
      foreach (var t in _transactions)
      {
        t.Error = new ErrNotConnected();
        t.finish();
      }
      _transactions.Clear();

      // spin and free up any writes that might have raced
      // with the cleanup process (blocked on writing
      // to transactionChan)
      bool doLoop = true;
      // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
      while (doLoop)
      {
        Select
          .CaseReceive(_transactionChan, t =>
          {
            t.Error = new ErrNotConnected();
            t.finish();
          })
          .Default(() =>
          {
            // keep spinning until there are 0 concurrent producers
            if (_concurrentProducers == 0)
            {
              doLoop = false;
              return;
            }
            // give the runtime a chance to schedule other racing goroutines
            Thread.Sleep(TimeSpan.FromMilliseconds(5));
            // TODO: create PR in go-nsq: is continue necessary in default case?
          });
      }
    }

    void IConnDelegate.OnResponse(Conn c, byte[] data)
    {
      _responseChan.Send(data);
    }

    void IConnDelegate.OnError(Conn c, byte[] data)
    {
      _errorChan.Send(data);
    }

    void IConnDelegate.OnMessage(Conn c, Message m)
    {
      // no-op
    }

    void IConnDelegate.OnMessageFinished(Conn c, Message m)
    {
      // no-op
    }

    void IConnDelegate.OnMessageRequeued(Conn c, Message m)
    {
      // no-op
    }

    void IConnDelegate.OnBackoff(Conn c)
    {
      // no-op
    }

    void IConnDelegate.OnContinue(Conn c)
    {
      // no-op
    }

    void IConnDelegate.OnResume(Conn c)
    {
      // no-op
    }

    void IConnDelegate.OnIOError(Conn c, Exception err)
    {
      close();
    }

    void IConnDelegate.OnHeartbeat(Conn c)
    {
      // no-op
    }

    void IConnDelegate.OnClose(Conn c)
    {
      lock (_guard)
      {
        _closeChan.Close();
      }
    }
  }
}