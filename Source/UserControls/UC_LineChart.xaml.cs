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

    public partial class UC_LineChart : UserControl
    {
        /***********************************************************************************************
        * 
        * Constants
        * 
        **********************************************************************************************/
        //Axes
        private const double D_AXIS_X_SIZE_FACTOR = 0.8;
        private const double D_AXIS_TICK_MARK_SIZE = 4;
        private const int I_MODULO_MAIN_LABELS_X = 2, I_MODULO_MAIN_LABELS_Y = 2;                   //Place a label every n-th tickmark 
        private const int I_NUM_OF_MAIN_TICK_MARKS_X = 12, I_NUM_OF_MAIN_TICK_MARKS_Y = 10;
        private const int I_TIME_INCREMENT = 30 * 60;                                               //The timestep to increment the X-Axis in seconds

        //Dimensions
        private const double D_FONSTIZE_CHECKBOX = 16;
        private const int I_MARGIN_CHECKBOX_VERTICAL = 15, I_MARGIN_CHECKBOX_HORIZONTAL = 20;       //Margin of the checkboxes

        //Intervalls
        private const int I_INTERVAL_RESIZE_TIMER = 25;                                             //Interval of the timer for checking resize

        //Stroke
        const double D_AXIS_STROKE_WIDTH = 1;
        const double D_GRID_STROKE_WIDTH = 1;
        const double D_TICK_MARK_STROKE_WIDTH = 2;

        //Text
        const double D_LABEL_MARGIN = 5, D_TEXT_MARGIN = 20;
        const int I_FONT_SIZE_AXIS_TITLE = 14, I_FONT_SIZE_LABEL = 12;


        /***********************************************************************************************
        * 
        * Variables
        * 
        **********************************************************************************************/
        //Objects
        private Canvas CVSAxis, CVSSeries;
        private DispatcherTimer TIMResize;                                                          //This timer starts when the control is resized. It detects if the resize is finished by checking a timeout
        ResourceDictionary RDSeriesColors = new ResourceDictionary();                               //Colors of the series

        public SortedDictionary<String, Class_Series> SDSeries;
        private SolidColorBrush SCBGridStroke, SCBMainStroke, SCBText;                              //Colors of the chart

        //Primitive
        private bool bAutoScaleMode = true;                                                         //Auto scale mode
        private double dAxisXLength, dAxisYLength;                                                  //Length of X and Y Axes
        private double dOriginX, dOriginY;                                                          //Position of the origin
        private double dPixelsPerSecond, dPixelsPerValue;                                           //Number of pixels per second on the X-Axis and valueon the Y-Axis
        private double dMaxSeriesValue;                                                             //Maximum value of all series
        private double dAxisYIncrementValue, dAxisYDecrementValue;                                  //Values for decrement and increment the Y-Axis max value on user scaling
        private int iAxisXMaxValue = 0, iAxisYMaxValue = 100, iAxisYMaxValueFirstDigit = 1;         //Initial values for the axes maximums. 



        private String[] asAxisXLabels = new String[I_NUM_OF_MAIN_TICK_MARKS_X / I_MODULO_MAIN_LABELS_X];    //Labels for the X-Axis

        //Bindings

        /***********************************************************************************************
        * 
        * Construtor
        * 
        **********************************************************************************************/
        public UC_LineChart()
        {
            InitializeComponent();

            //Initialize objects

            //Canvas
            CVSAxis = new Canvas();
            CVSSeries = new Canvas();
            //Adding the canvases to the main canvas
            canvas.Children.Add(CVSAxis);
            canvas.Children.Add(CVSSeries);

            //ResourceDictionary
            RDSeriesColors = new ResourceDictionary();


            //Sorted Dictionary
            SDSeries = new SortedDictionary<String, Class_Series>();

            //Timer
            TIMResize = new DispatcherTimer();
            TIMResize.Interval = TimeSpan.FromMilliseconds(I_INTERVAL_RESIZE_TIMER);
            TIMResize.Tick += TIMResize_Tick;


            vIncrementMaxTime();                                                                    //Set the initial max time value
        }

        /***********************************************************************************************
        * 
        * Commands
        * 
        **********************************************************************************************/

        /***********************************************************************************************
        * 
        * Events
        * 
        **********************************************************************************************/
        //This event is called if the canvas is resized
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CVSSeries.Visibility = Visibility.Hidden;                                               //Hide all series here 

            TIMResize.Stop();
            TIMResize.Start();
            vDrawAxes();                                                                            //Redraw the axis on every resize
        }

        //This event is called if the mousewheel was moved
        private void Control_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                bAutoScaleMode = false;                                                             //Disable auto scale mode
                iAxisYMaxValue += (int)dAxisYIncrementValue;                                        //Increment the max value
                iAxisYMaxValueFirstDigit++;                                                         //Increment the first digit 
                if (iAxisYMaxValueFirstDigit == 10)                                                 //Transition from 9 to 10 -> increment value * 10
                {
                    iAxisYMaxValueFirstDigit = 1;
                    dAxisYIncrementValue *= 10;
                }
                else if (iAxisYMaxValueFirstDigit == 2)                                             //Transition from 1 to 2 -> Decrement value * 10 ( = increment value)  
                {
                    dAxisYDecrementValue = dAxisYIncrementValue;
                }
            }
            else if (e.Delta < 0 && iAxisYMaxValue > 1)
            {
                bAutoScaleMode = false;                                                             //Disable auto scale mode
                iAxisYMaxValue -= (int)dAxisYDecrementValue;
                iAxisYMaxValueFirstDigit--;
                if (iAxisYMaxValueFirstDigit == 1)                                                  //Transition from 2 to 1 -> decrement value / 10
                {
                    dAxisYDecrementValue /= 10;
                }
                else if (iAxisYMaxValueFirstDigit == 0)                                             //Transition from 1 to 0 (10 to 9) -> increment value / 10 ( = decrement value)
                {
                    iAxisYMaxValueFirstDigit = 9;
                    dAxisYIncrementValue = dAxisYDecrementValue;
                }
            }
            vDrawAll();
        }


        //This event is called if the auto scale menu item was clicked
        private void MIAutoScale_Click(object sender, RoutedEventArgs e)
        {
            bAutoScaleMode = true;
            vRescaleY();
        }

        //This event is called if the resize timer elapsed successfully
        private void TIMResize_Tick(object sender, EventArgs e)
        {
            TIMResize.Stop();
            vDrawAll();
        }

        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function inserts a new series to the chart
        public void vAddNewSeries(String qsSeriesName, SortedList<int, double> qSLDataPoints)
        {
            Class_Series addingSeries = new Class_Series(qsSeriesName, qSLDataPoints);
            if (qSLDataPoints.Count > 0)                                                            //Check if the sd contains data
            {
                //Find the maximum value for the time 
                int iMaxTimeValue = qSLDataPoints.Keys.Last();                                      //Maximum time value of the added list
                while (iMaxTimeValue > iAxisXMaxValue)
                {
                    vIncrementMaxTime();                                                            //Recalculate the maximum time of the X-Axis
                }
            }

            //Check if a the max value of the series is the new biggest value
            foreach (double dValue in qSLDataPoints.Values)
            {
                if (dValue > dMaxSeriesValue)
                {
                    dMaxSeriesValue = dValue;
                }
            }
            if (true == bAutoScaleMode)
            {
                vRescaleY();
            }
            addingSeries.vSetColor((SolidColorBrush)RDSeriesColors[$"Col_{SDSeries.Count}"]);       //Set the color of the series
            CVSSeries.Children.Add(addingSeries.canvas);                                            //Add the series canvas to the canvas of series
            SDSeries.Add(addingSeries.sSeriesName, addingSeries);                                   //Add series to the dictionary of series
            WPLegend.Children.Add(addingSeries.uC_CheckBoxFilled);                                  //Add the ckeckbox control
        }

        //This function redraws the entire chart
        private void vDrawAll()
        {
            //Axis
            vDrawAxes();

            //Series
            CVSSeries.Visibility = Visibility.Visible;                                              //Show all series
            foreach (Class_Series class_Series in SDSeries.Values)
            {
                vDrawSeries(class_Series);
            }

        }

        //This function draws the axes and the axes titles
        private void vDrawAxes()
        {
            //Calculation
            //Initialize the TextBlocks to measure their size
            CVSAxis.Children.Clear();                                                               //Delete all existing children
            TextBlock TBLAxisYTitle = new TextBlock();                                              //TextBlock for Y-Axis
            TBLAxisYTitle.FontSize = I_FONT_SIZE_AXIS_TITLE;
            TBLAxisYTitle.Foreground = SCBText;
            TBLAxisYTitle.Text = "$_Unit$";
            Size SZTBLAxisYTitle = LHStringFunctions.SZMeasureString(TBLAxisYTitle);                //Measure the size of the textbox

            TextBlock TBLAxisYLabel = new TextBlock();                                              //Biggest tickmark of the Y-Axis
            TBLAxisYLabel.FontSize = I_FONT_SIZE_LABEL;
            TBLAxisYLabel.Foreground = SCBText;
            TBLAxisYLabel.Text = iAxisYMaxValue.ToString();
            Size SZTBLAxisYLabel = LHStringFunctions.SZMeasureString(TBLAxisYLabel);                //Measure the size of the textbox

            //Calculate the origin of the chart
            dOriginY = canvas.ActualHeight - D_TEXT_MARGIN - SZTBLAxisYTitle.Height - D_TEXT_MARGIN - SZTBLAxisYLabel.Height - D_LABEL_MARGIN - D_AXIS_TICK_MARK_SIZE;    //OriginY is constraint by the bottom
            dOriginX = D_TEXT_MARGIN + SZTBLAxisYTitle.Height + D_TEXT_MARGIN + SZTBLAxisYLabel.Width + D_LABEL_MARGIN + D_AXIS_TICK_MARK_SIZE;                           //OriginX is constraint by the left
            dAxisYLength = dOriginY - D_TEXT_MARGIN;                                                                                                                      //Calculate the size of the Y-Axis
            dAxisXLength = canvas.ActualWidth - dOriginX;                                                                                                                 //Calculate the size of the X-Axis
            dPixelsPerSecond = dAxisXLength / (iAxisXMaxValue);                                                                                                           //Calculate pixels per second for setting the series points
            dPixelsPerValue = dAxisYLength / iAxisYMaxValue;                                                                                                              //Calculate pixels per value for setting the series points

            //Y-Axis
            //Y-Axis title
            TBLAxisYTitle.LayoutTransform = new RotateTransform(270);
            Canvas.SetLeft(TBLAxisYTitle, D_TEXT_MARGIN);                                           //Center the Y-Axis title
            Canvas.SetTop(TBLAxisYTitle, dAxisYLength / 2 - SZTBLAxisYTitle.Width / 2);             //Center the Y-Axis title
            CVSAxis.Children.Add(TBLAxisYTitle);                                                    //Add the textblock to the canvas

            //Y-Axis line
            Line LIAxisY = new Line();
            LIAxisY.X1 = LIAxisY.X2 = dOriginX;                                                     //Set X-Positions of the Y-Axis
            LIAxisY.Y1 = dOriginY - dAxisYLength; LIAxisY.Y2 = dOriginY;                            //Set length the Y-Axis
            LIAxisY.Stroke = SCBMainStroke;
            LIAxisY.StrokeThickness = D_AXIS_STROKE_WIDTH;
            CVSAxis.Children.Add(LIAxisY);

            //Y-Axis tickmarks
            double dTickMarkSpacingY = dAxisYLength / I_NUM_OF_MAIN_TICK_MARKS_Y;                   //Spacing of the Y-Tickmarks
            double dTickMarkYPosX = dOriginX + D_AXIS_STROKE_WIDTH;                                 //X-Position of the tickmarks
            double dLabelStep = (double)iAxisYMaxValue / I_NUM_OF_MAIN_TICK_MARKS_Y;                //Tick mark lable step value
            for (int iTickMarkYCnt = 0; iTickMarkYCnt < I_NUM_OF_MAIN_TICK_MARKS_Y; iTickMarkYCnt++)
            {
                //Line
                Line LITickMark = new Line();
                LITickMark.X1 = dTickMarkYPosX; LITickMark.X2 = dTickMarkYPosX - D_AXIS_TICK_MARK_SIZE;
                LITickMark.Y1 = LITickMark.Y2 = dTickMarkSpacingY * iTickMarkYCnt + D_TEXT_MARGIN;
                LITickMark.Stroke = SCBMainStroke;
                LITickMark.StrokeThickness = D_TICK_MARK_STROKE_WIDTH;
                CVSAxis.Children.Add(LITickMark);

                //Grid
                Line LIGrid = new Line();
                LIGrid.X1 = dOriginX;
                LIGrid.X2 = dOriginX + dAxisXLength;
                LIGrid.Y1 = LIGrid.Y2 = LITickMark.Y1;                                              //Reuse the Y-Position of the tickmark
                LIGrid.Stroke = SCBGridStroke;
                LIGrid.StrokeThickness = D_GRID_STROKE_WIDTH;
                CVSAxis.Children.Add(LIGrid);

                //Label
                if (0 == (iTickMarkYCnt % I_MODULO_MAIN_LABELS_Y))                                  //Check if a lable should be placed
                {
                    TextBlock TBLLabel = new TextBlock();
                    TBLLabel.FontSize = I_FONT_SIZE_LABEL;
                    TBLLabel.Foreground = SCBText;
                    TBLLabel.Text = (iAxisYMaxValue - iTickMarkYCnt * dLabelStep).ToString();
                    TBLLabel.TextAlignment = TextAlignment.Right;
                    Canvas.SetTop(TBLLabel, dTickMarkSpacingY * iTickMarkYCnt - SZTBLAxisYLabel.Height / 2 + D_TEXT_MARGIN);
                    Canvas.SetRight(TBLLabel, CVSAxis.ActualWidth - dOriginX + D_AXIS_TICK_MARK_SIZE + D_LABEL_MARGIN);
                    CVSAxis.Children.Add(TBLLabel);
                }
            }

            //X-Axis
            //X-Axis title
            TextBlock TBLAxisXTitle = new TextBlock();                                              //TextBlock for Y-Axis
            TBLAxisXTitle.FontSize = I_FONT_SIZE_AXIS_TITLE;
            TBLAxisXTitle.Foreground = SCBText;
            TBLAxisXTitle.Text = "$_Time$";
            Size SZTBLAxisXLabel = LHStringFunctions.SZMeasureString(TBLAxisXTitle);                                                     //Measure the size of the textbox
            Canvas.SetTop(TBLAxisXTitle, dOriginY + D_AXIS_TICK_MARK_SIZE + D_LABEL_MARGIN + SZTBLAxisXLabel.Height + D_TEXT_MARGIN);    //Set X-Position of the Textblock
            Canvas.SetLeft(TBLAxisXTitle, dOriginX + dAxisXLength / 2 - SZTBLAxisXLabel.Width / 2);                                      //Center the X-Axis title
            CVSAxis.Children.Add(TBLAxisXTitle);

            //X-Axis line
            Line LIAxisX = new Line();
            LIAxisX.X1 = dOriginX; LIAxisX.X2 = dAxisXLength + dOriginX;                            //Set X-Positions of the X-Axis
            LIAxisX.Y1 = LIAxisX.Y2 = dOriginY;                                                     //Set length the X-Axis
            LIAxisX.Stroke = SCBMainStroke;
            LIAxisX.StrokeThickness = D_AXIS_STROKE_WIDTH;
            CVSAxis.Children.Add(LIAxisX);

            //X-Axis tickmarks
            double dTickMarkSpacingX = dAxisXLength / I_NUM_OF_MAIN_TICK_MARKS_X;                   //Spacing of the X-Tickmarks
            double dTickMarkXPosY = dOriginY;                                                       //Y-Position of the tickmarks

            for (int iTickMarkXCnt = 1; iTickMarkXCnt <= I_NUM_OF_MAIN_TICK_MARKS_X; iTickMarkXCnt++)
            {
                Line LITickMark = new Line();
                LITickMark.X1 = LITickMark.X2 = dOriginX + dTickMarkSpacingX * iTickMarkXCnt;
                LITickMark.Y1 = dTickMarkXPosY; LITickMark.Y2 = dTickMarkXPosY + D_AXIS_TICK_MARK_SIZE;
                LITickMark.Stroke = SCBMainStroke;
                LITickMark.StrokeThickness = D_TICK_MARK_STROKE_WIDTH;
                CVSAxis.Children.Add(LITickMark);

                //Grid
                Line LIGrid = new Line();
                LIGrid.X1 = LIGrid.X2 = LITickMark.X1;                                              //Reuse the X-Position of the tickmark
                LIGrid.Y1 = dOriginY;
                LIGrid.Y2 = dOriginY - dAxisYLength;
                LIGrid.Stroke = SCBGridStroke;
                LIGrid.StrokeThickness = D_GRID_STROKE_WIDTH;
                CVSAxis.Children.Add(LIGrid);

                //Label
                if (0 == (iTickMarkXCnt % I_MODULO_MAIN_LABELS_X))                                  //Check if a lable should be placed
                {
                    TextBlock TBLLabel = new TextBlock();
                    TBLLabel.FontSize = I_FONT_SIZE_LABEL;
                    TBLLabel.Foreground = SCBText;
                    TBLLabel.Text = asAxisXLabels[iTickMarkXCnt / I_MODULO_MAIN_LABELS_X - 1];
                    TBLLabel.TextAlignment = TextAlignment.Right;
                    Canvas.SetTop(TBLLabel, dOriginY + D_AXIS_TICK_MARK_SIZE + D_LABEL_MARGIN);
                    Canvas.SetLeft(TBLLabel, dOriginX + dTickMarkSpacingX * iTickMarkXCnt - LHStringFunctions.SZMeasureString(TBLLabel).Width / 2);
                    CVSAxis.Children.Add(TBLLabel);
                }
            }

        }

        //This function draws an entire series
        private void vDrawSeries(Class_Series qSeries)
        {
            if (qSeries.SLDataPoints.Count < 2)                                                     //Draw only if there are at least 2 datapoints
            {
                return;
            }
            qSeries.iSLReadIndex = 0;
            qSeries.polyline.Points.Clear();
            double dX, dY;
            foreach (KeyValuePair<int, double> actDataPoint in qSeries.SLDataPoints)
            {
                dX = dOriginX + actDataPoint.Key * dPixelsPerSecond;
                if (dX < (dOriginX + dAxisXLength))
                {
                    if (actDataPoint.Value > iAxisYMaxValue)                                        //Maximum value exceeds the X-Axis
                    {
                        qSeries.polyline.Points.Add(new Point(dX, dOriginY - dAxisYLength));
                    }
                    else                                                                            //Maximum value is within the X-Axis range
                    {
                        dY = dOriginY - actDataPoint.Value * dPixelsPerValue;
                        qSeries.polyline.Points.Add(new Point(dX, dY));
                    }
                }
            }
            qSeries.iSLReadIndex = qSeries.SLDataPoints.Last().Key;
        }

        //This function increments the max time of the X-Axis and calculates the neccessary values
        private void vIncrementMaxTime()
        {
            iAxisXMaxValue += I_TIME_INCREMENT;                                                     //Increment the maximum time

            //Calculate the strings for the X-Axis labels
            int iMaxMinutes = iAxisXMaxValue / 60;
            int iLabelStepMinutes = iMaxMinutes / (I_NUM_OF_MAIN_TICK_MARKS_X / I_MODULO_MAIN_LABELS_X);
            int iHours, iMinutes;
            for (int iLabelCnt = 1; iLabelCnt <= (I_NUM_OF_MAIN_TICK_MARKS_X / I_MODULO_MAIN_LABELS_X); iLabelCnt++)
            {
                iHours = (iLabelCnt * iLabelStepMinutes / 60);
                iMinutes = (iLabelCnt * iLabelStepMinutes) - 60 * iHours;
                asAxisXLabels[iLabelCnt - 1] = iHours.ToString();
                asAxisXLabels[iLabelCnt - 1] += ":";
                asAxisXLabels[iLabelCnt - 1] += iMinutes.ToString("00");
            }
            dPixelsPerSecond = dAxisXLength / (iAxisXMaxValue);
        }

        //This function calculates the maximum Y-Axis value using the max value of all series
        public void vRescaleY()
        {
            int iPowCnt = 0;                                                                        //Counts the power of 10 of the max value
            double dMaxSeriesValueTemp = dMaxSeriesValue;
            while (dMaxSeriesValueTemp >= 1)
            {
                dMaxSeriesValueTemp /= 10;
                iPowCnt++;
            }
            iAxisYMaxValue = (int)Math.Pow(10, iPowCnt - 1);                                        //Calculate the step size for increase decrease
            dAxisYIncrementValue = dAxisYDecrementValue = iAxisYMaxValue;                           //Save the value for zoom step
            iAxisYMaxValueFirstDigit = (1 + (int)(dMaxSeriesValue / iAxisYMaxValue));               //Save the first digit of the new max value
            iAxisYMaxValue *= iAxisYMaxValueFirstDigit;                                             //Calculate the new max value

            if (iAxisYMaxValueFirstDigit == 10)
            {
                iAxisYMaxValueFirstDigit = 1;
                dAxisYIncrementValue *= 10;
            }
            vDrawAll();
        }

        //This function sets the color theme of the chart
        public void vSetColorTheme(ResourceDictionary qRDTheme)
        {
            canvas.Background = (SolidColorBrush)qRDTheme["Col_UC_LineChartBackground"];            //Background of the canvas
            SCBGridStroke = (SolidColorBrush)qRDTheme["Col_UC_LineChartGridStroke"];                //Main stroke
            SCBMainStroke = (SolidColorBrush)qRDTheme["Col_UC_LineChartMainStroke"];                //Main stroke
            SCBText = (SolidColorBrush)qRDTheme["Col_UC_LineChartText"];                            //TextColor
            //Series colors
            String sThemeID = (String)qRDTheme["Str_ID"];
            if ("Light" == sThemeID)
            {
                RDSeriesColors.Source = new Uri("pack://application:,,,/LHWpfControlLibrary;Component/Styles/UC_LineChart/THM_LineChartSeries.xaml", UriKind.Absolute);
            }
            else if ("Dark" == sThemeID)
            {
                RDSeriesColors.Source = new Uri("pack://application:,,,/LHWpfControlLibrary;Component/Styles/UC_LineChart/THM_LineChartSeries.Night.xaml", UriKind.Absolute);
            }

        }

        //This function calls the update mechanism of all series
        public void vUpdateAllSeries()
        {
            bool  bRescaleY = false;
            double dX, dY;
            int iLastIndex, iMaxTimeValue;
            foreach (Class_Series series in SDSeries.Values)
            {
                iLastIndex = series.SLDataPoints.Count()-1;
                iMaxTimeValue = series.SLDataPoints.Keys[iLastIndex];                               //Maximum time value of the list
                if (iMaxTimeValue > iAxisXMaxValue)                                                 //X-Axis needs to be resized -> redraw entire chart
                {
                    while (iMaxTimeValue > iAxisXMaxValue)
                    {
                        vIncrementMaxTime();                                                        //Recalculate the maximum time of the X-Axis
                    }
                    vDrawAll();
                    return;
                }
                else if(series.iSLReadIndex>=series.SLDataPoints.Count)
                {
                    vDrawAll();
                    return;
                }
                else                                                                                //X-Axis needs no resize -> only add new points to the polyline and dont redraw
                {
                    int iTimeStamp;
                    double dValue;
                    while (iLastIndex != series.iSLReadIndex)
                    {
                        series.iSLReadIndex++;
                        iTimeStamp = series.SLDataPoints.Keys[series.iSLReadIndex];
                        dValue= series.SLDataPoints.Values[series.iSLReadIndex];
                        if (series.SLDataPoints.ContainsKey(series.iSLReadIndex))
                        {
                            dX = dOriginX + iTimeStamp* dPixelsPerSecond;
                            if (dX < (dOriginX + dAxisXLength))
                            {
                                if (dValue > iAxisYMaxValue)                                        //Maximum value exceeds the Y-Axis
                                {
                                    series.polyline.Points.Add(new Point(dX, dOriginY - dAxisYLength));
                                }
                                else                                                                //Maximum value is within the Y-Axis range
                                {
                                    dY = dOriginY - dValue * dPixelsPerValue;
                                    series.polyline.Points.Add(new Point(dX, dY));
                                }
                            }
                            //Check if the new valueis the new biggest value
                            if (dValue > dMaxSeriesValue)
                            {
                                dMaxSeriesValue = dValue;
                                if (dMaxSeriesValue > iAxisYMaxValue)
                                {
                                    bRescaleY = true;                                               //Indicator for rescaling after all points were added
                                }
                            }
                        }
                    }
                    if(bRescaleY)
                    {
                        vRescaleY();
                    }
                }
            }
        }

        /***********************************************************************************************
        * 
        * Classes
        * 
        **********************************************************************************************/
        //This class contains the data of a line series
        public class Class_Series
        {
            //Constants
            //Objects
            public Canvas canvas;                                                                   //A canvas to draw the series to
            public Polyline polyline;                                                               //The line of the series
            public SortedList<int, double> SLDataPoints;                                            //List contains all datapoints
            public SolidColorBrush SCBStroke;                                                       //Stroke of the series
            public UC_CheckBoxFilled uC_CheckBoxFilled;                                             //The checkbox for the series
            //Primitive
            public int iSLReadIndex = 0;                                                            //The index of the last added point from SLDataPoint to the chart
            public String sSeriesName;                                                              //Name of the series. Is shown in the legend

            //Constructor
            public Class_Series(String qsSeriesName, SortedList<int, double> qSLDataPoints)
            {
                canvas = new Canvas();
                sSeriesName = qsSeriesName;
                SLDataPoints = qSLDataPoints;                                                       //Set the SDDataPoints of the series (SDDataPoints is an object -> passed by reference)
                //Initialize the checkbox
                uC_CheckBoxFilled = new UC_CheckBoxFilled();
                uC_CheckBoxFilled.Margin = new Thickness(I_MARGIN_CHECKBOX_HORIZONTAL, I_MARGIN_CHECKBOX_VERTICAL, I_MARGIN_CHECKBOX_HORIZONTAL, I_MARGIN_CHECKBOX_VERTICAL);
                uC_CheckBoxFilled.Text = sSeriesName;
                uC_CheckBoxFilled.bIsChecked = true;
                uC_CheckBoxFilled.EHCheckedChanged += CBF_CheckedChanged;
                uC_CheckBoxFilled.FontSize = D_FONSTIZE_CHECKBOX;

                //Initialize the polyline
                polyline = new Polyline();
                polyline.StrokeThickness = 2;
                polyline.StrokeLineJoin = PenLineJoin.Round;
                canvas.Children.Add(polyline);
            }

            //Events
            //This event is triggered if the checkbox checked state has changed
            private void CBF_CheckedChanged(object sender, EventArgs e)
            {
                if (uC_CheckBoxFilled.bIsChecked)
                {
                    canvas.Visibility = Visibility.Visible;
                }
                else
                {
                    canvas.Visibility = Visibility.Hidden;
                }
            }

            //Functions
            //This function sets the color of the series
            public void vSetColor(SolidColorBrush SCBColor)
            {
                SCBStroke = SCBColor.Clone();
                uC_CheckBoxFilled.vSetCheckedColor(SCBStroke);
                polyline.Stroke = SCBStroke;
            }


        }

    }
}
