using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OxyPlot;
using OxyPlot.Series;
using System.ComponentModel;
using OxyPlot.Axes;

namespace Histogram
{
    public class Measurement
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public DateTime DateTime { get; set; }
    }

    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private PlotModel plotModel;
        public PlotModel PlotModel
        {
            get { return plotModel; }
            set { plotModel = value; OnPropertyChanged("PlotModel"); }
        }
        private DateTime lastUpdate = DateTime.Now;

        public MainViewModel()
        {
            PlotModel = new PlotModel { Title = "Speed" };
            SetUpModel();
            LoadData();
        }

        private readonly List<OxyColor> colors = new List<OxyColor>
                                            {
                                                OxyColors.Green,
                                                OxyColors.IndianRed,
                                                OxyColors.Coral,
                                                OxyColors.Chartreuse,
                                                OxyColors.Azure
                                            };

        private readonly List<MarkerType> markerTypes = new List<MarkerType>
                                                   {
                                                       MarkerType.Plus,
                                                       MarkerType.Star,
                                                       MarkerType.Diamond,
                                                       MarkerType.Triangle,
                                                       MarkerType.Cross
                                                   };

        private void SetUpModel()
        {
            var dateAxis = new DateTimeAxis()
            {
                Position = AxisPosition.Bottom,
                Title = "Date",
                StringFormat = "dd/MM/yy HH:mm",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IntervalLength = 80
            };
            PlotModel.Axes.Add(dateAxis);
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                Title = "Value"
            };
            PlotModel.Axes.Add(valueAxis);
        }

        private void LoadData()
        {
            var measurements = new List<Measurement>();

            // no previous data
            measurements.Add(new Measurement() { Id = 0, DateTime = DateTime.Now, Value = 0 });

            foreach (var data in measurements)
            {
                var lineSerie = new LineSeries
                {
                    StrokeThickness = 2,
                    MarkerSize = 3,
                    MarkerStroke = colors[0],
                    MarkerType = markerTypes[0],
                    CanTrackerInterpolatePoints = false,
                    Title = "Motor RPM",
                    Smooth = false,
                };

                var guideLine = new LineSeries
                {
                    StrokeThickness = 2,
                    MarkerSize = 3,
                    MarkerStroke = colors[1],
                    MarkerType = markerTypes[1],
                    CanTrackerInterpolatePoints = false,
                    Title = "Expected RPM",
                    Smooth = false,
                };

                lineSerie.Points.Add(new DataPoint(DateTimeAxis.ToDouble(data.DateTime), data.Value));
                guideLine.Points.Add(new DataPoint(DateTimeAxis.ToDouble(data.DateTime), 0));
                PlotModel.Series.Add(lineSerie);
                PlotModel.Series.Add(guideLine);
            }
        }

        public void UpdateModel(double value, double expectedSpeedValue)
        {
            var lineSerie = PlotModel.Series[0] as LineSeries;
            if (lineSerie != null)
            {
                lineSerie.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now), value));
            }

            var guideLine = PlotModel.Series[1] as LineSeries;
            if (guideLine != null)
            {
                guideLine.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now), expectedSpeedValue));
            }

            lastUpdate = DateTime.Now;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
