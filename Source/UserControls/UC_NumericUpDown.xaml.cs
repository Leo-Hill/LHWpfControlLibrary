using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace LHWpfControlLibrary.Source.UserControls
{

    public partial class UC_NumericUpDown : UserControl
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
        private Regex RXNoLetters;                                                                  //Regex that matches disallowed text
        //Primitive
        //Bindings
        private double _dCurrentNumber; public double dCurrentNumber
        {
            get { return _dCurrentNumber; }
            set { _dCurrentNumber = value; if (_dCurrentNumber < iMinValue) { _dCurrentNumber = iMinValue; } else if (_dCurrentNumber > iMaxValue) { _dCurrentNumber = iMaxValue; } vConvertNumberToTextBox(); }
        }                                                                                                                            //The current number in double format
        public double dIncrement { get; set; }                                                                                       //Increment and decrement value
        private int _iMaxValue; public int iMaxValue { get { return _iMaxValue; } set { _iMaxValue = value; vValidateInput(); } }    //Maximum value                                                        
        private int _iMinValue; public int iMinValue { get { return _iMinValue; } set { _iMinValue = value; vValidateInput(); } }    //Minimum value                                                      
        public int iNumOfDecimals { get; set; }                                                                                      //Number of decimals

        private String sNumerFormat;                                                                //The format to convert the number to text
        public String sText { get { return TBMain.Text; } }                                         //The text of the textbox

        /***********************************************************************************************
        * 
        * Construtor
        * 
        **********************************************************************************************/
        public UC_NumericUpDown()
        {
            InitializeComponent();
            DataContext = this;
            iNumOfDecimals = 0;                                                                     //Default value no decimals
            dIncrement = 1;                                                                         //Default increment is 1

            //Check if a decimal point is allowed
            if (iNumOfDecimals == 0)
            {
                if (iMinValue >= 0)
                {
                    RXNoLetters = new Regex("[^0-9]+");                                             //No decimal and minus alowed
                }
                else
                {
                    RXNoLetters = new Regex("[^0-9-]+");                                            //No decimal alowed
                }
                TBMain.Text = "0";
            }
            else
            {
                if (iMinValue >= 0)
                {
                    RXNoLetters = new Regex("[^0-9.,]+");                                           //Decimal but no minus alowed

                }
                else
                {
                    RXNoLetters = new Regex("[^0-9.,-]+");                                          //Decimal alowed
                }
            }
            _dCurrentNumber = iMinValue;                                                            //Set the initial value of the number
            sNumerFormat = "N" + iNumOfDecimals.ToString();                                         //String for formatting the number to the textbox
            vConvertNumberToTextBox();
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
            if (!bTextIsAllowed(e.Text))
            {
                e.Handled = true;
            }
        }

        //This event is called if the controll is fully loaded
        private void UCNumericUpDown_Loaded(object sender, RoutedEventArgs e)
        {
        }
        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function sets the textbox according to the current number
        private void vConvertNumberToTextBox()
        {
            TBMain.Text = _dCurrentNumber.ToString(sNumerFormat);
        }

        //This function decrements the number according to the increment step
        private void vDecrementNumber()
        {
            _dCurrentNumber -= dIncrement;
            if (_dCurrentNumber < iMinValue)
            {
                _dCurrentNumber = iMinValue;
            }
            vConvertNumberToTextBox();
            if (EHValueChanged != null)
            {
                EHValueChanged(this, EventArgs.Empty);
            }
        }

        //This function returns the current value as int
        public int iGetInt()
        {
            return (int)Math.Round(_dCurrentNumber);
        }

        //This function increments the number according to the increment step
        private void vIncrementNumber()
        {
            _dCurrentNumber += dIncrement;
            if (_dCurrentNumber > iMaxValue)
            {
                _dCurrentNumber = iMaxValue;
            }
            vConvertNumberToTextBox();
            if (EHValueChanged != null)
            {
                EHValueChanged(this, EventArgs.Empty);
            }
        }

        //This function returns true if the text is allowed in the textbox
        private bool bTextIsAllowed(string text)
        {
            return !RXNoLetters.IsMatch(text);
        }

        //This function finally checks if the input is valid. If not it resets the textbox text to the current number
        private void vValidateInput()
        {
            double dSaveValue = _dCurrentNumber;
            if (iNumOfDecimals == 0)                                                                //No decimals -> parsing int
            {
                try
                {
                    _dCurrentNumber = int.Parse(TBMain.Text);                                       //Try to convert to int
                    if (_dCurrentNumber < iMinValue)
                    {
                        _dCurrentNumber = iMinValue;
                    }
                    else if (_dCurrentNumber > iMaxValue)
                    {
                        _dCurrentNumber = iMaxValue;
                    }
                }
                catch
                {

                }
                finally
                {
                    vConvertNumberToTextBox();
                }
            }
            else                                                                                    //Decimals -> parsing double
            {
                try
                {
                    _dCurrentNumber = double.Parse(TBMain.Text);                                    //Try to convert to int
                    if (_dCurrentNumber < iMinValue)
                    {
                        _dCurrentNumber = iMinValue;
                    }
                    else if (_dCurrentNumber > iMaxValue)
                    {
                        _dCurrentNumber = iMaxValue;
                    }
                }
                catch
                {

                }
                finally
                {
                    vConvertNumberToTextBox();
                }
            }
            if (EHValueChanged != null && dSaveValue != _dCurrentNumber && this.IsLoaded)           //The event should be triggered if the value has changed and the control is fully loaded
            {
                EHValueChanged(this, EventArgs.Empty);
            }
        }
    }
}
