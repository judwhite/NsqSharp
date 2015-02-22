NsqSharp
========

[![Build Status](https://travis-ci.org/judwhite/NsqSharp.svg?branch=master)](https://travis-ci.org/judwhite/NsqSharp)

A .NET client library for [NSQ](https://github.com/bitly/nsq), a realtime distributed messaging platform.

## Work in Progress

This project is currently suitable for building proof-of-concept code, receiving pull requests and reporting issues. Thorough testing would be encouraged before using in production as I've not yet completed my own large scale testing.

The public API has not reached stability. Future commits may change the API slightly.

## Examples

#### Publisher

```C#
// NOTE: Work in progress

var w = new Producer("127.0.0.1:4150");
w.SetLogger(new ConsoleLogger(), LogLevel.Debug);

w.Publish("string-topic-name", "Hello!");
w.Publish("bytes-topic-name", new byte[] { 1, 2, 3, 4 });

// ...

w.Stop();
```

#### Consumer

```C#
// NOTE: Work in progress

// Create a new Consumer for each topic or channel
var r = new Consumer("string-topic-name", "channel-name");
r.SetLogger(new ConsoleLogger(), LogLevel.Debug);

r.AddHandler(/* instance of IHandler */);

r.ConnectToNSQD("127.0.0.1:4150");
// or r.ConnectToNSQLookupd("127.0.0.1:4161");

// ...

r.Stop();
```

## NsqSharp Project Goals
- Structurally similar to the official [go-nsq](https://github.com/bitly/go-nsq) client.
- Up to date with the latest stable release of go-nsq.
- Provide similar behavior and semantics as the official package.

## Pull Requests

When submitting a pull request please keep in mind we're trying to stay as close to [go-nsq](https://github.com/bitly/go-nsq) as possible. This sometimes means writing C# which looks more like Go and follows their file layout.

#### NsqSharp.Bus

NsqSharp.Bus is an optional IoC-friendly abstraction layer built on top of NsqSharp. The original motivation for this addon was to ease migration from another .NET service bus. This addon is in the early phases of development. It is not required to use NsqSharp.

More information to come on this project soon, possibly in another repository.

## License

This project is open source and released under the [MIT license.](LICENSE)
