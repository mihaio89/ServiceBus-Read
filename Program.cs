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
        static string? connectionString;
        static string? queueName;
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.int.json", optional: true)
                .Build();

            connectionString = configuration["ServiceBus:ServiceBusConnectionString"];
            queueName = configuration["ServiceBus:QueueName"];

           // string session = args[0];

         //   await ProcessMessageEventArgs();

            
            await DeleteDLQ();

        }

        static async Task ProcessMessageEventArgs() {

            // Create a ServiceBusClient and a ServiceBusSessionProcessor
            await using var client = new ServiceBusClient(connectionString);
            var sessionProcessorOptions = new ServiceBusSessionProcessorOptions
            {
                MaxConcurrentSessions = 1,
                SessionIdleTimeout = TimeSpan.FromMinutes(5),
                //MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
               // SessionIds = { session },
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

                // Renew the session lock to prevent it from expiring prematurely
            //    await args.RenewSessionLockAsync();

                // Complete the session message to remove it from the queue
                await args.CompleteMessageAsync(message);
                  // Renew the session lock
              //    await args.RenewSessionLockAsync();
                
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

            await Task.Delay(TimeSpan.FromMinutes(60));

            // Stop the session processor
            await sessionProcessor.StopProcessingAsync();

        }

        
        static async Task DeleteDLQ() {

            await using var client = new ServiceBusClient(connectionString);
            await using var deadLetterReceiver = client.CreateReceiver(queueName + "/$DeadLetterQueue", new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            });

            var messages = await deadLetterReceiver.ReceiveMessagesAsync(maxMessages: 1000);
            foreach (var message in messages)
            {
                // Process the message, if necessary

                Console.WriteLine($"Deleted message {message.MessageId} from DLQ");
            }

        }
        

    }