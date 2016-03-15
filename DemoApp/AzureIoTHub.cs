using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

static class AzureIoTHub
{
    //
    // Note: this connection string is specific to the device "PIDWheel". To configure other devices,
    // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
    //
    const string deviceConnectionString = "HostName=PIDControlSystemHub.azure-devices.net;DeviceId=PIDWheel;SharedAccessKey=MX2Ueo5ShrdtA7xTPDB6O1Fg0TfY81Lmk1rHew/L+6c=";

    //
    // To monitor messages sent to device "PIDWheel" use iothub-explorer as follows:
    //    iothub-explorer HostName=PIDControlSystemHub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=hWbXWCz9CkzIHj9tZJ2kuWpme5P1fcOsff6SO6KAvdw= monitor-events "PIDWheel"
    //

    // Refer to http://aka.ms/azure-iot-hub-vs-cs-wiki for more information on Connected Service for Azure IoT Hub

    public static async Task SendDeviceToCloudMessageAsync(string str)
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Http1);
        
        var message = new Message(Encoding.ASCII.GetBytes(str));

        await deviceClient.SendEventAsync(message);
    }

    public static async Task<float> ReceiveCloudToDeviceMessageAsync()
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Http1);

        while (true)
        {
            var receivedMessage = await deviceClient.ReceiveAsync();

            if (receivedMessage != null)
            {
                var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                await deviceClient.CompleteAsync(receivedMessage);
                dynamic dataObject = JsonConvert.DeserializeObject(messageData);
                deviceClient.CompleteAsync(receivedMessage);
                return dataObject.ThresholdReached;
            }

            //  Note: In this sample, the polling interval is set to 
            //  10 seconds to enable you to see messages as they are sent.
            //  To enable an IoT solution to scale, you should extend this 
            //  interval. For example, to scale to 1 million devices, set 
            //  the polling interval to 25 minutes.
            //  For further information, see
            //  https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }
}
