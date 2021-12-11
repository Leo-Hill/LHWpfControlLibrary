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

    public partial class UC_NumericUpDown : UserControl, INotifyPropertyChanged
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
        public event EventHandler EHValueChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private Regex RXNoLetters;                                                                  //Regex that matches disallowed text

        //Primitive
        public double dCurrentNumber                                                                //The current number in double format
        {
            get { return (double)GetValue(DPdCurrentNumber); }
            set
            {
                if (value < iMinValue)
                {
                    SetValue(DPdCurrentNumber, (double)iMinValue);
                }
                else if (value > iMaxValue)
                {
                    SetValue(DPdCurrentNumber, (double)iMaxValue);
                }
                else
                {
                    SetValue(DPdCurrentNumber, (double)value);
                }
            }
        }
        public static readonly DependencyProperty DPdCurrentNumber = DependencyProperty.Register(nameof(dCurrentNumber), typeof(double), typeof(UC_NumericUpDown), new UIPropertyMetadata(null));

        public double dIncrement
        {
            get
            {
                return (double)GetValue(DPdIncrement);
            }
            set
            {
                SetValue(DPdIncrement, (double)value);
            }
        }
        public static readonly DependencyProperty DPdIncrement = DependencyProperty.Register(nameof(dIncrement), typeof(double), typeof(UC_NumericUpDown), new UIPropertyMetadata((double)1));


        public int iMaxValue                                                                        //Maximum value of the NUD     
        {
            get
            {
                return (int)GetValue(DPiMaxValue);
            }
            set
            {
                SetValue(DPiMaxValue, (int)value);
                vValidateInput();
            }
        }
        public static readonly DependencyProperty DPiMaxValue = DependencyProperty.Register(nameof(iMaxValue), typeof(int), typeof(UC_NumericUpDown), new UIPropertyMetadata(null));

        public int iMinValue                                                                        //Minimum value of the NUD
        {
            get
            {
                return (int)GetValue(DPiMinValue);
            }
            set
            {
                SetValue(DPiMinValue, (int)value);
                vValidateInput();
            }
        }
        public static readonly DependencyProperty DPiMinValue = DependencyProperty.Register(nameof(iMinValue), typeof(int), typeof(UC_NumericUpDown), new UIPropertyMetadata(null));

        private int _iNumOfDecimals; public int iNumOfDecimals                                      //Number of decimals
        {
            get => _iNumOfDecimals;
            set
            {
                _iNumOfDecimals = value;
                vSetNumberFormat();
            }
        }

        private String sNumberFormat;                                                               //The format to convert the number to text
        public String sText { get { return TBMain.Text; } }                                         //The text of the textbox


        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public UC_NumericUpDown()
        {
            InitializeComponent();
            dCurrentNumber = 0;                                                                     //Set the initial value of the number
            vSetNumberFormat();
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
        //This event is called if the mouse is down on the up button
        private void BUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vIncrementNumber();
        }
        //This event is called if the mouse is down on the down button
        private void BDown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vDecrementNumber();
        }

        //This event is called if the textbox lost its focus
        private void TBMain_LostFocus(object sender, RoutedEventArgs e)
        {
            vValidateInput();
        }

        //This event is called if a key was pressed
        private void TBMain_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vValidateInput();
            }
            else if (e.Key == Key.Up)
            {
                vIncrementNumber();
            }
            else if (e.Key == Key.Down)
            {
                vDecrementNumber();
            }

        }

        //This event is called before the text of the textbox is changed
        private void TBMain_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (RXNoLetters.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        //This event is called if the controll is fully loaded
        private void UCNumericUpDown_Loaded(object sender, RoutedEventArgs e)
        {
            if (iMinValue > dCurrentNumber)
            {
                dCurrentNumber = iMinValue;
            }
        }

        //This event is an event of the INotifyChanged interface
        public void OnPropertyChanged(String qSPropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(qSPropertyName));
        }

        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function initializes the binding converter to enable according to the num of decimals
        public void vSetNumberFormat()
        {
            //Check if a decimal point is allowed
            if (iNumOfDecimals == 0)
            {
                sNumberFormat = "0";                                                                //String for formatting the number to the textbox

                if (iMinValue >= 0)
                {
                    RXNoLetters = new Regex("[^0-9]+");                                             //No decimal and minus alowed
                }
                else
                {
                    RXNoLetters = new Regex("[^0-9-]+");                                            //No decimal alowed
                }
            }
            else
            {
                sNumberFormat = "0.";                                                               //String for formatting the number to the textbox
                for (int iCnt = 0; iCnt < iNumOfDecimals; iCnt++)
                {
                    sNumberFormat += '#';
                }

                if (iMinValue >= 0)
                {
                    RXNoLetters = new Regex("[^0-9.,]+");                                           //Decimal but no minus alowed

                }
                else
                {
                    RXNoLetters = new Regex("[^0-9.,-]+");                                          //Decimal alowed
                }
            }

            //Bind the textbox text to the double value
            //This binding takes care, that the textbox is updated, if the dCurrentNumber is updated via binding
            IVCDoubleToString iVCDoubleToString = new IVCDoubleToString(sNumberFormat);
            Binding TextBoxBinding = new Binding();
            TextBoxBinding.Path = new PropertyPath(nameof(dCurrentNumber));
            TextBoxBinding.Converter = iVCDoubleToString;
            TextBoxBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            TextBoxBinding.Mode = BindingMode.OneWay;
            TBMain.SetBinding(TextBox.TextProperty, TextBoxBinding);

            OnPropertyChanged(nameof(dCurrentNumber));                                              //Trigger the converter
        }

        //This function decrements the number according to the increment step
        private void vDecrementNumber()
        {
            dCurrentNumber = dCurrentNumber - dIncrement;
            if (EHValueChanged != null)
            {
                EHValueChanged(this, EventArgs.Empty);
            }
        }

        //This function returns the current value as int
        public int iGetInt()
        {
            return (int)Math.Round(dCurrentNumber);
        }

        //This function increments the number according to the increment step
        private void vIncrementNumber()
        {
            dCurrentNumber = dCurrentNumber + dIncrement;
            if (EHValueChanged != null)
            {
                EHValueChanged(this, EventArgs.Empty);
            }
        }

        //This function finally checks if the input is valid. If not it resets the textbox text to the current number
        private void vValidateInput()
        {
            double dSaveValue = dCurrentNumber;

            try
            {
                if (iNumOfDecimals == 0)                                                            //No decimals -> parsing int
                {
                    dCurrentNumber = (double)int.Parse(TBMain.Text);                                //Try to convert to int
                }

                else                                                                                //Decimals -> parsing double
                {
                    dCurrentNumber = double.Parse(TBMain.Text);                                     //Try to convert to double
                }
            }
            catch
            {
            }
            if (EHValueChanged != null && dSaveValue != dCurrentNumber && this.IsLoaded)            //The event should be triggered if the value has changed and the control is fully loaded
            {
                EHValueChanged(this, EventArgs.Empty);
            }
        }


        /***********************************************************************************************
        * 
        * Converter
        * 
        **********************************************************************************************/
        //This converter is used for updtaing a property if it is chagned in a window via binding. Dataflow from Viewmodel->Property
        public class IVCDoubleToString : IValueConverter
        {
            String sNumberFormat;

            public IVCDoubleToString(String qsNumberFormat)
            {
                sNumberFormat = qsNumberFormat;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                double dInput = (double)value;
                return dInput.ToString(sNumberFormat);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

        }

    }


}
