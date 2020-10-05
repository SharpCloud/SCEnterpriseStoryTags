﻿using SCEnterpriseStoryTags.Services;
using System.Windows;
using System.Windows.Threading;

namespace SCEnterpriseStoryTags
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var splashScreen = new SplashScreen("Images/Splash.png");
            splashScreen.Show(true, true);

            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var message = $"{e.Exception.Message} {e.Exception.StackTrace}";
            IOService.WriteToFile("SCEnterpriseStoryTags.log", message, false);
        }
    }
}
