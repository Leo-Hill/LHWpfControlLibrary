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
    public partial class UC_ImageButtonWithText : UserControl
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

        /***********************************************************************************************
        * 
        * Properties
        * 
        **********************************************************************************************/
        //Command of the button
        public ICommand Command { get { return (ICommand)GetValue(DPButton); } set { SetValue(DPButton, value); } }
        //DependencyProperty for the button command
        public static readonly DependencyProperty DPButton =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(UC_ImageButtonWithText), new UIPropertyMetadata(null));

        //Label for the text
        public String Text { get { return (String)GetValue(DPText); } set { SetValue(DPText, value); } }
        //DependencyProperty for label text
        public static readonly DependencyProperty DPText =
            DependencyProperty.Register(nameof(Text), typeof(String), typeof(UC_ImageButtonWithText));

        //ImageSource for the image
        public ImageSource Image { get { return (ImageSource)GetValue(DPIcon); } set { SetValue(DPIcon, value); } }
        //DependencyProperty for the icon
        public static readonly DependencyProperty DPIcon =
            DependencyProperty.Register(nameof(Image), typeof(ImageSource), typeof(UC_ImageButtonWithText));


        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public UC_ImageButtonWithText()
        {
            InitializeComponent();
        }
    }
}
