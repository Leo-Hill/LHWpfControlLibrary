﻿using LHCommonFunctions.Source;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TestSuite.Data;

namespace TestSuite.Windows
{
    /// <summary>
    /// Interaktionslogik für Window_Controls.xaml
    /// </summary>
    public partial class Window_Controls : Window
    {
        public Window_Controls()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Resources.MergedDictionaries.Add(Class_Settings.themeDictionary);
        }
    }
}