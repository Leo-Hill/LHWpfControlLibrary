using LHCommonFunctions.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSuite.Windows
{
    public class Window_UserControlsVM : Class_ViewModelBase
    {

        public Window_UserControlsVM()
        {
            Command_ToolBarButtonClick = new Class_RelayCommand(Command_ToolBarButtonClick_Executed);
            Command_ImageButtonWithTextClick = new Class_RelayCommand(Command_ImageButtonWithTextClick_Executed);
        }

        //ToolBarButton
        public int ToolBarButtonClickCnt
        { get; set; }
        public Class_RelayCommand Command_ToolBarButtonClick { get; }
        private void Command_ToolBarButtonClick_Executed(Object qObject)
        {
            ToolBarButtonClickCnt++;
            OnPropertyChanged(nameof(ToolBarButtonClickCnt));
        }

        //ImageButtonWithText
        public int ImageButtonClickCnt
        { get; set; }
        public Class_RelayCommand Command_ImageButtonWithTextClick { get; }
        private void Command_ImageButtonWithTextClick_Executed(Object qObject)
        {
            ImageButtonClickCnt++;
            OnPropertyChanged(nameof(ImageButtonClickCnt));
        }

        //CheckBoxFilled


    }
}
