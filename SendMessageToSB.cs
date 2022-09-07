using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Azure.Messaging.ServiceBus;
using Bogus;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using static Microsoft.Azure.Amqp.Serialization.SerializableType;
using Faker;

namespace eventdrivenapp
{

    public class SendMessageToSB
    {
        public List Tags { get; set; }
        public class DeviceReading
        {
            [JsonProperty("id")]
            public string DeviceId { get; set; }
            public decimal DeviceTemperature { get; set; }
            public string DamageLevel { get; set; }
            public int DeviceAgeInDays { get; set; }
        }
        static string connectionString = "";

        // name of your Service Bus queue
        static string queueName = "orders";
        // the client that owns the connection and can be used to create senders and receivers
        static ServiceBusClient client;

        // the sender used to publish messages to the queue
        static ServiceBusSender sender;


        [FunctionName("SendMessageToSB")]
        [return: ServiceBus("orders", Connection = "outputSbMsg")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            IActionResult result = null;
            client = new ServiceBusClient(connectionString);
            sender = client.CreateSender(queueName);

            try
            {
                

                var deviceIterations = new Faker<DeviceReading>()
                .RuleFor(i => i.DeviceId, (fake) => Guid.NewGuid().ToString())
                .RuleFor(i => i.DeviceTemperature, (fake) => Math.Round(fake.Random.Decimal(0.00m, 30.00m), 2))
                .RuleFor(i => i.DamageLevel, (fake) => fake.PickRandom(new List<string> { "Low", "Medium", "High" }))
                .RuleFor(i => i.DeviceAgeInDays, (fake) => fake.Random.Number(1, 60))
                .GenerateLazy(5000);
                // create a batch 
                using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
                
                foreach (var reading in deviceIterations)
                {
                    var eventReading = JsonConvert.SerializeObject(reading);
                    log.LogInformation(eventReading);
                    string message = $"Service Bus queue messages created at: {DateTime.Now}";
                    log.LogInformation(message);
                     messageBatch.TryAddMessage(new ServiceBusMessage(eventReading));


                    //await outputSbQueue.AddAsync(eventReading);
                    //message = $"Service Bus queue messages sent: {DateTime.Now}";
                    
                }
             
                await sender.SendMessagesAsync(messageBatch);
                Console.WriteLine($"A batch of messages has been published to the queue.");

                
               
            }
            catch (Exception ex)
            {
                string message = $"Something went wrong. Exception thrown: {ex.Message}";
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;

        }
    }
}
