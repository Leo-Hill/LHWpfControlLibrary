using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaktionslogik für UC_TitleBar.xaml
    /// </summary>
    public partial class UC_TitleBar : UserControl
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
        private Window WParentWindow;


        /***********************************************************************************************
        * 
        * Properties
        * 
        **********************************************************************************************/
        //Title of the bar
        public String Title
        {
            get { return (String)GetValue(DPTitle); }
            set { SetValue(DPTitle, value); }
        }

        //DependencyProperty for Title
        public static readonly DependencyProperty DPTitle = DependencyProperty.Register("Title", typeof(String), typeof(UC_TitleBar));

        public bool CanMaximize
        {
            get { return (bool)GetValue(DPCanMaximize); }
            set { SetValue(DPCanMaximize, value); }
        }
        public static readonly DependencyProperty DPCanMaximize = DependencyProperty.Register("CanMaximize", typeof(bool), typeof(UC_TitleBar), new PropertyMetadata(true));


        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public UC_TitleBar()
        {
            InitializeComponent();
            this.Loaded += This_Loaded;                                                             //Add a handler for loaded components
            DataContext = this;
        }

        /***********************************************************************************************
        * 
        * Commands
        * 
        **********************************************************************************************/

        //CommandClose is always enabled
        private void CommandMinimize_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        //Close the window
        private void CommandMinimize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(Window.GetWindow(this));
        }

        //CommandRestore is always enabled
        private void CommandRestore_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        //Restore the window
        private void CommandRestore_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(Window.GetWindow(this));
        }

        //CommandMaximize is always enabled
        private void CommandMaximize_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CanMaximize;
        }
        //Maximize the window
        private void CommandMaximize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(Window.GetWindow(this));
        }

        //CommandClose is always enabled
        private void CommandClose_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        //Close the window
        private void CommandClose_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(Window.GetWindow(this));
        }

        /***********************************************************************************************
        * 
        * Events
        * 
        **********************************************************************************************/
        //This event is called when the control is loaded
        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                WParentWindow = Window.GetWindow(this);
                WParentWindow.StateChanged += WParent_StateChanged;
                vDetermineWindowStateButton();
            }
            catch
            {

            }
        }

        //This event is called if the parent window state is changed
        private void WParent_StateChanged(object sender, EventArgs e)
        {
            vDetermineWindowStateButton();
        }

        private void StackPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (WParentWindow.WindowState == WindowState.Maximized)
                {

                    Point MousePosition = WParentWindow.PointToScreen(Mouse.GetPosition(WParentWindow));
                    Point RelativePosition = Mouse.GetPosition(SPParent);

                    double dMaximizedWidth= WParentWindow.ActualWidth;

                    //Setting window state to normal will resize and reposition the window according to application specific default values 
                    WParentWindow.WindowState = WindowState.Normal;

                    //The width of the window will shrink, so we have to calculate the x position in relation to that
                    double dXScalingFactor = WParentWindow.Width / dMaximizedWidth;
                    WParentWindow.Top = MousePosition.Y - RelativePosition.Y;
                    WParentWindow.Left = MousePosition.X-RelativePosition.X*dXScalingFactor;
                }
                WParentWindow.DragMove();
            }

        }

        private void This_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (WParentWindow.WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(Window.GetWindow(this));
                }
                else if (true == CanMaximize)
                {
                    SystemCommands.MaximizeWindow(Window.GetWindow(this));
                }
            }
        }

        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function sets the visibility of the maximize and normalize button according to the parent window state
        void vDetermineWindowStateButton()
        {
            if (WParentWindow.WindowState == WindowState.Maximized)
            {
                BRestore.Visibility = Visibility.Visible;
                BMaximize.Visibility = Visibility.Collapsed;
            }
            else
            {
                BRestore.Visibility = Visibility.Collapsed;
                BMaximize.Visibility = Visibility.Visible;
            }

        }


    }
}
