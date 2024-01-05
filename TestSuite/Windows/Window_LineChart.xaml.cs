using LHWpfControlLibrary.Source.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TestSuite.Data;

namespace TestSuite.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Window_LineChart : Window
    {

        SortedList<int, UC_LineChart.Class_Series> _manualSeries;
        public Window_LineChart()
        {
            InitializeComponent();

            this.Resources.MergedDictionaries.Add(Class_Settings.themeDictionary);                          //Set the GUI theme

            _chart.SetLanguage(Class_Settings.languageDictionary);
            _chart.SetColorTheme(Class_Settings.themeDictionary);

            _manualSeries = new SortedList<int, UC_LineChart.Class_Series>();

            _chart.AxisXTitle = "Time";
            _chart.AxisYTitle = "Value";

        }

        void ClearTestResults()
        {
            _chart.Reset();
            _stressTestNumOfPoints.Text = "";
        }

        BackgroundWorker testWorker;
        private void Button_StressTestClick(object sender, RoutedEventArgs e)
        {

            if (testWorker != null && testWorker.IsBusy)
            {
                return;
            }
            ClearTestResults();

            int numOfSeries = 6;
            int testDataLength = 48; //Test data in hours
            int testDataInterval = 10; //Interval in seconds

            
            testWorker = new BackgroundWorker();
            testWorker.WorkerSupportsCancellation = true;
            testWorker.WorkerReportsProgress = true;


            UC_LineChart.Class_Series[] testSeries = new UC_LineChart.Class_Series[numOfSeries];
            for (int seriesCnt = 0; seriesCnt < testSeries.Length; seriesCnt++)
            {
                testSeries[seriesCnt] = _chart.AddNewSeries("Series " + seriesCnt, seriesCnt);
            }


            testWorker.ProgressChanged += (o, e) =>
            {
                _stressTestNumOfPoints.Text = "Num of points drawn: " + e.ProgressPercentage;
                _chart.Update();
            };

            testWorker.DoWork += (o, e) =>
            {

                int numOfDataPoints = (testDataLength * 60 * 60) / testDataInterval;
                for (int pointCount = 0; pointCount < numOfDataPoints; pointCount++)
                {
                    int x = pointCount * testDataInterval;
                    for (int seriesCnt = 0; seriesCnt < testSeries.Length; seriesCnt++)
                    {
                        testSeries[seriesCnt].AddDataPoint(x, (10000 * pointCount / numOfDataPoints) * (float)Math.Sin(0.5 * (seriesCnt + 1) * Math.PI * pointCount / (numOfDataPoints)));
                    }
                    if (pointCount % 100 == 0)
                    {
                        testWorker.ReportProgress(pointCount * numOfSeries);
                        Thread.Sleep(100);
                    }
                    if (testWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                testWorker.ReportProgress(numOfDataPoints * numOfSeries);
            };

            testWorker.RunWorkerAsync();
        }

        private void Button_DemoClick(object sender, RoutedEventArgs e)
        {
            ClearTestResults();

            int numOfSeries = 6;
            int testDataLength = 5; //Test data in hours
            int testDataInterval = 60; //Interval in seconds


            BackgroundWorker testWorker;
            testWorker = new BackgroundWorker();
            testWorker.WorkerReportsProgress = true;

            _chart.Reset();


            UC_LineChart.Class_Series[] testSeries = new UC_LineChart.Class_Series[numOfSeries];
            for (int seriesCnt = 0; seriesCnt < testSeries.Length; seriesCnt++)
            {
                testSeries[seriesCnt] = _chart.AddNewSeries("Series " + seriesCnt, seriesCnt);
            }
            int numOfDataPoints = (testDataLength * 60 * 60) / testDataInterval;
            for (int pointCount = 0; pointCount < numOfDataPoints; pointCount++)
            {
                int x = pointCount * testDataInterval;
                for (int seriesCnt = 0; seriesCnt < testSeries.Length; seriesCnt++)
                {
                    testSeries[seriesCnt].AddDataPoint(x, (float)(Math.Pow(10, seriesCnt - 4) * Math.Sin(2 * Math.PI * pointCount / (numOfDataPoints))));
                }
            }
            _chart.Update();
        }

        private void Button_ResetClick(object sender, RoutedEventArgs e)
        {
            _chart.Reset();
        }

        private void Button_ClearClick(object sender, RoutedEventArgs e)
        {
            _chart.Clear();
        }

        private void Button_UpdateClick(object sender, RoutedEventArgs e)
        {
            _chart.Update();
        }

        private void Button_AddSeriesClick(object sender, RoutedEventArgs e)
        {
            int seriesNumber = (int)_seriesPosition.dCurrentNumber;
            if (_manualSeries.ContainsKey(seriesNumber))
            {
                _manualSeries.Remove(seriesNumber);
            }
            UC_LineChart.Class_Series addingSeries = _chart.AddNewSeries("TestSeries" + seriesNumber, seriesNumber);
            _manualSeries.Add(seriesNumber, addingSeries);
        }

        private void Button_AddPointsClick(object sender, RoutedEventArgs e)
        {
            const int numOfValuesNecessaryForNegative = 100;
            int seriesNumber = (int)_seriesPosition.dCurrentNumber;
            if (!_manualSeries.ContainsKey(seriesNumber))
            {
                return;
            }
            for (int dataPointCnt = 0; dataPointCnt < _numOfPointsToAdd.dCurrentNumber; dataPointCnt++)
            {
                _manualSeries.Values[seriesNumber].AddDataPoint(10 * dataPointCnt, -10 * dataPointCnt + 10 * (numOfValuesNecessaryForNegative - 1));
            }
        }
    }
}
