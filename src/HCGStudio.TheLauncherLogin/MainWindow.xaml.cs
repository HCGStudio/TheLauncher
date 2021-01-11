using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace HCGStudio.TheLauncherLogin
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void webView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("https://login.live.com/oauth20_desktop.srf"))
            {
                Console.WriteLine(e.Uri);
                Close();
            }
        }
    }
}