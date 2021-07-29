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
        private const double D_AXIS_TICK_MARK_LENGTH = 4;                                           //Length of the tickmark
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
        private DispatcherTimer TIMResize;                                                          //This timer starts when the control is resized. It detects if the resize is finished by checking a timeout. Prevents multiple redrawing while resizing
        ResourceDictionary RDTheme;                                                                 //Color theme
        public List<Class_Series> LSeries;
        private SolidColorBrush SCBGridStroke, SCBMainStroke, SCBText;                              //Colors of the chart

        //Primitive
        private bool bAutoScaleMode = true;                                                         //Auto scale mode
        private double dAxisXLength, dAxisYLength;                                                  //Length of X and Y Axes
        private double dOriginX, dOriginY;                                                          //Position of the origin
        private double dPixelsPerSecond, dPixelsPerValue;                                           //Number of pixels per second on the X-Axis and pixel per value on the Y-Axis
        private int iAxisYZoomIncrementValue, iAxisYZoomDecrementValue;                          //Values for decrement and increment the Y-Axis max value on user zoom scaling
        private int iAxisXMaxValue, iAxisYMaxValue;                                                 //Values for the axes maximums. 
        private int iAxisYMaxValueFirstDigit;                                                       //First digit of the Y axis maximum. Used for calculating the amount of zooming on the next user scale event

        private String[] asAxisXLabels = new String[I_NUM_OF_MAIN_TICK_MARKS_X / I_MODULO_MAIN_LABELS_X];    //Labels for the X-Axis
        public String sAxisXTitle = "{TIME}", sAxisYTitle = "";
        /***********************************************************************************************
        * 
        * Construtor
        * 
        **********************************************************************************************/
        public UC_LineChart()
        {
            InitializeComponent();

            //Initialize variables
            //Objects
            CVSAxis = new Canvas();
            CVSSeries = new Canvas();

            //Adding the canvases to the main canvas
            canvas.Children.Add(CVSAxis);
            canvas.Children.Add(CVSSeries);

            RDTheme = new ResourceDictionary();
            LSeries = new List<Class_Series>();

            //Timer
            TIMResize = new DispatcherTimer();
            TIMResize.Interval = TimeSpan.FromMilliseconds(I_INTERVAL_RESIZE_TIMER);
            TIMResize.Tick += TIMResize_Tick;

            vReset();
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
            if (e.Delta > 0 && iAxisYMaxValue > 1)
            {
                bAutoScaleMode = false;                                                             //Disable auto scale mode
                iAxisYMaxValue -= iAxisYZoomDecrementValue;
                iAxisYMaxValueFirstDigit--;
                if (iAxisYMaxValueFirstDigit == 1)                                                  //Transition from 2 to 1 -> decrement value / 10
                {
                    iAxisYZoomDecrementValue /= 10;
                }
                else if (iAxisYMaxValueFirstDigit == 0)                                             //Transition from 1 to 0 (10 to 9) -> increment value / 10 ( = decrement value)
                {
                    iAxisYMaxValueFirstDigit = 9;
                    iAxisYZoomIncrementValue = iAxisYZoomDecrementValue;
                }
            }
            else if (e.Delta < 0)
            {
                bAutoScaleMode = false;                                                             //Disable auto scale mode
                iAxisYMaxValue += iAxisYZoomIncrementValue;                                    //Increment the max value
                iAxisYMaxValueFirstDigit++;                                                         //Increment the first digit 
                if (iAxisYMaxValueFirstDigit == 10)                                                 //Transition from 9 to 10 -> increment value * 10
                {
                    iAxisYMaxValueFirstDigit = 1;
                    iAxisYZoomIncrementValue *= 10;
                }
                else if (iAxisYMaxValueFirstDigit == 2)                                             //Transition from 1 to 2 -> Decrement value * 10 ( = increment value)  
                {
                    iAxisYZoomDecrementValue = iAxisYZoomIncrementValue;
                }
            }
            vDrawAll();                                                                             //Redraw entire chart
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
            vDrawAll();                                                                             //Redraw entire chart
        }

        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function inserts a new series to the chart. With the qiSeriesId an existing series can be replaced
        public void vAddNewSeries(String qsSeriesName, SortedList<int, float> qSLDataPoints, int qiSeriesId = 1000)
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

            //Find the max value if the series
            foreach (double dValue in qSLDataPoints.Values)
            {
                if (true == double.IsFinite(dValue) && dValue > addingSeries.dMaxValue)
                {
                    addingSeries.dMaxValue = dValue;
                }
            }

            if (LSeries.Count < qiSeriesId)                                                         //Check if series should be added at the end or replaces an existing one
            {
                CVSSeries.Children.Add(addingSeries.canvas);                                        //Add the series canvas to the canvas of series
                LSeries.Add(addingSeries);                                                          //Add series to the dictionary of series
                WPLegend.Children.Add(addingSeries.uC_CheckBoxFilled);                              //Add the ckeckbox control

                addingSeries.vSetColor((SolidColorBrush)RDTheme[$"Col_{LSeries.Count - 1}"]);       //Set the color of the series
            }
            else
            {
                CVSSeries.Children.RemoveAt(qiSeriesId);                                            //Remove the existing series canvas from the canvas of series
                LSeries[qiSeriesId].EHVisibilityChanged = null;                                         //Clean up the event handler
                LSeries.RemoveAt(qiSeriesId);                                                       //Remove the existing series
                WPLegend.Children.RemoveAt(qiSeriesId);                                             //Remove the existing chkeckbox control

                CVSSeries.Children.Insert(qiSeriesId, addingSeries.canvas);                         //Add the new series canvas to the canvas of series
                LSeries.Insert(qiSeriesId, addingSeries);                                           //Add the new series to the dictionary of series
                WPLegend.Children.Insert(qiSeriesId, addingSeries.uC_CheckBoxFilled);               //Add the new ckeckbox control

                addingSeries.vSetColor((SolidColorBrush)RDTheme[$"Col_{qiSeriesId}"]);              //Set the color of the series
            }
            addingSeries.EHVisibilityChanged += new EventHandler(OnSeriesVisibilityChanged); //Suscribe to the visibility changed event

            if (true == bAutoScaleMode)
            {
                vRescaleY();
            }
        }

        //This function redraws the entire chart
        private void vDrawAll()
        {
            //Axis
            vDrawAxes();

            //Series
            CVSSeries.Visibility = Visibility.Visible;                                              //Show all series
            foreach (Class_Series class_Series in LSeries)
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
            TBLAxisYTitle.Text = sAxisYTitle;
            Size SZTBLAxisYTitle = LHStringFunctions.SZMeasureString(TBLAxisYTitle);                //Measure the size of the Y-Axis tile textbox

            TextBlock TBLAxisXTitle = new TextBlock();                                              //TextBlock for X-Axis
            TBLAxisXTitle.FontSize = I_FONT_SIZE_AXIS_TITLE;
            TBLAxisXTitle.Foreground = SCBText;
            TBLAxisXTitle.Text = sAxisXTitle;
            Size SZTBLAxisXTitle = LHStringFunctions.SZMeasureString(TBLAxisXTitle);                //Measure the size of the X-Axis tile textbox

            TextBlock TBLAxisYLabel = new TextBlock();                                              //Biggest tickmark of the Y-Axis
            TBLAxisYLabel.FontSize = I_FONT_SIZE_LABEL;
            TBLAxisYLabel.Foreground = SCBText;
            TBLAxisYLabel.Text = iAxisYMaxValue.ToString();
            Size SZTBLAxisYLabel = LHStringFunctions.SZMeasureString(TBLAxisYLabel);                //Measure the size of the 

            //Calculate the origin of the chart
            dOriginY = canvas.ActualHeight - D_TEXT_MARGIN - SZTBLAxisXTitle.Height - D_TEXT_MARGIN - SZTBLAxisYLabel.Height - D_LABEL_MARGIN - D_AXIS_TICK_MARK_LENGTH;    //OriginY is constraint by the bottom
            dOriginX = D_TEXT_MARGIN + SZTBLAxisYTitle.Height + D_TEXT_MARGIN + SZTBLAxisYLabel.Width + D_LABEL_MARGIN + D_AXIS_TICK_MARK_LENGTH;                           //OriginX is constraint by the left
            dAxisYLength = dOriginY - D_TEXT_MARGIN;                                                                                                                        //Calculate the size of the Y-Axis
            dAxisXLength = canvas.ActualWidth - dOriginX;                                                                                                                   //Calculate the size of the X-Axis
            dPixelsPerSecond = dAxisXLength / (iAxisXMaxValue);                                                                                                             //Calculate pixels per second for setting the series points
            dPixelsPerValue = dAxisYLength / iAxisYMaxValue;                                                                                                                //Calculate pixels per value for setting the series points

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
                LITickMark.X1 = dTickMarkYPosX; LITickMark.X2 = dTickMarkYPosX - D_AXIS_TICK_MARK_LENGTH;
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
                    TBLLabel.Text = (iAxisYMaxValue - iTickMarkYCnt * dLabelStep).ToString("0.##");
                    TBLLabel.TextAlignment = TextAlignment.Right;
                    Canvas.SetTop(TBLLabel, dTickMarkSpacingY * iTickMarkYCnt - SZTBLAxisYLabel.Height / 2 + D_TEXT_MARGIN);
                    Canvas.SetRight(TBLLabel, CVSAxis.ActualWidth - dOriginX + D_AXIS_TICK_MARK_LENGTH + D_LABEL_MARGIN);
                    CVSAxis.Children.Add(TBLLabel);
                }
            }

            //X-Axis
            //X-Axis title
            Canvas.SetTop(TBLAxisXTitle, dOriginY + D_AXIS_TICK_MARK_LENGTH + D_LABEL_MARGIN + SZTBLAxisXTitle.Height + D_TEXT_MARGIN);    //Set X-Position of the Textblock
            Canvas.SetLeft(TBLAxisXTitle, dOriginX + dAxisXLength / 2 - SZTBLAxisXTitle.Width / 2);                                        //Center the X-Axis title
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
                LITickMark.Y1 = dTickMarkXPosY; LITickMark.Y2 = dTickMarkXPosY + D_AXIS_TICK_MARK_LENGTH;
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
                    Canvas.SetTop(TBLLabel, dOriginY + D_AXIS_TICK_MARK_LENGTH + D_LABEL_MARGIN);
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
            foreach (KeyValuePair<int, float> actDataPoint in qSeries.SLDataPoints)
            {
                dX = dOriginX + actDataPoint.Key * dPixelsPerSecond;
                if (dX < (dOriginX + dAxisXLength)) //Check if value is in X-Axis range
                {
                    if (actDataPoint.Value > iAxisYMaxValue)                                        //Value exceeds the Y-Axis positive bounds
                    {
                        qSeries.polyline.Points.Add(new Point(dX, dOriginY - dAxisYLength));
                    }
                    else if (actDataPoint.Value < 0)                                        //Value exceeds the Y-Axis negative bounds
                    {
                        qSeries.polyline.Points.Add(new Point(dX, dOriginY));
                    }
                    else                                                                            //Value is within the Y-Axis range
                    {
                        dY = dOriginY - actDataPoint.Value * dPixelsPerValue;
                        qSeries.polyline.Points.Add(new Point(dX, dY));
                    }
                }
            }
            qSeries.iSLReadIndex = qSeries.SLDataPoints.Count;
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

        //This event is called if the visibility of a series has changed
        private void OnSeriesVisibilityChanged(object sender, EventArgs e)
        {
            vRescaleY(); //Rescale the y axis
        }

        //This function calculates the maximum Y-Axis value using the max value of all series
        public void vRescaleY()
        {
            double dMaxSeriesValue = 0;                                                             //Maximum value of all series

            //Find the max value
            foreach (Class_Series series in LSeries)
            {
                if (Visibility.Visible == series.canvas.Visibility && series.dMaxValue > dMaxSeriesValue)
                {
                    dMaxSeriesValue = series.dMaxValue; //Set the new max value
                }
            }

            if (dMaxSeriesValue > 0)
            {
                double dMaxSeriesValueTemp = dMaxSeriesValue;
                iAxisYZoomIncrementValue = iAxisYZoomDecrementValue = (int)Math.Pow(10, Math.Floor(Math.Log10(dMaxSeriesValue)));    //Calculate the incremet values for zooming
                iAxisYMaxValueFirstDigit = (1 + (int)(dMaxSeriesValue / iAxisYZoomIncrementValue));           //Save the first digit of the new max value
                iAxisYMaxValue = iAxisYZoomIncrementValue * iAxisYMaxValueFirstDigit;                                         //Calculate the new max value
                if (iAxisYMaxValueFirstDigit == 10)                                                 //Hanle calculation overflow and set the next decade as first digit and zoom factor
                {
                    iAxisYMaxValueFirstDigit = 1;
                    iAxisYZoomIncrementValue *= 10;
                }
                vDrawAll();                                                                         //Redraw entire chart
            }
        }

        //This function resets the entire line chart
        public void vReset()
        {
            bAutoScaleMode = true;
            iAxisYZoomIncrementValue = 100;                                                         //Initial values for zooming
            iAxisYZoomDecrementValue = 10;                                                          //Initial values for zooming
            iAxisXMaxValue = 0;                                                                     //Initial values for the axes maximums
            iAxisYMaxValue = 100;                                                                   //Initial values for the axes maximums
            iAxisYMaxValueFirstDigit = 1;                                                           //First digit is 1 because iAxisYMaxValue = 100

            for (int iSeriesCnt = 0; iSeriesCnt < LSeries.Count; iSeriesCnt++)
            {
                LSeries[iSeriesCnt].vReset();                                                       //Reset the series
                LSeries[iSeriesCnt].vSetColor((SolidColorBrush)RDTheme[$"Col_{iSeriesCnt}"]);       //Reset the color of the series
            }
            vIncrementMaxTime();
            vDrawAxes();
        }

        //This function sets the color theme of the chart
        public void vSetColorTheme(ResourceDictionary qRDTheme)
        {
            RDTheme = qRDTheme;
            canvas.Background = (SolidColorBrush)RDTheme["Col_UC_LineChartBackground"];             //Background of the canvas
            SCBGridStroke = (SolidColorBrush)RDTheme["Col_UC_LineChartGridStroke"];                 //Main stroke
            SCBMainStroke = (SolidColorBrush)RDTheme["Col_UC_LineChartMainStroke"];                 //Main stroke
            SCBText = (SolidColorBrush)RDTheme["Col_UC_LineChartText"];                             //TextColor

            for (int iSeriesCnt = 0; iSeriesCnt < LSeries.Count; iSeriesCnt++)
            {
                LSeries[iSeriesCnt].vSetColor((SolidColorBrush)RDTheme[$"Col_{iSeriesCnt}"]);       //Set the color of the series
            }
            vDrawAll();
        }

        //This function calls the update mechanism of all series
        public void vUpdateAllSeries()
        {
            double dX, dY;
            int iLastIndex, iMaxTimeValue;
            foreach (Class_Series series in LSeries)
            {
                iLastIndex = series.SLDataPoints.Count() - 1;
                if (series.SLDataPoints.Count > 0)
                {
                    iMaxTimeValue = series.SLDataPoints.Keys[iLastIndex];                           //Maximum time value of the list
                    if (iMaxTimeValue > iAxisXMaxValue)                                             //X-Axis needs to be resized -> redraw entire chart
                    {
                        while (iMaxTimeValue > iAxisXMaxValue)
                        {
                            vIncrementMaxTime();                                                    //Recalculate the maximum time of the X-Axis
                        }
                        vDrawAll();                                                                 //Redraw entire chart
                        return;
                    }
                    else if (series.iSLReadIndex > series.SLDataPoints.Count)
                    {
                        vDrawAll();                                                                 //Redraw entire chart
                        return;
                    }
                    else                                                                            //X-Axis needs no resize -> only add new points to the polyline and dont redraw
                    {
                        bool bRescaleY = false;
                        int iTimeStamp;
                        double dValue;
                        while (iLastIndex >= series.iSLReadIndex)
                        {
                            iTimeStamp = series.SLDataPoints.Keys[series.iSLReadIndex];
                            dValue = series.SLDataPoints.Values[series.iSLReadIndex];
                            dX = dOriginX + iTimeStamp * dPixelsPerSecond;
                            if (dX < (dOriginX + dAxisXLength))
                            {
                                if (dValue > iAxisYMaxValue)                                        //Value exceeds the Y-Axis positive bounds
                                {
                                    series.polyline.Points.Add(new Point(dX, dOriginY - dAxisYLength));
                                }
                                else if (dValue < 0)                                        //Value exceeds the Y-Axis negative bounds
                                {
                                    series.polyline.Points.Add(new Point(dX, dOriginY));
                                }
                                else                                                                //Maximum value is within the Y-Axis range
                                {
                                    dY = dOriginY - dValue * dPixelsPerValue;
                                    series.polyline.Points.Add(new Point(dX, dY));
                                }
                            }
                            //Check if the new value is the new biggest value
                            if (true == double.IsFinite(dValue) && dValue > series.dMaxValue)
                            {
                                series.dMaxValue = dValue;
                                if (series.dMaxValue > iAxisYMaxValue)
                                {
                                    bRescaleY = true;                                               //Indicator for rescaling after all points were added
                                }
                            }
                            series.iSLReadIndex++;
                        }
                        if (true == bAutoScaleMode && true == bRescaleY)
                        {
                            vRescaleY();
                        }
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
            public EventHandler EHVisibilityChanged; //Event is triggered if the visibility of the series has changed
            public Polyline polyline;                                                               //The line of the series
            public SortedList<int, float> SLDataPoints;                                             //List contains all datapoints. Int is relative time in seconds
            public SolidColorBrush SCBStroke;                                                       //Stroke of the series
            public UC_CheckBoxFilled uC_CheckBoxFilled;                                             //The checkbox for the series
            //Primitive
            public double dMaxValue; //Maximum value of the series
            public int iSLReadIndex;                                                                //The index of the next unread value from the SLDatapoints
            public String sSeriesName;                                                              //Name of the series. Is shown in the legend

            //Constructor
            public Class_Series(String qsSeriesName, SortedList<int, float> qSLDataPoints)
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

                vReset();
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
                EHVisibilityChanged?.Invoke(null, EventArgs.Empty); //Call the visibility changed event
            }

            //Functions
            //This function resets the series
            public void vReset()
            {
                dMaxValue = 0;
                iSLReadIndex = 0;
                canvas.Children.Clear();
                //Create a new polyline
                polyline = new Polyline();
                polyline.StrokeThickness = 2;
                polyline.StrokeLineJoin = PenLineJoin.Round;
                canvas.Children.Add(polyline);
            }
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
