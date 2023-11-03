using LHCommonFunctions.Source;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LHWpfControlLibrary.Source.UserControls
{

    /***********************************************************************************************
    * 
    * This UserControl can draws a line chart with time based X-Axis
    * 
    **********************************************************************************************/

    public partial class UC_LineChart : UserControl, INotifyPropertyChanged
    {
        /***********************************************************************************************
        * 
        * Constants
        * 
        **********************************************************************************************/
        //Axes
        private const double _axisTickMarkLength = 4;                                           //Length of the tickmark

        //Place a tick mark label every n-th tickmark 
        private const int _moduloLabelsX = 2;
        private const int _moduloLabelsY = 2;

        //Number of tickmarks/grid-lines
        private const int _numOfTickmarksX = 12;
        private const int _numOfTickmarksY = 10;

        private const int _IncrementStepX = 30 * 60;                                               //The time-step to increment the X-Axis in seconds

        //Dimensions
        private const double _fontSizeCheckbox = 16;
        private const int _fontSizeAxisTitle = 14;
        private const int _fontSizeLabel = 12;
        private const int _marginTextBoxVertical = 15;
        private const int _marginTextBoxHorizontal = 20;
        private const double _marginLabels = 5;
        private const double _marginDefault = 20;

        //Intervals
        private const int _resizeDrawingTimeout = 250;                                             //Timeout to wait after resize event before redrawing the series

        //Stroke
        private const double _axisStrokeWidth = 1;
        private const double _gridStrokeWidth = 1;
        private const double _tickMarkStrokeWidth = 2;


        /***********************************************************************************************
        * 
        * Variables
        * 
        **********************************************************************************************/
        //Objects
        private Canvas _axisCanvas, _seriesCanvas;

        //When the user resizes the window, the resize event will be triggered multiple times in one second.
        //Redrawing all series is a intensive task, so we only want to do this if resizing was finished.
        //Detecting the end of resizing is accomplished by starting a timer when a resize event is detected.
        //If the timer ran out of its maximum time, we know the resizing is over and we can redraw the entire chart.
        private DispatcherTimer _resizeTimer;

        ResourceDictionary _theme;                                                                  //Color theme
        private List<Class_Series> _seriesList; //All series displayed in the chart
        private SolidColorBrush _gridStrokeBrush, _mainStrokeBrush, _textBrush;                     //Colors of the chart

        //Primitive
        private bool _positiveValuesOnly = false;                                                    //Determines if we show negative values or not

        //If enabled, the Y-Axis will automatically scale according to the max/min values of the series
        //Use the property YAutoScaleMode to set it in order to reflect the changes in bindings.
        private bool _yAutoScaleMode;

        private double _axisXLength, _axisYLength;                                                  //Length of X and Y Axes
        private Point _origin;                                                                      //Position of the origin of the diagram.
        private double _pixelFactorX, _pixelFactorY;                                                //Number of pixels per value on the X-Axis / Y-Axis

        //The zoom values determine the number by which the maximum Y-Axis value will be modified,
        //if the user zooms in out.
        private float _axisYZoomIncrementValue; //When zooming out, the maximum Y-Axis value will be incremented by this number
        private float _axisYZoomDecrementValue;   //When zooming in, the maximum Y-Axis value will be decremented by this number                       

        private int _axisXValueLimit;                                                                 //Maximum value which can be displayed on the X-Axis with the current scale
        private float _axisYValueLimit;                                                              //Maximum/Minimum value which can be displayed on the X-Axis with the current scale

        public String AxisXTitle = "";
        public String AxisYTitle = "";


        /***********************************************************************************************
        * 
        * Properties
        * 
        **********************************************************************************************/
        public bool YAutoScaleMode
        {
            get { return _yAutoScaleMode; }
            set
            {
                _yAutoScaleMode = value;
                OnPropertyChanged(nameof(YAutoScaleMode));
            }
        }


        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public UC_LineChart()
        {
            InitializeComponent();
            this.DataContext = this;

            //Initialize variables
            //Objects
            _axisCanvas = new Canvas();
            _seriesCanvas = new Canvas();
            _origin = new Point();

            //Adding the canvases to the main canvas
            canvas.Children.Add(_axisCanvas);
            canvas.Children.Add(_seriesCanvas);

            _theme = new ResourceDictionary();
            _seriesList = new List<Class_Series>();

            //Timer
            _resizeTimer = new DispatcherTimer();
            _resizeTimer.Interval = TimeSpan.FromMilliseconds(_resizeDrawingTimeout);
            _resizeTimer.Tick += TIMResize_Tick;

            vReset();
        }


        /***********************************************************************************************
        * 
        * Events
        * 
        **********************************************************************************************/

        public event PropertyChangedEventHandler PropertyChanged;

        //This function calls the property changed event of a property
        public void OnPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// <summary>
        /// This event is called if the canvas is resized
        /// </summary>
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _seriesCanvas.Visibility = Visibility.Hidden;

            _resizeTimer.Stop();
            _resizeTimer.Start();
            DrawChartFrame();
        }



        /// <summary>
        /// This event is called if the mousewheel was moved
        /// </summary>
        private void Control_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                YAutoScaleMode = false;
                _axisYValueLimit -= _axisYZoomDecrementValue;
            }
            else if (e.Delta < 0)
            {
                YAutoScaleMode = false;
                _axisYValueLimit += _axisYZoomIncrementValue;
            }
            CalculateZoomValues();
            Redraw();
        }

        /// <summary>
        /// This event is called if the auto scale menu item was clicked
        /// </summary>
        private void MIAutoScale_Click(object sender, RoutedEventArgs e)
        {
            YAutoScaleMode = true;
            RescaleY(force: true);
            Redraw();
        }

        /// <summary>
        /// This event is called if the resize timer elapsed successfully
        /// </summary>
        private void TIMResize_Tick(object sender, EventArgs e)
        {
            _resizeTimer.Stop();
            Redraw();
        }

        /// <summary>
        /// This event will be called if the visibility of a series has changed.
        /// </summary>
        private void OnSeriesVisibilityChanged(object sender, EventArgs e)
        {
            RescaleX();
            if (YAutoScaleMode)
            {
                RescaleY();
            }
            Redraw();
        }


        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/

        /// <summary>
        /// This function inserts a new series to the chart.
        /// </summary>
        /// <param name="addingSeries">The series to add to the chart</param>
        /// <param name="seriesPosition">The position of the series. Series will be ordered by this number.
        /// If there is already a series placed at this position, it will be replaced.</param>
        public void AddNewSeries(Class_Series addingSeries, uint seriesPosition)
        {
            if (_seriesList.Count <= seriesPosition)
            {
                //There is no series existing on the current position
                _seriesCanvas.Children.Add(addingSeries.canvas);
                _seriesList.Add(addingSeries);
                _legendPanel.Children.Add(addingSeries.uC_CheckBoxFilled);

                addingSeries.SetColor((SolidColorBrush)_theme[$"Col_{_seriesList.Count - 1}"]);
            }
            else
            {
                //There is a series existing on the current position, so we remove it first
                _seriesCanvas.Children.RemoveAt((int)seriesPosition);
                _seriesList.RemoveAt((int)seriesPosition);
                _legendPanel.Children.RemoveAt((int)seriesPosition);

                _seriesCanvas.Children.Insert((int)seriesPosition, addingSeries.canvas);
                _seriesList.Insert((int)seriesPosition, addingSeries);
                _legendPanel.Children.Insert((int)seriesPosition, addingSeries.uC_CheckBoxFilled);

                addingSeries.SetColor((SolidColorBrush)_theme[$"Col_{seriesPosition}"]);
            }
            addingSeries.VisibilityChangedHandler += new EventHandler(OnSeriesVisibilityChanged);   //Subscribe to the visibility changed event
        }

        /// <summary>
        /// This function analyzes the _axisYValueLimit and performs the calculation of the zoom values.
        /// </summary>
        void CalculateZoomValues()
        {
            float decadeOfYLimit = (float)Math.Pow(10, Math.Floor(Math.Log10(_axisYValueLimit)));
            _axisYZoomIncrementValue = decadeOfYLimit;
            _axisYZoomDecrementValue = _axisYZoomIncrementValue;
            if (Math.Round(_axisYValueLimit / decadeOfYLimit) == 1)
            { //The numbers should be checked for equality, but due to minimal changes they can be slightly off (double)
                _axisYZoomDecrementValue = _axisYZoomIncrementValue / 10;
            }
        }

        /// <summary>
        /// This function draws the frame of the chart containing of axes, labels, tickmarks and titles. 
        /// It calculates necessary values for scaling and sizing.
        /// </summary>
        private void DrawChartFrame()
        {
            if (canvas.ActualWidth == 0 || canvas.ActualHeight == 0)
            {
                //This might happen if the application starts and resizes.
                //We just return here since this is not a reasonable size
                return;
            }

            //Calculation
            //Initialize the TextBlocks to measure their size
            _axisCanvas.Children.Clear();                                                           //Delete all existing children
            TextBlock axisYTitle = new TextBlock();                                                 //TextBlock for Y-Axis
            axisYTitle.FontSize = _fontSizeAxisTitle;
            axisYTitle.Foreground = _textBrush;
            axisYTitle.Text = AxisYTitle;
            Size axisYTitleSize = LHStringFunctions.SZMeasureString(axisYTitle);                    //Measure the size of the Y-Axis tile textbox

            TextBlock axisXTitle = new TextBlock();                                                 //TextBlock for X-Axis
            axisXTitle.FontSize = _fontSizeAxisTitle;
            axisXTitle.Foreground = _textBrush;
            axisXTitle.Text = AxisXTitle;
            Size axisXTitleSize = LHStringFunctions.SZMeasureString(axisXTitle);                    //Measure the size of the X-Axis tile textbox

            TextBlock axisYLabel = new TextBlock();                                                 //Biggest tickmark of the Y-Axis
            axisYLabel.FontSize = _fontSizeLabel;
            axisYLabel.Foreground = _textBrush;

            String axisYTickMarkTextFormat = "0.##";
            if (_axisYZoomDecrementValue < 1)
            {
                int iNumOfDecimals = -(int)Math.Floor(Math.Log10(_axisYZoomDecrementValue));
                iNumOfDecimals = Math.Max(2, iNumOfDecimals);
                axisYTickMarkTextFormat = "0.";
                for (; iNumOfDecimals > 0; iNumOfDecimals--)
                {
                    axisYTickMarkTextFormat += "#";
                }
            }
            axisYLabel.Text = _axisYValueLimit.ToString(axisYTickMarkTextFormat);
            Size axisXLabelSize = LHStringFunctions.SZMeasureString(axisYLabel);

            //Calculate the origin of the chart
            _origin.Y = canvas.ActualHeight - _marginDefault - axisXTitleSize.Height - _marginDefault - axisXLabelSize.Height - _marginLabels - _axisTickMarkLength;    //OriginY is constraint by the bottom
            _origin.X = _marginDefault + axisYTitleSize.Height + _marginDefault + axisXLabelSize.Width + _marginLabels + _axisTickMarkLength;                           //OriginX is constraint by the left
            _axisYLength = _origin.Y - _marginDefault;                                                                                                                      //Calculate the size of the Y-Axis
            _axisXLength = canvas.ActualWidth - _origin.X;                                                                                                                 //Calculate the size of the X-Axis
            _pixelFactorX = _axisXLength / _axisXValueLimit;                                                                                                                   //Calculate pixels per second for setting the series points
            _pixelFactorY = _axisYLength / _axisYValueLimit;                                                                                                                   //Calculate pixels per value for setting the series points

            if (!_positiveValuesOnly)
            {
                _origin.Y -= _axisYLength / 2;
                _pixelFactorY = _axisYLength / (2 * _axisYValueLimit);
            }

            //Y-Axis
            //Y-Axis title
            axisYTitle.LayoutTransform = new RotateTransform(270);
            Canvas.SetLeft(axisYTitle, _marginDefault);                                              //Center the Y-Axis title
            Canvas.SetTop(axisYTitle, _axisYLength / 2 - axisYTitleSize.Width / 2);                 //Center the Y-Axis title
            _axisCanvas.Children.Add(axisYTitle);                                                   //Add the textblock to the canvas

            //Y-Axis line
            Line axisY = new Line();
            axisY.X1 = axisY.X2 = _origin.X;                                                      //Set X-Positions of the Y-Axis
            if (_positiveValuesOnly)
            {
                axisY.Y1 = _origin.Y;
                axisY.Y2 = _origin.Y - _axisYLength;
            }
            else
            {
                axisY.Y1 = _origin.Y + _axisYLength / 2;
                axisY.Y2 = _origin.Y - _axisYLength / 2;
            }
            axisY.Stroke = _mainStrokeBrush;
            axisY.StrokeThickness = _axisStrokeWidth;
            _axisCanvas.Children.Add(axisY);

            //Y-Axis tickmarks
            double tickMarkSpacingY = _axisYLength / _numOfTickmarksY;                    //Spacing of the Y-tickmarks
            double tickMarkYPosX = _origin.X + _axisStrokeWidth;                               //X-Position of the tickmarks
            double labelStep = (double)_axisYValueLimit / _numOfTickmarksY;                 //Tick mark label step value

            if (!_positiveValuesOnly)
            {
                labelStep *= 2;                                                                     //Used to print the hole scale doubled (positive and negative)
            }

            for (int tickMarkYCnt = 0; tickMarkYCnt < _numOfTickmarksY + 1; tickMarkYCnt++)
            {
                //Line
                double y_pos = tickMarkSpacingY * tickMarkYCnt + _marginDefault;
                Line tickMarkY = new Line();
                tickMarkY.X1 = tickMarkYPosX;
                tickMarkY.X2 = tickMarkYPosX - _axisTickMarkLength;
                tickMarkY.Y1 = tickMarkY.Y2 = y_pos;
                tickMarkY.Stroke = _mainStrokeBrush;
                tickMarkY.StrokeThickness = _tickMarkStrokeWidth;
                _axisCanvas.Children.Add(tickMarkY);

                //Grid
                Line gridLineY = new Line();
                gridLineY.X1 = _origin.X;
                gridLineY.X2 = _origin.X + _axisXLength;
                gridLineY.Y1 = gridLineY.Y2 = y_pos;                                                //Reuse the Y-Position of the tickmark
                gridLineY.Stroke = _gridStrokeBrush;
                gridLineY.StrokeThickness = _gridStrokeWidth;
                _axisCanvas.Children.Add(gridLineY);

                //Label
                if (0 == (tickMarkYCnt % _moduloLabelsY))                                   //Check if a label should be placed
                {
                    TextBlock labelY = new TextBlock();
                    labelY.FontSize = _fontSizeLabel;
                    labelY.Foreground = _textBrush;
                    labelY.Text = (_axisYValueLimit - tickMarkYCnt * labelStep).ToString(axisYTickMarkTextFormat);
                    labelY.TextAlignment = TextAlignment.Right;
                    Canvas.SetTop(labelY, y_pos - axisXLabelSize.Height / 2);
                    Canvas.SetRight(labelY, _axisCanvas.ActualWidth - _origin.X + _axisTickMarkLength + _marginLabels);
                    _axisCanvas.Children.Add(labelY);
                }
            }

            //X-Axis
            //X-Axis title
            Canvas.SetTop(axisXTitle, _marginDefault + _axisYLength + _axisTickMarkLength + _marginLabels + axisXTitleSize.Height + _marginDefault);    //Set X-Position of the Textblock
            Canvas.SetLeft(axisXTitle, _origin.X + _axisXLength / 2 - axisXTitleSize.Width / 2);                                                         //Center the X-Axis title
            _axisCanvas.Children.Add(axisXTitle);

            //X-Axis line
            Line axisX = new Line();
            axisX.X1 = _origin.X;
            axisX.X2 = axisX.X1 + _axisXLength;
            axisX.Y1 = axisX.Y2 = _origin.Y;
            axisX.Stroke = _mainStrokeBrush;
            axisX.StrokeThickness = _axisStrokeWidth;
            _axisCanvas.Children.Add(axisX);

            //X-Axis tickmarks
            double tickMarkSpacingX = _axisXLength / _numOfTickmarksX;                    //Spacing of the X-Tickmarks
            double tickMarkXPosY = axisX.Y1;                                                        //Y-Position of the tickmarks

            //Calculate the strings for the X-Axis labels
            int maxMinutes = _axisXValueLimit / 60;
            int labelStepMinutes = maxMinutes / (_numOfTickmarksX / _moduloLabelsX);

            int numOfLabels = _numOfTickmarksX / _moduloLabelsX;
            String[] _axisXLabels = new String[numOfLabels];    //Labels for the X-Axis
            for (int labelCnt = 1; labelCnt <= numOfLabels; labelCnt++)
            {
                int hours = (labelCnt * labelStepMinutes / 60);
                int minutes = (labelCnt * labelStepMinutes) - 60 * hours;
                _axisXLabels[labelCnt - 1] = hours.ToString();
                _axisXLabels[labelCnt - 1] += ":";
                _axisXLabels[labelCnt - 1] += minutes.ToString("00");
            }

            for (int tickMarkXCnt = 1; tickMarkXCnt <= _numOfTickmarksX; tickMarkXCnt++)
            {
                Line tickMarkX = new Line();
                tickMarkX.X1 = tickMarkX.X2 = _origin.X + tickMarkSpacingX * tickMarkXCnt;
                tickMarkX.Y1 = tickMarkXPosY; tickMarkX.Y2 = tickMarkXPosY + _axisTickMarkLength;
                tickMarkX.Stroke = _mainStrokeBrush;
                tickMarkX.StrokeThickness = _tickMarkStrokeWidth;
                _axisCanvas.Children.Add(tickMarkX);

                //Grid
                Line gridLineX = new Line();
                gridLineX.X1 = gridLineX.X2 = tickMarkX.X1;                                         //Reuse the X-Position of the tickmark
                if (_positiveValuesOnly)
                {
                    gridLineX.Y1 = _origin.Y;
                    gridLineX.Y2 = _origin.Y - _axisYLength;

                }
                else
                {
                    gridLineX.Y1 = _origin.Y + _axisYLength / 2;
                    gridLineX.Y2 = _origin.Y - _axisYLength / 2;
                }
                gridLineX.Stroke = _gridStrokeBrush;
                gridLineX.StrokeThickness = _gridStrokeWidth;
                _axisCanvas.Children.Add(gridLineX);

                //Label
                if (0 == (tickMarkXCnt % _moduloLabelsX))                                   //Check if a label should be placed
                {
                    TextBlock labelX = new TextBlock();
                    labelX.FontSize = _fontSizeLabel;
                    labelX.Foreground = _textBrush;
                    labelX.Background = canvas.Background;
                    labelX.Text = _axisXLabels[tickMarkXCnt / _moduloLabelsX - 1];
                    labelX.TextAlignment = TextAlignment.Right;
                    Canvas.SetTop(labelX, axisX.Y1 + _axisTickMarkLength + _marginLabels);
                    Canvas.SetLeft(labelX, _origin.X + tickMarkSpacingX * tickMarkXCnt - LHStringFunctions.SZMeasureString(labelX).Width / 2);
                    _axisCanvas.Children.Add(labelX);
                }
            }

        }

        /// <summary>
        /// This function finds the maximum X-Axis value using the max value of all series.
        /// It will update the _axisXValueLimit if necessary.
        /// </summary>
        /// <returns>True if _axisXValueLimit was updated and the chart needs a redraw, false if not.</returns>
        private bool RescaleX()
        {
            //Before rescaling we analyze all series for max timestamps
            int maxTimestamp = _axisXValueLimit;
            foreach (Class_Series series in _seriesList)
            {
                series.AnalyzeMaxValues();
                if (series.MaxTimestamp > _axisXValueLimit)
                {
                    maxTimestamp = series.MaxTimestamp;
                }
            }
            if (maxTimestamp == _axisXValueLimit)
            {
                //No rescale necessary
                return false;
            }

            _axisXValueLimit = _IncrementStepX * ((maxTimestamp / _IncrementStepX) + 1);
            return true;
        }

        /// <summary>
        /// This function finds the maximum Y-Axis value using the max value of all series.
        /// It will update the _axisYValueLimit if necessary.
        /// </summary>
        /// <param name="force">Set this parameter to true, in order to recalculate the _axisYValueLimit
        /// regardless if there is a new maximum value in the series data</param>
        /// <returns>True if _axisYValueLimit was updated and the chart needs a redraw, false if not.</returns>
        private bool RescaleY(bool force = false)
        {
            //Before auto rescaling we analyze all series for max values
            float maxValue = 0;
            bool resizeNecessary = false;
            _positiveValuesOnly = true;
            foreach (Class_Series series in _seriesList)
            {
                if (!series.IsVisible())
                {
                    continue;
                }
                series.AnalyzeMaxValues();
                if (series.MaxValue > maxValue)
                {
                    maxValue = series.MaxValue;
                    resizeNecessary = true;
                }
                if (series.MinValue < 0)
                {
                    _positiveValuesOnly = false;
                }
                if (!_positiveValuesOnly && series.MinValue < 0 && Math.Abs(series.MinValue) > maxValue)
                {
                    maxValue = -series.MinValue;
                    resizeNecessary = true;
                }
            }

            if (maxValue == 0 || (!resizeNecessary && !force))
            {
                //No rescale necessary
                return false;
            }

            //The zoom increment value is represented by the power of 10 in which the max values is
            //The zoom decrement value has to be lowered if we enter the decade below when zooming in
            //E.g. maxValue = 101 -> _axisYValueLimit = 200; _axisYZoomIncrementValue = 100; _axisYZoomDecrementValue = 100  
            //E.g. maxValue = 99  -> _axisYValueLimit = 100; _axisYZoomIncrementValue = 100; _axisYZoomDecrementValue = 10
            double decadeOfMaxYValue = Math.Pow(10, Math.Floor(Math.Log10(maxValue)));
            _axisYValueLimit = (float)(Math.Floor(maxValue / decadeOfMaxYValue) * decadeOfMaxYValue + decadeOfMaxYValue);
            CalculateZoomValues();
            return true;
        }

        /// <summary>
        /// This function resets the entire line chart by clearing its content.
        /// </summary>
        public void vReset()
        {
            for (int iSeriesCnt = 0; iSeriesCnt < _seriesList.Count; iSeriesCnt++)
            {
                _seriesList[iSeriesCnt].ClearPoints();                                                   //Reset the series
            }

            YAutoScaleMode = true;
            _axisYValueLimit = 10;
            RescaleX();
            RescaleY(force: true);
            Redraw();
        }


        /// <summary>
        /// This function lets you switch the color theme of the chart
        /// </summary>
        /// <param name="theme">The resource dictionary containing the theme</param>
        public void SetColorTheme(ResourceDictionary theme)
        {
            _theme = theme;
            canvas.Background = (SolidColorBrush)_theme["Col_UC_LineChartBackground"];              //Background of the canvas
            _gridStrokeBrush = (SolidColorBrush)_theme["Col_UC_LineChartGridStroke"];               //Main stroke
            _mainStrokeBrush = (SolidColorBrush)_theme["Col_UC_LineChartMainStroke"];               //Main stroke
            _textBrush = (SolidColorBrush)_theme["Col_UC_LineChartText"];                           //TextColor

            for (int iSeriesCnt = 0; iSeriesCnt < _seriesList.Count; iSeriesCnt++)
            {
                _seriesList[iSeriesCnt].SetColor((SolidColorBrush)_theme[$"Col_{iSeriesCnt}"]);    //Set the color of the series
            }
            Redraw();
        }

        public void SetLanguage(ResourceDictionary language)
        {
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(language);
        }

        /// <summary>
        /// This function redraws the entire chart including frame and series.
        /// </summary>
        private void Redraw()
        {
            foreach (Class_Series series in _seriesList)
            {
                series.ClearPoints();
            }
            _seriesCanvas.Visibility = Visibility.Visible;                                          //Series might be hidden from the resize event
            DrawChartFrame();
            UpdateSeries();
        }

        /// <summary>
        /// Call this function to update the chart
        /// Undrawn data will be drawn. Rescaling will be performed automatically.
        /// </summary>
        public void UpdateChart()
        {
            bool rescaled = RescaleX();
            if (YAutoScaleMode)
            {
                rescaled |= RescaleY();
            }
            if (rescaled)
            {
                Redraw();
                return;
            }

            UpdateSeries();
        }

        /// <summary>
        /// This function will trigger the displayed series to draw all undrawn datapoints
        /// </summary>
        private void UpdateSeries()
        {
            double yLimitBottom = _positiveValuesOnly ? _origin.Y : _origin.Y + _axisYLength / 2;
            double yLimitTop = _positiveValuesOnly ? _origin.Y - _axisYLength : _origin.Y - _axisYLength / 2;
            foreach (Class_Series series in _seriesList)
            {
                series.DrawDatapoints(_origin, _pixelFactorX, _pixelFactorY, _origin.X, _origin.X + _axisXLength, yLimitBottom, yLimitTop);
            }
        }

        /***********************************************************************************************
        * 
        * Classes
        * 
        **********************************************************************************************/
        //This class contains the data of a line series
        public class Class_Series : IDisposable
        {
            //Constants
            //Objects
            public Canvas canvas;                                                                   //A canvas to draw the series to
            public EventHandler VisibilityChangedHandler;                                           //Event is triggered if the visibility of the series has changed
            private Polyline Polyline;                                                               //The line of the series
            private SortedList<int, float> dataPoints;                                               //List contains all datapoints. Int is relative time in seconds
            private SolidColorBrush SCBStroke;                                                       //Stroke of the series
            public UC_CheckBoxFilled uC_CheckBoxFilled;                                             //The checkbox for the series
            //Primitive
            public int dataReadIndex;                                                               //The index of the next unread value from the datapoints
            public String sSeriesName;                                                              //Name of the series. Is shown in the legend
            public float MaxValue { get; private set; }
            public float MinValue { get; private set; }
            public int MaxTimestamp { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="seriesName">Name of the series displayed in the legend</param>
            public Class_Series(String seriesName)
            {
                canvas = new Canvas();
                sSeriesName = seriesName;
                dataPoints = new SortedList<int, float>();
                //Initialize the checkbox
                uC_CheckBoxFilled = new UC_CheckBoxFilled();
                uC_CheckBoxFilled.Margin = new Thickness(_marginTextBoxHorizontal, _marginTextBoxVertical, _marginTextBoxHorizontal, _marginTextBoxVertical);
                uC_CheckBoxFilled.Text = sSeriesName;
                uC_CheckBoxFilled.bIsChecked = true;
                uC_CheckBoxFilled.EHCheckedChanged += CheckedChanged;
                uC_CheckBoxFilled.FontSize = _fontSizeCheckbox;

                //Create a new polyline
                Polyline = new Polyline();
                Polyline.StrokeThickness = 2;
                Polyline.StrokeLineJoin = PenLineJoin.Round;
                canvas.Children.Add(Polyline);

                ClearPoints();
            }

            /// <summary>
            /// Destructor
            /// </summary>
            public void Dispose()
            {
                VisibilityChangedHandler = null;
            }
            //Events

            /// <summary>
            /// This event is triggered if the checkbox checked state has changed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void CheckedChanged(object sender, EventArgs e)
            {
                if (uC_CheckBoxFilled.bIsChecked)
                {
                    canvas.Visibility = Visibility.Visible;
                }
                else
                {
                    canvas.Visibility = Visibility.Hidden;
                }
                VisibilityChangedHandler?.Invoke(null, EventArgs.Empty);                            //Call the visibility changed event
            }

            //Functions

            /// <summary>
            /// Use this function to add a datapoint to the series
            /// </summary>
            /// <param name="x">The timestamp in seconds of the datapoint</param>
            /// <param name="y">The value of the datapoint</param>
            public void AddDataPoint(int x, float y)
            {
                if (dataPoints.ContainsKey(x))
                {
                    dataPoints[x] = y;
                    return;
                }

                if (dataPoints.Count > 0 && x < dataPoints.Keys[dataPoints.Count - 1])
                {
                    //If a value was added whose timestamp is before the latest timestamp, we have to redraw the series.
                    ClearPoints();
                }
                dataPoints.Add(x, y);
            }

            /// <summary>
            /// This function will analyze the data and update MaxTimestamp, MaxValue and MinValue
            /// </summary>
            public void AnalyzeMaxValues()
            {
                if (dataPoints.Count == 0)
                {
                    return;
                }

                MaxTimestamp = dataPoints.Keys[dataPoints.Count - 1];

                //We only analyze the values which are not drawn yet, in order to prevent searching through all data every time
                for (int maxValueReadIndex = dataReadIndex; maxValueReadIndex < dataPoints.Count; maxValueReadIndex++)
                {
                    float value = dataPoints.Values[maxValueReadIndex];
                    if (!double.IsFinite(value))
                    {
                        continue;
                    }
                    if (value > MaxValue)
                    {
                        MaxValue = value;
                    }
                    else if (value < MinValue)
                    {
                        MinValue = value;
                    }
                }
            }


            /// <summary>
            /// 
            /// </summary>
            public void DrawDatapoints(Point origin,
                                       double pixelFactorX,
                                       double pixelFactorY,
                                       double graphLeft,
                                       double graphRight,
                                       double graphBottom,
                                       double graphTop)
            {
                if (dataReadIndex >= dataPoints.Count)
                {
                    //This should not happen
                    //Possible case is that the data was exchanged externally
                    ClearPoints();
                }

                if (dataPoints.Count < 2)
                {
                    //We don't draw the series unless there are at least two datapoints
                    return;
                }

                while (dataReadIndex < dataPoints.Count)
                {
                    int timestamp = dataPoints.Keys[dataReadIndex];
                    float value = dataPoints.Values[dataReadIndex];
                    double xPos = origin.X + timestamp * pixelFactorX;
                    double yPos = origin.Y - value * pixelFactorY;

                    //We limit the values, in order not to exceed the current graph boundaries
                    xPos = Math.Clamp(xPos, graphLeft, graphRight);
                    yPos = Math.Clamp(yPos, graphTop, graphBottom);
                    Polyline.Points.Add(new Point(xPos, yPos));

                    AnalyzeMaxValues(); //A new value was added, so we check if it is a new maximum value.
                    dataReadIndex++;
                }
            }

            /// <summary>
            /// The datapoints will be cleared from the polyline.
            /// All status variables will be cleared.
            /// </summary>
            public void ClearPoints()
            {
                dataReadIndex = 0;
                MaxTimestamp = 0;
                MaxValue = 0;
                MinValue = 0;
                Polyline.Points.Clear();
            }

            /// <summary>
            /// Query if the series is visible
            /// </summary>
            /// <returns>True if visible, false if not</returns>
            public bool IsVisible()
            {
                return canvas.Visibility == Visibility.Visible;
            }

            /// <summary>
            /// This function sets the color of the series
            /// </summary>
            /// <param name="color">The color to set</param>
            public void SetColor(SolidColorBrush color)
            {
                SCBStroke = color.Clone();
                uC_CheckBoxFilled.vSetCheckedColor(SCBStroke);
                Polyline.Stroke = SCBStroke;
            }


        }

    }
}
