
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NsqSharp;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Logging;

await Host.CreateDefaultBuilder(args)
    // .ConfigureAppConfiguration(static b =>
    // {
    //     b.AddModuleConfiguration();
    // })
    .ConfigureServices(static (ctx,sc) =>
    {
        sc.AddHostedService<Worker1>()
            .AddTransient<IBusConfiguration, BusConfiguration>()
            .AddTransient<IBus, NsqBus>()
            .AddTransient<IMessageSerializer, MessageSerializerClass>()
            .AddTransient<IMessageAuditor, MessageAuditorClass>()
            .AddTransient<IMessageTypeToTopicProvider, MessageTypeToTopicProviderClass>()
            .AddTransient<IHandlerTypeToChannelProvider, HandlerTypeToChannelProviderClass>()
            .AddTransient<IDefaultNsqLookupdHttpEndpoints, DefaultNsqLookupdHttpEndpointsClass>()
            .AddTransient<IDefaultThreadsPerHandler, DefaultThreadsPerHandlerClass>()
            .AddTransient<IPleaseWorkConfig, BackOffConfigClass>()
            .AddTransient<IBusStateChangedHandler, BusStateChangedHandlerClass>()
            .AddTransient<NsqSharp.Core.ILogger, RyansLogger>()
            .AddTransient<IPreCreateTopicsAndChannels, PreCreateTopicsAndChannelsClass>()
            .AddTransient<IMessageMutator, MessageMutatorClass>()
            .AddTransient<IMessageTopicRouter, MessageTopicRouterClass>()
            .AddTransient<INsqdPublisher, NsqdPublisherClass>();




    // THIS IS INSTANTIATED BY THE BUS CONFIG SO ALL THAT FOR NOTHING HAHAHAHA
    //    .AddTransient<IBus, NsqBus>()
    //    .AddTransient<ITopicChannelHandlerWrapper, ThisTopicChannel>() // todo: this is done differently
    //    .AddTransient<IMessageTypeToTopicProvider, MessageTypeToTopicProviderClass>()
    //    .AddTransient<IMessageSerializer, MessageSerializerClass>()
    //    .AddTransient<NsqSharp.Core.ILogger, RyansLogger>()
    //    .AddTransient<IMessageMutator, MessageMutatorClass>()
    //    .AddTransient<IMessageTopicRouter, MessageTopicRouterClass>()
    //    .AddTransient<INsqdPublisher, NsqdPublisherClass>();
       
        // sc.AddLogging()
            // .AddSingleton<IIoTFactory, AzureIoTFactory>()
            // .AddSingleton<IScriptProvider, FileSystemScriptProvider>()
            // .AddSingleton<IDeployable, NpgsqlDeploymentService>()
            // .AddHostedService<PurgeService>()
            // .AddHostedService<DeploymentWorker>();
    })
    .RunConsoleAsync();