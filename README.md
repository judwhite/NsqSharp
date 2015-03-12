NsqSharp
========

[![Build Status](https://travis-ci.org/judwhite/NsqSharp.svg?branch=master)](https://travis-ci.org/judwhite/NsqSharp) [![License](http://img.shields.io/:license-mit-blue.svg)](http://doge.mit-license.org)

A .NET client library for [NSQ](https://github.com/bitly/nsq), a realtime distributed messaging platform.

## Work in Progress

The public API has not reached stability. Future commits may change the API slightly.

Pull requests and issues are very welcome and appreciated.

## Examples

#### Producer

```C#
var w = new Producer("127.0.0.1:4150");
w.Publish("string-topic-name", "Hello!");
w.Publish("bytes-topic-name", new byte[] { 1, 2, 3, 4 });

// ...

w.Stop();
```

#### Consumer

```C#
// Create a new Consumer for each topic/channel
var r = new Consumer("string-topic-name", "channel-name");
r.AddHandler(/* instance of IHandler */);
r.ConnectToNSQD("127.0.0.1:4150"); // or r.ConnectToNSQLookupd("127.0.0.1:4161");

// ...

r.Stop();
```

More examples in the [Examples/NsqSharp](https://github.com/judwhite/NsqSharp/tree/master/Examples/NsqSharp) folder.

### NsqSharp Project Goals
- Structurally similar to the official [go-nsq](https://github.com/bitly/go-nsq) client.
- Up to date with the latest stable release of go-nsq.
- Provide similar behavior and semantics as the official package.

### Pull Requests

When submitting a pull request please keep in mind we're trying to stay as close to [go-nsq](https://github.com/bitly/go-nsq) as possible. This sometimes means writing C# which looks more like Go and follows their file layout. Code in the NsqSharp.Bus namespace should follow C# conventions and more or less look like other code in this namespace.

## NsqSharp.Bus

NsqSharp.Bus is an optional IoC-friendly abstraction layer built on top of NsqSharp. The motivation for this library is to provide conveniences for large scale applications and ease migration from other .NET message buses. This library is in the mid-phases of development. It is not required to use NsqSharp.

A working example is available in the [Examples/NsqSharp.Bus/PointOfSale](https://github.com/judwhite/NsqSharp/tree/master/Examples/NsqSharp.Bus/PointOfSale) folder.

The example highlights:
- [StructureMap](https://github.com/structuremap/structuremap) (any dependency injection container will work)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) (any serialization method can be chosen)
- [NLog](https://github.com/NLog/NLog) (any logger can be used)
- Running the handler processes as either console applications or Windows Services with graceful shutdown.

NsqSharp has no 3rd party dependencies. StructureMap 2/3 and Newtonsoft.Json are supported through convenience classes which use reflection for the initial wire-up. Other containers and serializers can be used by implementing `IObjectBuilder` and `IMessageSerializer` wrappers in your code.

The [nsq-0.3.2-bin](https://github.com/judwhite/NsqSharp/tree/master/nsq-0.3.2-bin) folder contains Windows compiled versions of NSQ tools for convenience. The `nsqd` and `nsqlookupd` processes have been modified to run as either console applications or Windows Services; batch files exist to install/uninstall the services. [This repository](https://github.com/judwhite/nsq/tree/master/apps) contains the modified source for running as a Windows Service. Please encourage the NSQ team to support Windows Services in the official repository :)

## License

This project is open source and released under the [MIT license.](LICENSE)
