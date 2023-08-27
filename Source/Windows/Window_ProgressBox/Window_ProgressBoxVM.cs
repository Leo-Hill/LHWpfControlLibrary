using LHCommonFunctions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LHWpfControlLibrary.Source.Windows
{
    public abstract class Window_ProgressBoxVM : ViewModelBase
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
        public event EventHandler EHOnRequestClose;


        /***********************************************************************************************
        * 
        * Properties
        * 
        **********************************************************************************************/
        public abstract bool bIsLoaded { set; }                                                     //This property is set by the view if the window is loaded

        public abstract int iProgress { get; }                                                      //Progress the progressbar binds to

        public abstract String sHeader { get; }                                                     //Header text
        public abstract String sProgressText { get; set; }                                          //Text above the progressbar


        /***********************************************************************************************
        * 
        * Constructor
        * 
        **********************************************************************************************/
       

        /***********************************************************************************************
        * 
        * Commands
        * 
        **********************************************************************************************/
        public abstract RelayCommand Command_OK { get; }
        public virtual void vCommand_OK_Executed(Object qObject)
        {
            EHOnRequestClose.Invoke(this, new EventArgs());                                         //Close the window
        }
        public abstract bool bCommand_OK_CanExecute(Object qObject);


       
        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/

    }
}
