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
using System.Windows.Shapes;
using TestSuite.Data;

namespace TestSuite.Windows
{
    /// <summary>
    /// Interaktionslogik für Window_UserControls.xaml
    /// </summary>
    public partial class Window_UserControls : Window
    {

        Window_UserControlsVM _viewModel;                                         


        public Window_UserControls()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Resources.MergedDictionaries.Add(Class_Settings.themeDictionary);
            _viewModel = new Window_UserControlsVM();
            this.DataContext = _viewModel;
        }

       
    }

}
