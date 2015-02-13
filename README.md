NsqSharp
========

[![Build Status](https://travis-ci.org/judwhite/NsqSharp.svg?branch=master)](https://travis-ci.org/judwhite/NsqSharp)

A .NET client library for [NSQ](https://github.com/bitly/nsq), a realtime distributed messaging platform.

## Work in Progress

This project is currently under development.

If you want to use NSQ within .NET today, check out [NSQnet](https://github.com/ClothesHorse/NSQnet).

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

## Project Goals
- Structurally similar to the official [go-nsq](https://github.com/bitly/go-nsq) client.
- Up to date with the latest stable release of go-nsq.
- Provide similar behavior and semantics as the official package.

## License

This project is open source and released under the [MIT license.](LICENSE)
