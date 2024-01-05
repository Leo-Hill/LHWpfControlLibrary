using LHCommonFunctions.Source;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TestSuite.Data;
using TestSuite.Windows;

namespace TestSuite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LHTraceFunctions.vTraceLine("Application startup");

            //General application settings
            Class_Settings.LoadApplicationSettings();                                                                                            //Load the application settings

            Window_Main window_Main = new Window_Main();
            window_Main.Show();
        }
    }
}
