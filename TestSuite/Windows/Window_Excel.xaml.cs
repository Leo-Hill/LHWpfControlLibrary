using LHCommonFunctions.Source;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
using Excel = Microsoft.Office.Interop.Excel;
namespace TestSuite.Windows
{
    /// <summary>
    /// Interaktionslogik für Window_Excel.xaml
    /// </summary>
    public partial class Window_Excel : Window
    {
        public Window_Excel()
        {
            InitializeComponent();
            this.Resources.MergedDictionaries.Add(Class_Settings.themeDictionary);                          //Set the GUI theme
        }


        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            Excel.Application excelApp = new Excel.Application();
            Excel.Workbook excelWorkBook = excelApp.Workbooks.Add();
            Excel.Worksheet excelWorkSheet = (Excel.Worksheet)excelWorkBook.Worksheets.get_Item(1);


            int numOfSeries = (int)NUD_NumOfSeries.dCurrentNumber;

            //Create header
            excelWorkSheet.Cells[1, 1] = "Time";
            for (int seriesCnt = 0; seriesCnt < numOfSeries; seriesCnt++)
            {
                excelWorkSheet.Cells[1, 2 + seriesCnt] = "Series " + (seriesCnt + 1);
            }


            //Add some data
            double phaseShift = (2 * Math.PI) / numOfSeries;
            const int numOfDataPoints = 100;

            DateTime[,] dateTimes = new DateTime[numOfDataPoints, 1];
            TimeSpan increment = (TimeSetter_End.ActDateTime - TimeSetter_Start.ActDateTime) / numOfDataPoints;

            double[,] data = new double[numOfDataPoints, numOfSeries];
            double amplitude = (NUD_MaxValue.dCurrentNumber - NUD_MinValue.dCurrentNumber) / 2;
            double dataYOffset = NUD_MaxValue.dCurrentNumber - amplitude;

            for (int dataCnt = 0; dataCnt < numOfDataPoints; dataCnt++)
            {
                dateTimes[dataCnt, 0] = TimeSetter_Start.ActDateTime.Add(increment * dataCnt);
                for (int seriesCnt = 0; seriesCnt < numOfSeries; seriesCnt++)
                {
                    data[dataCnt, seriesCnt] = dataYOffset + amplitude * Math.Sin((dataCnt * 2 * Math.PI / numOfDataPoints) - phaseShift * seriesCnt);
                }
            }

            //Time
            Excel.Range excelRange = (Excel.Range)excelWorkSheet.Range[excelWorkSheet.Cells[2, 1], excelWorkSheet.Cells[1 + dateTimes.Length, 1]];
            excelRange.Value = dateTimes;                                                         //Write the date-time array to excel
            excelRange = (Excel.Range)excelWorkSheet.Columns[1, Type.Missing];
            excelRange.NumberFormat = "hh:mm:ss";

            //Data
            excelRange = (Excel.Range)excelWorkSheet.Range[excelWorkSheet.Cells[2, 2], excelWorkSheet.Cells[1 + numOfDataPoints, 1 + numOfSeries]];
            excelRange.Value = data;
            excelRange = (Excel.Range)excelWorkSheet.Range[excelWorkSheet.Columns[2, Type.Missing], excelWorkSheet.Columns[1 + numOfSeries, Type.Missing]];
            excelRange.NumberFormat = "0.00";

            //Create a chart


            Excel.ChartObjects excelChartObjects = (Excel.ChartObjects)excelWorkSheet.ChartObjects();
            Excel.ChartObject excelChartObject = (Excel.ChartObject)excelChartObjects.Add(200, 10, 800, 400);
            Excel.Chart excelChart = excelChartObject.Chart;
            excelRange = (Excel.Range)excelWorkSheet.Range[excelWorkSheet.Columns[1, Type.Missing], excelWorkSheet.Columns[1 + numOfSeries, Type.Missing]];
            excelChart.SetSourceData(excelRange);
            LHExcelFunctions.vStyleLineChart(excelChartObject, TimeSetter_Start.ActDateTime, TimeSetter_End.ActDateTime);

            Mouse.OverrideCursor = null;
            try
            {

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Excel file|*.xlsx";
                if (saveFileDialog.ShowDialog() == true)
                {
                    excelWorkBook.SaveAs(saveFileDialog.FileName, Excel.XlFileFormat.xlWorkbookDefault);
                }
                excelWorkBook.Close(false);
                excelApp.Quit();
                Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
            }
            catch
            {
                return;
            }
            finally
            {
                //Clean up excel objects
                Marshal.ReleaseComObject(excelChartObjects);
                Marshal.ReleaseComObject(excelChartObject);
                Marshal.ReleaseComObject(excelChart);
                Marshal.ReleaseComObject(excelRange);
                Marshal.ReleaseComObject(excelWorkSheet);
                Marshal.ReleaseComObject(excelWorkBook);
                Marshal.ReleaseComObject(excelApp);
            }
        }
    }
}
