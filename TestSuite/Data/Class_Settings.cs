using LHCommonFunctions.Source;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace TestSuite.Data
{
    /***********************************************************************************************
    * 
    * This class provides functions for import and export of settings and handles global application settings
    * It contains all paths and filenames used in the application
    * 
    **********************************************************************************************/
    static class Class_Settings
    {
        /***********************************************************************************************
        * 
        * Constants
        * 
        **********************************************************************************************/
        //Paths
        public readonly static String appName = "LHWpfControlLibrary_TestSuite";                                //App name
        public readonly static String pathAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/" + appName;    //%APPDATA%
        public readonly static String pathSettings = Class_Settings.pathAppData + "/Files/Settings";

        //Filenames
        public const String filenameSettings = "Settings.xml";                                   //Filename of the settings file

        //IDs for settings xml file
        public const String settingsKeyTheme = "Theme";                                         //ID for the application theme
        public const String settingsKeyLanguage = "Language";                                   //ID for the application language
        public const String settingsKeyWindowState = "WindowState";                                      //ID for the last main window state

        /***********************************************************************************************
       * 
       * Variables
       * 
       **********************************************************************************************/

        public static SortedDictionary<String, String> settingsDictionary;                                  //Dictionary containing the current settings <ID,Value>
        public static ResourceDictionary languageDictionary;                                                //This variable contains the resource dictionary of the current language
        public static ResourceDictionary themeDictionary;                                                   //This variable contains the resource dictionary of the current color theme

        /***********************************************************************************************
        * 
        * Functions
        * 
        **********************************************************************************************/
        //This function returns a string from the RDLanguage
        public static String GetStringFromRDLanguage(String qsKey)
        {
            if (languageDictionary.Contains(qsKey))
            {
                return (String)languageDictionary[qsKey];
            }
            else
            {
                return "!LANGUAGE MISSING!";
            }
        }

        //This function returns a color from the RDTheme
        public static SolidColorBrush GetColorFromRDTheme(String qsKey)
        {

            if (themeDictionary.Contains(qsKey))
            {
                return (SolidColorBrush)Class_Settings.themeDictionary[qsKey];

            }
            else
            {
                return Brushes.Pink;
            }
        }

        //This function loads the settings xml file and stores settings the neccessary variables
        public static void LoadApplicationSettings()
        {
            //Create directories if not already existing
            Directory.CreateDirectory(pathSettings);                                             //Directory for saving settings

            //Generate the settings file if it doesn't exist
            if (!File.Exists(pathSettings + "\\" + filenameSettings))                         //Settings file doesn't exist
            {
                String sDefaultSettingsFilePath = "TestSuite.Resources.Assets.SettingsDefault.xml";
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(sDefaultSettingsFilePath);    //Get the default settings file from assets
                XDocument XDDefault = XDocument.Load(stream);                                                           //Load the settings file from assets
                XDDefault.Save(pathSettings + "\\" + filenameSettings);                                           //Save the settings file to the settings directory
            }

            settingsDictionary = LHXmlFunctions.ImportXmlToDictionary(pathSettings + "\\" + filenameSettings);    //Get a dictionary of the settings

            //Initialize variables according to settings dictionary
            SetRessourceDictionaryTheme();
            SetRessourceDictionaryLanguage();
        }

        //This function saves the settings stored in SDSettings to the settings xml
        public static void SaveApplicationsSettings()
        {
            LHXmlFunctions.ExportDictionaryToXml(settingsDictionary, pathSettings + "\\" + filenameSettings);
        }

        //This function sets the RDLanguage according to the current language setting
        public static void SetRessourceDictionaryLanguage()
        {
            languageDictionary = new ResourceDictionary();

            if (!settingsDictionary.ContainsKey(settingsKeyTheme) || settingsDictionary[settingsKeyTheme] == "en")
            {
                languageDictionary.Source = new Uri("Resources/Dictionaries/Language/Strings.en.xaml", UriKind.Relative);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }
            else
            {
                languageDictionary.Source = new Uri("Resources/Dictionaries/Language/Strings.de.xaml", UriKind.Relative);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
            }
        }


        //This function sets the RDTheme according to the current language setting
        public static void SetRessourceDictionaryTheme()
        {
            themeDictionary = new ResourceDictionary();

            if (!settingsDictionary.ContainsKey(settingsKeyTheme) || settingsDictionary[settingsKeyTheme] == "Light")
            {
                themeDictionary.Source = new Uri("Resources/Dictionaries/Themes/ThemeLight.xaml", UriKind.Relative);
            }
            else if (settingsDictionary[settingsKeyTheme] == "Dark")
            {
                themeDictionary.Source = new Uri("Resources/Dictionaries/Themes/ThemeDark.xaml", UriKind.Relative);
            }
        }
    }
}
