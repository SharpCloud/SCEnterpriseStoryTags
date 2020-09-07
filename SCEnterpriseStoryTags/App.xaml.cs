using System.Windows;

namespace AMRCStoryTags
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
        }
    }
}
