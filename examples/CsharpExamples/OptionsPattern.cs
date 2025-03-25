using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CsharpExamples;

internal class OptionsPattern
{
    public static async Task RunConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "client:ServiceAddresses:0", "pulsar+ssl://pulsar.service.com:6651" },

                { "producer:Topic:Topic", "persistent://my-tenant/my-namespace/my-topic-1" },
                { "producer:ProducerName", "my-producer" },

                {"consumer:consumerName", "test-consumer" },
                {"consumer:subscriptionName", "consumer-sub-name" },
                {"consumer:topics:0:topic", "my-tenant/my-namespace/my-topic-1" },
                {"consumer:topics:1:topic", "my-tenant/my-namespace/my-topic-2" },

                {"reader:topic:topic", "my-tenant/my-namespace/my-topic-1" },
                {"reader:subscriptionName", "reader-sub-name" },

            })
            .Build();

        var services = new ServiceCollection();

        var uris = configuration.GetSection("client:ServiceAddresses").Get<List<Uri>>();

        // pulsar client options
        services.AddOptions<PulsarClientConfiguration>()
            .Bind(configuration.GetSection("client"))
            .Configure(options => options.Authentication = AuthenticationFactory.Token("my-token"));

        // pulsar producer options
        services.AddOptions<ProducerConfiguration>()
            .Bind(configuration.GetSection("producer"))
            .Configure(options => options.BlockIfQueueFull = true);

        // pulsar consumer options
        services.AddOptions<ConsumerConfiguration<byte[]>>()
            .Bind(configuration.GetSection("consumer"))
            .Configure(options => options.SubscriptionType = SubscriptionType.KeyShared);

        // pulsar reader options
        services.AddOptions<ReaderConfiguration>()
           .Bind(configuration.GetSection("reader"))
           .Configure(options => options.StartMessageFromRollbackDuration = TimeSpan.FromHours(1));

        var provider = services.BuildServiceProvider();

        // setup client builder
        var clientOptions = provider.GetRequiredService<IOptions<PulsarClientConfiguration>>();
        var clientBuilder = new PulsarClientBuilder()
            .With(clientOptions.Value)
            .ListenerName("test-listener");

        // setup producer builder
        var producerOptions = provider.GetRequiredService<IOptions<ProducerConfiguration>>();
        var producerBuilder = (await clientBuilder.BuildAsync())
            .NewProducer()
            .With(producerOptions.Value);

        // setup consumer builder
        var consumerOptions = provider.GetRequiredService<IOptions<ConsumerConfiguration<byte[]>>>();
        var consumerBuilder = (await clientBuilder.BuildAsync())
            .NewConsumer()
            .With(consumerOptions.Value);

        // setup reader builder
        var readerOptions = provider.GetRequiredService<IOptions<ReaderConfiguration>>();
        var readerBuilder = (await clientBuilder.BuildAsync())
            .NewReader()
            .With(readerOptions.Value);
    }
}
