using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Histogram
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainViewModel viewModel;

        public MainPage()
        {
            viewModel = new MainViewModel();
            DataContext = viewModel;

            stopwatch.Start();

            var task = Task.Run(async () =>
            {
                while (true)
                {
                    await reRender();
                    Task.Delay(1000);
                }
            });

            this.InitializeComponent();
        }
        
        private System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
        private long lastUpdateMilliSeconds;

        private async Task reRender()
        {
            if (stopwatch.ElapsedMilliseconds > lastUpdateMilliSeconds + 1000)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                 {
                     viewModel.UpdateModel(0, ExpectedSpeed.Value);
                     Plot1.InvalidatePlot(); // this refreshes the plot
                     lastUpdateMilliSeconds = stopwatch.ElapsedMilliseconds;
                 });
            }
        }
    }
}
