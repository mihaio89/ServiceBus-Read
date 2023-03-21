using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace ServiceBus;
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
            var queueName = configuration["ServiceBus:QueueName"];

            // Create a ServiceBusClient and a ServiceBusSessionProcessor
            await using var client = new ServiceBusClient(connectionString);
            var sessionProcessorOptions = new ServiceBusSessionProcessorOptions
            {
                MaxConcurrentSessions = 1,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(60)
            };
            await using var sessionProcessor = client.CreateSessionProcessor(queueName, sessionProcessorOptions);

            // Add a session handler
            sessionProcessor.ProcessMessageAsync += async args =>
            {
                var message = args.Message;
                // Deserialize the message body to a JObject
                JObject body = JObject.Parse(message.Body.ToString());

                // Extract the i_ext and GK values from the JObject
                string i_ext = (string)body["i_ext"];
                string gk = (string)body["it_bit_it"][0]["GK"];
                //Console.WriteLine($"Received session message with SessionId '{message.SessionId}' and MessageId '{message.MessageId}': {message.Body}");
                Console.WriteLine($"Received session message SessionId '{message.SessionId}' : i_ext = '{i_ext}', GK = '{gk}'");

                // Complete the session message to remove it from the queue
                await args.CompleteMessageAsync(message);
            };

            // Add an error handler to handle any errors that occur during message processing
            sessionProcessor.ProcessErrorAsync += args =>
            {
                Console.WriteLine(args.Exception.ToString());
                return Task.CompletedTask;
            };

            // Start the session processor and wait for session messages to arrive
            Console.WriteLine($"Listening for session messages on queue '{queueName}'...");
            await sessionProcessor.StartProcessingAsync();

            // Keep the processor running for 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(15));

            // Stop the session processor
            await sessionProcessor.StopProcessingAsync();
        }
    }
