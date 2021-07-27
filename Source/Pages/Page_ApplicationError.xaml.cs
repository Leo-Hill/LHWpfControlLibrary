using LHCommonFunctions.Source;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LHWpfControlLibrary.Source.Pages
{
    /***********************************************************************************************
   * 
   * This page shows an application/exception error. Can be used for iserting in a Messagebox
   * 
   **********************************************************************************************/
    public partial class Page_ApplicationError : Page
    {
        public Page_ApplicationError(Exception qException, ResourceDictionary qRDLanguage, ResourceDictionary qRDTheme)
        {
            InitializeComponent();
            DataContext = this;

            //Set the language and the theme
            this.Resources.MergedDictionaries.Add(qRDLanguage);                       //Set the GUI language
            this.Resources.MergedDictionaries.Add(qRDTheme);                          //Set the GUI theme

            LExceptionDetails.Content = qException.ToString();
        }

       
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            Window parentWindow = Window.GetWindow(this);   //Get the parent window
            LHMiscFunctions.vCenterWindowOnScreen(parentWindow); //Center the window on screen
        }
    }
}
