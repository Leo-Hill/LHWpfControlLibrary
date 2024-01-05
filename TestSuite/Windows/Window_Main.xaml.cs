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
using TestSuite.Data;

namespace TestSuite.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Window_Main : Window
    {

        private List<RadioButton> radioButtonsTheme;                                            

        public Window_Main()
        {
            InitializeComponent();

            this.DataContext = this;

            radioButtonsTheme = new List<RadioButton>();
            radioButtonsTheme.Add(radioButtonLight);
            radioButtonsTheme.Add(radioButtonDark);
            foreach (RadioButton item in radioButtonsTheme)
            {
                if (item.Tag.ToString() == Class_Settings.settingsDictionary[Class_Settings.settingsKeyTheme])
                {
                    item.IsChecked = true;
                    break;
                }
            }

            this.Resources.MergedDictionaries.Add(Class_Settings.themeDictionary);                          //Set the GUI theme
            Command_Controls = new Class_RelayCommand(Command_Controls_Executed);
            Command_LineChart = new Class_RelayCommand(Command_LineChart_Executed);
            Command_Excel = new Class_RelayCommand(Command_Excel_Executed);
        }

        //This event is called when a Theme RadioButton was clicked
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            //Uncheck all other RadioButtons
            foreach (RadioButton item in radioButtonsTheme)
            {
                item.IsChecked = false;
            }

           ((RadioButton)sender).IsChecked = true;

            if (((RadioButton)sender).Tag.ToString() != Class_Settings.settingsDictionary[Class_Settings.settingsKeyTheme])
            {
                if (false == this.Resources.MergedDictionaries.Remove(Class_Settings.themeDictionary))
                {
                    throw new Exception();
                }

                Class_Settings.settingsDictionary[Class_Settings.settingsKeyTheme] = ((RadioButton)sender).Tag.ToString();
                Class_Settings.SetRessourceDictionaryTheme();
                Class_Settings.SaveApplicationsSettings();

                this.Resources.MergedDictionaries.Add(Class_Settings.themeDictionary);
            }
        }


        public Class_RelayCommand Command_Controls { get; }
        private void Command_Controls_Executed(Object qObject)
        {
            Window_Controls controls = new Window_Controls();
            controls.ShowDialog();
        }

        public Class_RelayCommand Command_LineChart { get; }
        private void Command_LineChart_Executed(Object qObject)
        {
            Window_LineChart lineChart = new Window_LineChart();
            lineChart.ShowDialog();
        }

        public Class_RelayCommand Command_Excel { get; }
        private void Command_Excel_Executed(Object qObject)
        {
            Window_Excel excel = new Window_Excel();
            excel.ShowDialog();
        }
    }
}
