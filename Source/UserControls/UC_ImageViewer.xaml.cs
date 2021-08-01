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

namespace LHWpfControlLibrary.Source.UserControls
{
    /// <summary>
    /// Interaktionslogik für UC_ImageViewer.xaml
    /// </summary>
    public partial class UC_ImageViewer : UserControl
    {
        /***********************************************************************************************
        * 
        * Variables
        * 
        **********************************************************************************************/
        //Primitive
        int iImageCnt;                                                                              //Counter for the current images

        public String[] asImagePaths;                                                               //Paths of the images to show 

        /***********************************************************************************************
        * 
        * Properites
        * 
        **********************************************************************************************/
        public String sImageNumber { get; set; }                                                    //Text below the imageviewer

        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
        public UC_ImageViewer()
        {
            InitializeComponent();
            DataContext = this;                                                                     //Set the datacontext

            //Initialize variables
            iImageCnt = 0;
            asImagePaths = new String[0];

            //Initialize commands
            Command_NextImage = new Class_RelayCommand(vCommand_NextImage_Executed, o => (iImageCnt + 1) < asImagePaths.Length);
            Command_PreviousImage = new Class_RelayCommand(vCommand_PreviousImage_Executed, o => iImageCnt > 0);
        }

        /***********************************************************************************************
        * 
        * Events
        * 
        **********************************************************************************************/


        /***********************************************************************************************
        * 
        * Commands
        * 
        **********************************************************************************************/
        public Class_RelayCommand Command_NextImage { get; }                                        //Command to switch to the next image
        private void vCommand_NextImage_Executed(Object qObject)
        {
            iImageCnt++;
            vUpdateViewer();
        }

        public Class_RelayCommand Command_PreviousImage { get; }                                    //Command to switch to the next previous
        private void vCommand_PreviousImage_Executed(Object qObject)
        {
            iImageCnt--;
            vUpdateViewer();
        }

        /***********************************************************************************************
       * 
       * Functions
       * 
       **********************************************************************************************/
        public void vUpdateViewer()
        {
            ImgViewer.Source = new BitmapImage(new Uri(asImagePaths[iImageCnt], UriKind.Absolute));
            sImageNumber = (iImageCnt + 1) + " / " + (asImagePaths.Length);
            LText.Content = sImageNumber;
        }

    }
}
