using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Windows.Data;
using System.Windows.Input;

namespace LHWpfControlLibrary.Source
{
    //This converter converts an routedcommand's keyboard shortcut to a text
    public class IVCRoutedCommandToInputGestureText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RoutedCommand routedCommand = value as RoutedCommand;
            if (routedCommand != null)
            {
                InputGestureCollection inputGestureCollection = routedCommand.InputGestures;
                if ((inputGestureCollection != null) && (inputGestureCollection.Count >= 1))
                {
                    for (int i = 0; i < inputGestureCollection.Count; i++)                          //Search for the first key gesture
                    {
                        KeyGesture keyGesture = ((IList)inputGestureCollection)[i] as KeyGesture;
                        if (keyGesture != null)
                        {
                            return " ("+keyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture) +")";
                        }
                    }
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    //This converter converts a datetime to text
    public class IVCDateTimeToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dateTime = (DateTime)value;                                                    //Input value is Datetime
            return dateTime.ToShortDateString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
