using LHCommonFunctions.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace LHWpfControlLibrary.Source.Windows
{
    /// <summary>
    /// Interaktionslogik für Window_ProgressBox.xaml
    /// </summary>
    public partial class Window_ProgressBox : Window
    {
        /***********************************************************************************************
        * 
        * Variables
        * 
        **********************************************************************************************/

        Window_ProgressBoxVM window_ProgressBoxVM;                                                  //ViewModel of the window


        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public Window_ProgressBox(Window_ProgressBoxVM qWindow_ProgressBoxVM, ResourceDictionary qRDTheme)
        {
            //Imitialize the window
            InitializeComponent();
            DataContext = qWindow_ProgressBoxVM;
            window_ProgressBoxVM = qWindow_ProgressBoxVM;
            window_ProgressBoxVM.EHOnRequestClose += Window_ProgressBoxVM_EHOnRequestClose;

            this.Resources.MergedDictionaries.Add(qRDTheme);                                        //Set the GUI theme
        }



        /***********************************************************************************************
        * 
        * Events
        * 
        **********************************************************************************************/
        //This function is caled if the window is fully loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            window_ProgressBoxVM.bIsLoaded = true;
        }

        //This event is called if the event hanlder of the viewmodel fired an event
        private void Window_ProgressBoxVM_EHOnRequestClose(object sender, EventArgs e)
        {
            this.Close();
        }


     

    }
}
