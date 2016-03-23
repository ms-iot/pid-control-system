using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Maker;
using Windows.Devices;
using Windows.System.Threading;
using System.Threading;
using Windows.Devices.Sensors;
using System.ComponentModel;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DemoApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const double MAX_RPM = 500;
        private const double PID_INTERVAL_MS = 30;

        // The PID gain constants were derived from trial and error tuning
        private const float PROPORTIONAL_GAIN = 0.05f;
        private const float INTEGRAL_GAIN = 0.008f;
        private const float DERIVATIVE_GAIN = 0.1f;

        Motor motor;
        PidController.PidController pid;
        AutoResetEvent _pidReady = new AutoResetEvent(true);
        AutoResetEvent _azurePipeReady = new AutoResetEvent(true);
        ThreadPoolTimer pidTimer;
        Accelerometer accelerometer;

        // Only for the demo-use case of limiting the throttle via IoTHub
        float iotHubThresholdValue = 100f;

        public MainPage()
        {
            accelerometer = Accelerometer.GetDefault();
            pid = new PidController.PidController(PROPORTIONAL_GAIN, INTEGRAL_GAIN, DERIVATIVE_GAIN, 100f, 0f);
            pid.SetPoint = 0;
            motor = new Motor(0, 36, 250);

            this.InitializeComponent();
        }

        private async void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            await toggleAccelerometer.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                var val = 0f;
                if (togglePID.IsOn)
                {
                    // Limit the accelerometer PID control to 500 RPM max
                    val = (float)args.Reading.AccelerationZ * -500f;
                    if (val > 500) val = 500;
                    if (val < 0) val = 0;
                    pid.SetPoint = (float)val;
                    Slider.Value = (int)val;
                }
                else
                {
                    val = (float)args.Reading.AccelerationZ * -100f;
                    if (val > 100) val = 100;
                    if (val < 0) val = 0;
                    motor.Throttle = (double)val;
                    Slider.Value = (int)val * 12.0;
                }
                
            }));
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (togglePID.IsOn) // Set the target RPM
            {
                pid.SetPoint = (float)e.NewValue;
                targetRPM.Text = e.NewValue.ToString();
            }
            else  // Set the throttle
            {
                var val = e.NewValue / 12.0;
                motor.Throttle = val;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await motor.Initialize();
            ThreadPoolTimer.CreatePeriodicTimer(UpdateUI, TimeSpan.FromMilliseconds(300));
            ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                _azurePipeReady.WaitOne();
                SendToAzureIoTHub(_azurePipeReady);
            }, TimeSpan.FromSeconds(1));
            ReceiveCloudToDeviceThrottleMessage();
        }

        private async void ReceiveCloudToDeviceThrottleMessage()
        {
            iotHubThresholdValue = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
            ReceiveCloudToDeviceThrottleMessage();
        }

        private async void SendToAzureIoTHub(AutoResetEvent resetEvent)
        {
            await AzureIoTHub.SendDeviceToCloudMessageAsync("{\"rpm\":"+motor.RPM + ",\"throttle\":" + motor.Throttle + "}");
            resetEvent.Set();
        }

        private async void UpdateUI(ThreadPoolTimer timer)
        {
            await RPM_Gauge.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                RPM_Gauge.Value = motor.RPM;
                Throttle_Gauge.Value = motor.Throttle;

                // This is a temporary UI workaround for the fact that the current RPM is
                // only calculated on an interrupt so if the motor stops spinning completely
                // the RPM is never updated to reflect 0.
                if (motor.Throttle == 0.0)
                    RPM_Gauge.Value = 0.0;
            }));
        }

        private void UpdatePID(AutoResetEvent resetEvent)
        {
            pid.ProcessVariable = motor.RPM;
            if (iotHubThresholdValue < pid.ControlVariable)
            {
                motor.Throttle = iotHubThresholdValue;
            }
            else
            {
                motor.Throttle = pid.ControlVariable;
            }
            resetEvent.Set();
        }

        private void togglePID_Toggled(object sender, RoutedEventArgs e)
        {
            if (togglePID.IsOn)
            {
                // Lock the PID on the current RPM
                pid.ProcessVariable = motor.RPM;
                pid.SetPoint = motor.RPM;
                targetIndicator.Visibility = Visibility.Visible;
                targetRPM.Text = pid.SetPoint.ToString();

                pidTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
                {
                    _pidReady.WaitOne();
                    UpdatePID(_pidReady);
                }, TimeSpan.FromMilliseconds(PID_INTERVAL_MS));
            }
            else
            {
                targetIndicator.Visibility = Visibility.Collapsed;

                // Turn off the PID controller
                pidTimer.Cancel();
                pidTimer = null;
            }
        }

        private void toggleAccelerometer_Toggled(object sender, RoutedEventArgs e)
        {
            if (toggleAccelerometer.IsOn)
            {
                Slider.IsEnabled = false;
                accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            }
            else
            {
                Slider.IsEnabled = true;
                accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            }
        }
    }
}
