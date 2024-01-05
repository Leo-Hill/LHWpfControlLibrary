using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace LHWpfControlLibrary.Source.UserControls
{

    public partial class UC_TimeSetter : UserControl, INotifyPropertyChanged
    {
        /***********************************************************************************************
        * 
        * Constants
        * 
        **********************************************************************************************/

        /***********************************************************************************************
        * 
        * Variables
        * 
        **********************************************************************************************/
        //Objects
        Binding TextBoxBindingHours, TextBoxBindingMinutes, TextBoxBindingSeconds;                  //Bindings for the textboxes

        public DateTime ActDateTime                                                                 //Datetime to set
        {
            get
            {
                return (DateTime)GetValue(DPActDateTime);
            }
            set
            {
                SetValue(DPActDateTime, value);
            }
        }
        public static readonly DependencyProperty DPActDateTime = DependencyProperty.Register(nameof(ActDateTime), typeof(DateTime), typeof(UC_TimeSetter), new UIPropertyMetadata(null));

        private DateTime MaxDateTime;                                                               //Maximum datetime (calculated with iMaxTime)
        private DateTime MinDateTime;                                                               //Minimum datetime (calculated with iMinTime)

        public event PropertyChangedEventHandler PropertyChanged;
        private Regex RXNoLetters;                                                                  //Regex that matches disallowed text
        private TextBox TBLastSelected;

        //Primitive
        private bool bTextBoxesUnbound;                                                             //Flag indicates if a textbox was updated in code behind -> binding is lost and has to be reset
        public int iMaxTime                                                                         //Maximum time in seconds
        {
            get
            {
                return (int)GetValue(DPiMaxTime);
            }
            set
            {
                SetValue(DPiMaxTime, value);
            }
        }
        public static readonly DependencyProperty DPiMaxTime = DependencyProperty.Register(nameof(iMaxTime), typeof(int), typeof(UC_TimeSetter), new UIPropertyMetadata(null));
        public int iMinTime                                                                         //Minimum time in seconds
        {
            get
            {
                return (int)GetValue(DPiMinTime);
            }
            set
            {
                SetValue(DPiMinTime, value);
            }
        }
        public static readonly DependencyProperty DPiMinTime = DependencyProperty.Register(nameof(iMinTime), typeof(int), typeof(UC_TimeSetter), new UIPropertyMetadata(null));

        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public UC_TimeSetter()
        {
            InitializeComponent();

            TBLastSelected = TBSecons;
            RXNoLetters = new Regex("[^0-9]+");                                                     //Only numbers alowed

            //Intiialize bindings
            //This binding takes care, that the textbox is updated, if the ActDate is updated via binding
            IVCDateTimeToString iVCDateTimeToString = new IVCDateTimeToString();
            TextBoxBindingHours = new Binding();
            TextBoxBindingHours.Path = new PropertyPath(nameof(ActDateTime));
            TextBoxBindingHours.Converter = iVCDateTimeToString;
            TextBoxBindingHours.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            TextBoxBindingHours.Mode = BindingMode.OneWay;
            TextBoxBindingHours.ConverterParameter = "HH";

            iVCDateTimeToString = new IVCDateTimeToString();
            TextBoxBindingMinutes = new Binding();
            TextBoxBindingMinutes.Path = new PropertyPath(nameof(ActDateTime));
            TextBoxBindingMinutes.Converter = iVCDateTimeToString;
            TextBoxBindingMinutes.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            TextBoxBindingMinutes.Mode = BindingMode.OneWay;
            TextBoxBindingMinutes.ConverterParameter = "MM";

            iVCDateTimeToString = new IVCDateTimeToString();
            TextBoxBindingSeconds = new Binding();
            TextBoxBindingSeconds.Path = new PropertyPath(nameof(ActDateTime));
            TextBoxBindingSeconds.Converter = iVCDateTimeToString;
            TextBoxBindingSeconds.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            TextBoxBindingSeconds.Mode = BindingMode.OneWay;
            TextBoxBindingSeconds.ConverterParameter = "SS";

            vSetTextBoxBindings();
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

        //This event is called if the down on the up button is focused
        private void B_GotFocus(object sender, RoutedEventArgs e)
        {
            TBLastSelected.Focus();                                                                 //Select entire text of the last selected textbox
            TBLastSelected.SelectAll();                                                             //Select entire text of the last selected textbox
        }

        //This event is called if the mouse is down on the up button
        private void BUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TBLastSelected == TBHours)
            {
                ActDateTime = ActDateTime.Add(new TimeSpan(1, 0, 0));
            }
            else if (TBLastSelected == TBMinutes)
            {
                ActDateTime = ActDateTime.Add(new TimeSpan(0, 1, 0));
            }
            else if (TBLastSelected == TBSecons)
            {
                ActDateTime = ActDateTime.Add(new TimeSpan(0, 0, 1));
            }
            vRemoveDateFromTime();
            TBLastSelected.SelectAll();                                                             //Select entire text of the textbox
        }

        //This event is called if the mouse is down on the down button
        private void BDown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {

                if (TBLastSelected == TBHours)
                {
                    ActDateTime = ActDateTime.Subtract(new TimeSpan(1, 0, 0));
                }
                else if (TBLastSelected == TBMinutes)
                {
                    ActDateTime = ActDateTime.Subtract(new TimeSpan(0, 1, 0));
                }
                else if (TBLastSelected == TBSecons)
                {
                    ActDateTime = ActDateTime.Subtract(new TimeSpan(0, 0, 1));
                }
            }
            catch
            {

            }
            vRemoveDateFromTime();
            TBLastSelected.SelectAll();                                                             //Select entire text of the textbox
        }

        //This event is called if a textbox got focus
        private void TBInput_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.SelectAll();
            TBLastSelected = textBox;
            e.Handled = true;
        }

        //This event is called if a textbox lost focus. A new datetime will be created
        private void TBHours_LostFocus(object sender, RoutedEventArgs e)
        {
            vDateTimeFromTextBoxes();
        }
        //This event is called if a textbox was clicked
        private void TBInput_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((TextBox)sender).SelectAll();
            ((TextBox)sender).Focus();
            e.Handled = true;
        }

        //This event is called if text was input to the textboxes. It handles unallowed chars and ensures that a correct time is input
        private void TBInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            char cMaxFirstDigit;

            if (RXNoLetters.IsMatch(e.Text))                                                        //Check if there is a non number character in the text
            {
                e.Handled = true;
                return;
            }

            //Set the maximum first digit of the time
            if (textBox == TBHours)
            {
                cMaxFirstDigit = '2';
            }
            else
            {
                cMaxFirstDigit = '5';
            }
            //Check if the textbox should be set to " x"
            if (
                (String.IsNullOrEmpty(textBox.Text)) ||                                             //Check if text was deleted and is empty
                (textBox.Text[0] != ' ' || textBox.Text[1] > cMaxFirstDigit) ||                     //Check if the first digit exceeds the maximum to limit 24/59
                (textBox == TBHours && textBox.Text[1] == '2' && e.Text[0] > '3')                   //Chek if the second hour digit exceeds 3
               )
            {
                textBox.Text = " " + e.Text;
                e.Handled = true;
            }
            //Check if the textbox should be set to "xx"
            else
            {
                textBox.Text = textBox.Text[1] + e.Text;
                e.Handled = true;
            }
            bTextBoxesUnbound = true;                                                               //Text was changed in code behind
            textBox.SelectAll();                                                                    //Select entire text
        }

        //This event is an event of the INotifyChanged interface
        public void OnPropertyChanged(String qSPropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(qSPropertyName));
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                BUp_MouseDown(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                BDown_MouseDown(null, null);
                e.Handled = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Min and max date time settings
            MinDateTime = new DateTime();
            MinDateTime = MinDateTime.AddSeconds((int)GetValue(DPiMinTime));
            if (DateTime.Compare(MinDateTime, ActDateTime) > 0)
            {
                ActDateTime = MinDateTime;
            }
            MaxDateTime = new DateTime();
            if (iMaxTime == 0)
            {
                MaxDateTime = DateTime.MaxValue;

            }
            else
            {
                MaxDateTime = MaxDateTime.AddSeconds((int)GetValue(DPiMaxTime));
            }

            vRemoveDateFromTime();
        }

        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function sets the date-time according to the text-boxes
        private void vDateTimeFromTextBoxes()
        {
            DateTime dateTime = new DateTime();
            dateTime = dateTime.AddHours(int.Parse(TBHours.Text)).AddMinutes(int.Parse(TBMinutes.Text)).AddSeconds(int.Parse(TBSecons.Text));
            if (DateTime.Compare(MinDateTime, dateTime) > 0)
            {
                dateTime = MinDateTime;
            }
            else if (DateTime.Compare(dateTime, MaxDateTime) > 0)
            {
                dateTime = MaxDateTime;
            }
            ActDateTime = dateTime;
            if (true == bTextBoxesUnbound)
            {
                vSetTextBoxBindings();
            }
        }

        //Set the binding from the datetime to the textboxes
        private void vSetTextBoxBindings()
        {
            TBHours.SetBinding(TextBox.TextProperty, TextBoxBindingHours);
            TBMinutes.SetBinding(TextBox.TextProperty, TextBoxBindingMinutes);
            TBSecons.SetBinding(TextBox.TextProperty, TextBoxBindingSeconds);
            bTextBoxesUnbound = false;
        }

        /***********************************************************************************************
        * 
        * Converter
        * 
        **********************************************************************************************/

        //This converter is used for updtaing a property if it is chagned in a window via binding. Dataflow from Viewmodel->Property
        public class IVCDateTimeToString : IValueConverter
        {
            String sMode;
            DateTime dateTime;

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                sMode = (String)parameter;
                dateTime = (DateTime)value;
                if (sMode == "HH")
                {
                    return dateTime.Hour.ToString("00");
                }
                else if (sMode == "MM")
                {
                    return dateTime.Minute.ToString("00");
                }
                else if (sMode == "SS")
                {
                    return dateTime.Second.ToString("00");
                }
                return "";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

        }

        //This function is used to ensure the date is always 01.01.0001
        void vRemoveDateFromTime()
        {
            if (ActDateTime.Year > 1)
            {
                ActDateTime = ActDateTime.AddYears(1-ActDateTime.Year);
            }
            if (ActDateTime.Month > 1)
            {
                ActDateTime = ActDateTime.AddMonths(1-ActDateTime.Month);
            }
            if (ActDateTime.Day > 1)
            {
                ActDateTime = ActDateTime.AddDays(1-ActDateTime.Day);
            }
        }

    }

}
