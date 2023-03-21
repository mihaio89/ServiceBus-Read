using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace ServiceBus
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load the configuration file
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.int.json", optional: true)
                .Build();

            // Get the configuration values
            var connectionString = configuration["ServiceBus:ServiceBusConnectionString"];
            var topicName = configuration["ServiceBus:TopicName"];
            Console.WriteLine(topicName);

            // Create a ServiceBusAdministrationClient
            var adminClient = new ServiceBusAdministrationClient(connectionString);

            // Get the list of available subscriptions in the topic
            var subscriptions = adminClient.GetSubscriptionsAsync(topicName);

            // Create a ServiceBusClient
            var client = new ServiceBusClient(connectionString);

            // Receive messages from each subscription
            await foreach (var subscription in subscriptions)
            {
                Console.WriteLine(subscription.SubscriptionName);

                // Create a session processor for this subscription
                var sessionProcessor = client.CreateSessionProcessor(
                    topicName,
                    subscription.SubscriptionName,
                    new ServiceBusSessionProcessorOptions
                    {
                        MaxConcurrentSessions = 1
                    });

                // Process messages from the session
                sessionProcessor.ProcessMessageAsync += async args =>
                {
                    // Deserialize the message body to a JObject
                    JObject body = JObject.Parse(args.Message.Body.ToString());

                    // Extract the i_ext and GK values from the JObject
                    string i_ext = (string)body["i_ext"];
                    string gk = (string)body["it_bit_it"][0]["GK"];

                    // Print the message contents
                    Console.WriteLine($"Received message from subscription '{subscription.SubscriptionName}': i_ext = '{i_ext}', GK = '{gk}'");

                    // Complete the message to remove it from the queue
                    await args.CompleteMessageAsync(args.Message);
                };

                            // Set the ProcessErrorAsync handler
                sessionProcessor.ProcessErrorAsync += args =>
                {
                    Console.WriteLine(args.Exception.ToString());
                    return Task.CompletedTask;
                };

                // Start processing messages from the session
                await sessionProcessor.StartProcessingAsync();

                // Wait for user input to stop processing messages from the session
                Console.WriteLine("Press any key to stop processing messages from this subscription...");
                Console.ReadKey();

                // Stop processing messages from the session
                await sessionProcessor.StopProcessingAsync();
            }

            // Close the ServiceBusClient
            await client.DisposeAsync();
        }
    }
}
