NsqSharp
========

[![Build Status](https://travis-ci.org/judwhite/NsqSharp.svg?branch=master)](https://travis-ci.org/judwhite/NsqSharp)

A .NET client library for [NSQ](https://github.com/bitly/nsq), a realtime distributed messaging platform.

## Work in Progress

This project is currently suitable for building proof-of-concept code, receiving pull requests and reporting issues. Thorough testing is encouraged before using in production as large scale testing has not yet been completed.

The public API has not reached stability. Future commits may change the API slightly.

## Examples

#### Publisher

```C#
var w = new Producer("127.0.0.1:4150");
w.SetLogger(new ConsoleLogger(), LogLevel.Debug);

w.Publish("string-topic-name", "Hello!");
w.Publish("bytes-topic-name", new byte[] { 1, 2, 3, 4 });

// ...

w.Stop();
```

#### Consumer

```C#
// Create a new Consumer for each topic or channel
var r = new Consumer("string-topic-name", "channel-name");
r.SetLogger(new ConsoleLogger(), LogLevel.Debug);

r.AddHandler(/* instance of IHandler */);

r.ConnectToNSQD("127.0.0.1:4150");
// or r.ConnectToNSQLookupd("127.0.0.1:4161");

// ...

r.Stop();
```

More examples in the Examples folder.

## NsqSharp Project Goals
- Structurally similar to the official [go-nsq](https://github.com/bitly/go-nsq) client.
- Up to date with the latest stable release of go-nsq.
- Provide similar behavior and semantics as the official package.

## Pull Requests

When submitting a pull request please keep in mind we're trying to stay as close to [go-nsq](https://github.com/bitly/go-nsq) as possible. This sometimes means writing C# which looks more like Go and follows their file layout.

## NsqSharp.Bus

NsqSharp.Bus is an optional IoC-friendly abstraction layer built on top of NsqSharp. The motivation for this library is to provide conveniences for large scale applications and ease migration from other .NET message buses. This library is in the early phases of development. It is not required to use NsqSharp.

## License

This project is open source and released under the [MIT license.](LICENSE)
