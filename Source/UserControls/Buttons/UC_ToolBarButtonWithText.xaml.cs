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

namespace LHWpfControlLibrary.Source.UserControls
{
    /// <summary>
    /// Interaktionslogik für ToolBarButtonWithText.xaml
    /// </summary>
    public partial class UC_ToolBarButtonWithText : UserControl
    {
        /***********************************************************************************************
       * 
       * Constants
       * 
       * *********************************************************************************************/

        /***********************************************************************************************
        * 
        * Variables
        * 
        * *********************************************************************************************/

        /***********************************************************************************************
        * 
        * Properties
        * 
        * *********************************************************************************************/
        //Command of the button
        public ICommand Command { get { return (ICommand)GetValue(DPButton); } set { SetValue(DPButton, value); } }
        //DependencyProperty for the button command
        public static readonly DependencyProperty DPButton =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(UC_ToolBarButtonWithText), new UIPropertyMetadata(null));

        //Label for the text
        public String Text { get { return (String)GetValue(DPText); } set { SetValue(DPText, value); } }
        //DependencyProperty for label text
        public static readonly DependencyProperty DPText =
            DependencyProperty.Register("Text", typeof(String), typeof(UC_ToolBarButtonWithText));

        //ContentControl for the icon
        public DataTemplate Icon { get { return (DataTemplate)GetValue(DPIcon); } set { SetValue(DPIcon, value); } }
        //DependencyProperty for label text
        public static readonly DependencyProperty DPIcon =
            DependencyProperty.Register("Icon", typeof(DataTemplate), typeof(UC_ToolBarButtonWithText));
      

        /***********************************************************************************************
        * 
        * Constructor
        * 
        * *********************************************************************************************/
        public UC_ToolBarButtonWithText()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
