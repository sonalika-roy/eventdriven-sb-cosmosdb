using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace eventdrivenapp
{


    public static class SendMessageToDB
    {
        [FunctionName("SendMessageToDB")]
        [return: ServiceBus("srtest", Connection = "outputSbMsg")]

        public static async Task Run(
            [ServiceBusTrigger("srtest", Connection = "outputSbMsg")]
        ToDoItem[] toDoItemsIn,
            [CosmosDB(
                databaseName: "ToDoList",
                collectionName: "Items",
                ConnectionStringSetting = "CosmosDBConnection")]
                IAsyncCollector<ToDoItem> toDoItemsOut,
            ILogger log)
        {
            try { 
            log.LogInformation($"C# ServiceBus queue trigger function processed {toDoItemsIn?.Length} items");
            foreach (ToDoItem toDoItem in toDoItemsIn)
            {
                // log.LogInformation(message: $"Description={toDoItem.Description}");
                await toDoItemsOut.AddAsync(toDoItem);
            }
              }
              catch (Exception ex)
            {
                string message = $"Something went wrong. Exception thrown: {ex.Message}";
               // result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }

}
