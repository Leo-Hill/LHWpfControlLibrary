using LHCommonFunctions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LHWpfControlLibrary.Source.Windows
{

    public partial class Window_MessageBox : Window
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
        public MessageBoxResult messageBoxResult;                                                   //The result to return
        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        private Window_MessageBox()
        {
            InitializeComponent();
            this.Owner = Misc.GetActiveWindow();
        }

        /***********************************************************************************************
        * 
        * Events
        * 
        **********************************************************************************************/
        private void BPositiveButton_Click(object sender, RoutedEventArgs e)
        {
            messageBoxResult = MessageBoxResult.Yes;
            this.Close();
        }

        private void BNegativeButton_Click(object sender, RoutedEventArgs e)
        {
            messageBoxResult = MessageBoxResult.No;
            this.Close();
        }

        private void BNeutralButton_Click(object sender, RoutedEventArgs e)
        {
            messageBoxResult = MessageBoxResult.OK;
            this.Close();
        }

        private void BCancelButton_Click(object sender, RoutedEventArgs e)
        {
            messageBoxResult = MessageBoxResult.Cancel;
            this.Close();
        }

        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/

        //This function sets the messagebox controlls according to the MessageBoxType
        public void vSetMessageBoxType(MessageBoxButton qMessageBoxButton)
        {
            if (qMessageBoxButton == MessageBoxButton.YesNo)
            {
                BNeutralButton.Visibility = Visibility.Collapsed;
                BCancelButton.Visibility = Visibility.Collapsed;
            }
            else if (qMessageBoxButton == MessageBoxButton.OK)
            {
                BPositiveButton.Visibility = Visibility.Collapsed;
                BNegativeButton.Visibility = Visibility.Collapsed;
                BCancelButton.Visibility = Visibility.Collapsed;
            }
            else if (qMessageBoxButton == MessageBoxButton.OKCancel)
            {
                BPositiveButton.Visibility = Visibility.Collapsed;
                BNegativeButton.Visibility = Visibility.Collapsed;
            }
            else if (qMessageBoxButton == MessageBoxButton.YesNoCancel)
            {
                BNeutralButton.Visibility = Visibility.Collapsed;
            }
        }

        //This function shows the messagebox
        public static MessageBoxResult MBRShow(String qsTitle, String qsText, MessageBoxButton qMessageBoxButton, ResourceDictionary qRDTheme)
        {
            Window_MessageBox window_MessageBox = new Window_MessageBox();
            if (qRDTheme != null)
            {
                window_MessageBox.Resources.MergedDictionaries.Add(qRDTheme);                       //Set the GUI theme
            }
            window_MessageBox.LTitle.Content = qsTitle;
            window_MessageBox.LText.Content = qsText;
            window_MessageBox.vSetMessageBoxType(qMessageBoxButton);
            window_MessageBox.ShowDialog();
            return window_MessageBox.messageBoxResult;
        }

        //This function shows the messagebox with the content of a inserted page
        public static MessageBoxResult MBRShow(String qsTitle, String qsText, MessageBoxButton qMessageBoxButton, Page qInsertPage, ResourceDictionary qRDTheme)
        {
            Window_MessageBox window_MessageBox = new Window_MessageBox();
            if (qRDTheme != null)
            {
                window_MessageBox.Resources.MergedDictionaries.Add(qRDTheme);                       //Set the GUI theme
            }
            window_MessageBox.FInsert.Content = qInsertPage;
            window_MessageBox.LTitle.Content = qsTitle;
            window_MessageBox.LText.Content = qsText;
            window_MessageBox.vSetMessageBoxType(qMessageBoxButton);
            window_MessageBox.ShowDialog();
            return window_MessageBox.messageBoxResult;
        }


    }
}
