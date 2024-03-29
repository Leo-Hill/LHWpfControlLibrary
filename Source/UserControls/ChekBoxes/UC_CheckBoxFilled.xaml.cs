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

    public partial class UC_CheckBoxFilled : UserControl
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
        public EventHandler EHCheckedChanged;
        //Primitive

        /***********************************************************************************************
        * 
        * Properties
        * 
        **********************************************************************************************/
        //Text
        public String Text { get { return (String)GetValue(DPText); } set { SetValue(DPText, value); } }    //Title of the bar
        public static readonly DependencyProperty DPText = DependencyProperty.Register("Text", typeof(String), typeof(UC_CheckBoxFilled));

        //Colors
        public SolidColorBrush SCBBackground { get { return (SolidColorBrush)GetValue(DPSCBBackground); } set { SetValue(DPSCBBackground, value); } }
        public static readonly DependencyProperty DPSCBBackground = DependencyProperty.Register("SCBBackground", typeof(SolidColorBrush), typeof(UC_CheckBoxFilled));

        //Status
        public bool bIsChecked { get { return (bool)GetValue(DPIsChecked); } set { SetValue(DPIsChecked, value); } }    //IsChecked
        public static readonly DependencyProperty DPIsChecked = DependencyProperty.Register(nameof(bIsChecked), typeof(bool), typeof(UC_CheckBoxFilled), new UIPropertyMetadata(false));
        public bool bMouseDown { get { return (bool)GetValue(DPMouseDown); } set { SetValue(DPMouseDown, value); } }    //MouseDown
        public static readonly DependencyProperty DPMouseDown = DependencyProperty.Register(nameof(bMouseDown), typeof(bool), typeof(UC_CheckBoxFilled), new UIPropertyMetadata(false));
        public bool bIsPressed { get { return (bool)GetValue(DPPressed); } set { SetValue(DPPressed, value); } }    //MouseDown
        public static readonly DependencyProperty DPPressed = DependencyProperty.Register(nameof(bIsPressed), typeof(bool), typeof(UC_CheckBoxFilled), new UIPropertyMetadata(false));

        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public UC_CheckBoxFilled()
        {
            InitializeComponent();
            //this.DataContext = this;
            this.Cursor = Cursors.Hand;
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

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bIsPressed = true;
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (true == bIsPressed)
            {
                bIsChecked = !bIsChecked;
                if(null!=EHCheckedChanged)
                {
                    EHCheckedChanged(this, EventArgs.Empty);
                }
            }
            bIsPressed = false;
            
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if(true==bIsPressed)
            {
                bIsPressed = false;
            }
        }
        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function allows to set the fill color programmatically by creating a new style.
        public void vSetCheckedColor(SolidColorBrush qSolidColorBrush)
        {
            Style BaseStyle = (Style)FindResource("STY_CheckBoxFilledBorder");

            Style style = new Style(typeof(Border));                                                //New style for the border
            style.Setters.Add(new Setter(Border.BorderBrushProperty, qSolidColorBrush));
            style.Setters.Add(BaseStyle.Setters[1]);

            DataTrigger DTIsChecked = new DataTrigger();                                            //DataTrigger for checked
            Binding binding = new Binding();                                                        //Binding for the datatrigger
            binding.Path = new PropertyPath(nameof(bIsChecked));
            binding.Source = this;
            DTIsChecked.Binding = binding;
            DTIsChecked.Value = true;
            DTIsChecked.Setters.Add(new Setter(Border.BackgroundProperty, qSolidColorBrush));
            DTIsChecked.Setters.Add(new Setter(Border.BackgroundProperty, qSolidColorBrush));

            DataTrigger DTIsPressed = new DataTrigger();                                            //DataTrigger for is pressed
            binding = new Binding();                                                                //Binding for the datatrigger
            binding.Path = new PropertyPath(nameof(bIsPressed));
            binding.Source = this;
            DTIsPressed.Binding = binding;
            DTIsPressed.Value = true;
            SolidColorBrush SCBPressed = qSolidColorBrush.Clone();
            SCBPressed.Opacity = 0.5;
            DTIsPressed.Setters.Add(new Setter(Border.BackgroundProperty, SCBPressed));
            DTIsPressed.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.Transparent));

            style.Triggers.Add(DTIsChecked);
            style.Triggers.Add(BaseStyle.Triggers[1]);                                              //Trigger for mouseover
            style.Triggers.Add(DTIsPressed);
            BRDBox.Style = style;                                                                   //Apply the style to the border
        }

    }
}
