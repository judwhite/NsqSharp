NsqSharp
========

[![Build Status](https://travis-ci.org/judwhite/NsqSharp.svg?branch=master)](https://travis-ci.org/judwhite/NsqSharp)&nbsp;&nbsp;[![License](http://img.shields.io/:license-mit-blue.svg)](http://doge.mit-license.org)&nbsp;&nbsp;[![nuget](https://img.shields.io/nuget/v/NsqSharp.svg)](https://www.nuget.org/packages/NsqSharp)

A .NET client library for [NSQ](https://github.com/bitly/nsq), a realtime distributed messaging platform.

Check out this [slide deck](https://speakerdeck.com/snakes/nsq-nyc-golang-meetup?slide=19) for a quick intro to NSQ.

Watch [Spray Some NSQ On It](https://www.youtube.com/watch?v=CL_SUzXIUuI) by co-author [Matt Reiferson](https://github.com/mreiferson) for an under 30-minute intro to NSQ as a messaging platform.

## Quick Start

Download [`nsqd.exe`](https://github.com/judwhite/NsqSharp/blob/master/nsq-0.3.2-bin/nsqd.exe) and [`nsqlookupd.exe`](https://github.com/judwhite/NsqSharp/blob/master/nsq-0.3.2-bin/nsqlookupd.exe). Alternatively you can build these files from source https://github.com/bitly/nsq.

From two separate command lines run:
```
nsqlookupd

nsqd -lookupd-tcp-address=127.0.0.1:4160
```

To install as Windows Services:

```
mkdir c:\nsq\data

copy /y nsqd.exe c:\nsq
copy /y nsqlookupd.exe c:\nsq

sc create nsqlookupd binpath= "c:\nsq\nsqlookupd.exe" start= auto DisplayName= "nsqlookupd"
sc description nsqlookupd "nsqlookupd 0.3.2"
sc start nsqlookupd

sc create nsqd binpath= "c:\nsq\nsqd.exe -mem-queue-size=0 -lookupd-tcp-address=127.0.0.1:4160 -data-path=c:\nsq\data" start= auto DisplayName= "nsqd"
sc description nsqd "nsqd 0.3.2"
sc start nsqd
```

## C# Examples

`PM> Install-Package NsqSharp`

More examples are in the [Examples/NsqSharp](https://github.com/judwhite/NsqSharp/tree/master/Examples/NsqSharp) and [Examples/NsqSharp.Bus](https://github.com/judwhite/NsqSharp/tree/master/Examples/NsqSharp.Bus) folder.

#### Producer

```C#
using System;
using NsqSharp;

class Program
{
    static void Main()  
    {
        var producer = new Producer("127.0.0.1:4150");
        producer.Publish("test-topic-name", "Hello!");
    
        Console.WriteLine("Enter your message (blank line to quit):");
        string line = Console.ReadLine();
        while (!string.IsNullOrEmpty(line))
        {
            producer.Publish("test-topic-name", line);
            line = Console.ReadLine();
        }

        producer.Stop();
    }
}
```

#### Consumer

```C#
using System;
using System.Text;
using NsqSharp;

class Program
{
    static void Main()  
    {
        // Create a new Consumer for each topic/channel
        var consumer = new Consumer("test-topic-name", "channel-name");
        consumer.AddHandler(new MessageHandler());
        consumer.ConnectToNsqLookupd("127.0.0.1:4161");
    
        Console.WriteLine("Listening for messages. If this is the first execution, it " +
                          "could take up to 60s for topic producers to be discovered.");
        Console.WriteLine("Press enter to stop...");
        Console.ReadLine();

        consumer.Stop();
    }
}

public class MessageHandler : IHandler
{
    /// <summary>Handles a message.</summary>
    public void HandleMessage(Message message)
    {
        string msg = Encoding.UTF8.GetString(message.Body);
        Console.WriteLine(msg);
    }

    /// <summary>
    /// Called when a message has exceeded the specified <see cref="Config.MaxAttempts"/>.
    /// </summary>
    /// <param name="message">The failed message.</param>
    public void LogFailedMessage(Message message)
    {
        // Log failed messages
    }
}
```

## NsqSharp.Bus

The classes in the `NsqSharp.Bus` namespace are meant to provide conveniences for the structure of large scale applications and ease migration from other .NET message buses. The classes in this namespace are not required to use NsqSharp's `Producer` and `Consumer` directly.

Two working examples are available in [Examples/NsqSharp.Bus](https://github.com/judwhite/NsqSharp/tree/master/Examples/NsqSharp.Bus), the simple [PingPong](https://github.com/judwhite/NsqSharp/tree/master/Examples/NsqSharp.Bus/PingPong) application and the multi-process [PointOfSale](https://github.com/judwhite/NsqSharp/tree/master/Examples/NsqSharp.Bus/PointOfSale) application.

These example highlight:
- [StructureMap](https://github.com/structuremap/structuremap) (any dependency injection container will work)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) (any serialization method can be chosen)
- [NLog](https://github.com/NLog/NLog) (any logger can be used)
- Running the handler processes as either console applications or Windows Services with graceful shutdown.

NsqSharp has no 3rd party dependencies. StructureMap 2/3 and Newtonsoft.Json are supported through convenience classes which use reflection for the initial wire-up. Other containers and serializers can be used by implementing `IObjectBuilder` and `IMessageSerializer` wrappers in your code.

The [nsq-0.3.2-bin](https://github.com/judwhite/NsqSharp/tree/master/nsq-0.3.2-bin) folder contains Windows compiled versions of NSQ tools for convenience. The `nsqd` and `nsqlookupd` processes have been modified to run as either console applications or Windows Services; batch files exist to install/uninstall the services. [This repository](https://github.com/judwhite/nsq/tree/master/apps) contains the modified source for running as a Windows Service. If you'd like, you can encourage the NSQ team to support Windows Services in the official repository.

### NsqSharp Project Goals
- Structurally similar to the official [go-nsq](https://github.com/bitly/go-nsq) client.
- Up to date with the latest stable release of go-nsq.
- Provide similar behavior and semantics as the official package.

### Pull Requests

Pull requests and issues are very welcome and appreciated.

When submitting a pull request please keep in mind we're trying to stay as close to [go-nsq](https://github.com/bitly/go-nsq) as possible. This sometimes means writing C# which looks more like Go and follows their file layout. Code in the NsqSharp.Bus namespace should follow C# conventions and more or less look like other code in this namespace.

### License

This project is open source and released under the [MIT license.](LICENSE)
