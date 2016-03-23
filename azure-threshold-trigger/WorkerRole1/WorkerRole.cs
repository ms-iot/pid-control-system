using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System.Text;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        

        static ServiceClient serviceClient;
        static string iotHubConnectionString = "{YourConnectionStringHere}"; // TODO: Input your connection string here
        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;
        private const float MAX_THROTTLE = 60f; // TODO: Change your MAX_THROTTLE to whatever you want

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            eventHubClient = EventHubClient.CreateFromConnectionString(iotHubConnectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            foreach (string partition in d2cPartitions)
            {
                ReceiveMessagesFromDeviceAsync(cancellationToken, partition);
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }

        private async static Task ReceiveMessagesFromDeviceAsync(CancellationToken cancellationToken, string partition)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (!cancellationToken.IsCancellationRequested)
            {
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine(string.Format("Message received. Partition: {0} Data: '{1}'", partition, data));

                dynamic dataObject = JsonConvert.DeserializeObject(data);
                Console.WriteLine(string.Format("Throttle received: {0}", dataObject.throttle));
                if (dataObject.throttle > MAX_THROTTLE)
                {
                    Console.WriteLine("throttle over " + MAX_THROTTLE);
                    SendCloudToDeviceMessageAsync().Wait();
                }
            }
        }

        private async static Task SendCloudToDeviceMessageAsync()
        {
            string message = "{\"ThresholdReached\":" + MAX_THROTTLE.ToString() + "}";
            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync("PIDWheel", commandMessage);
            return;
        }
    }
}
