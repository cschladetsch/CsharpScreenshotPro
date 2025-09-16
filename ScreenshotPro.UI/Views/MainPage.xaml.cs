using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ScreenshotPro.UI.Views
{
    /// <summary>
    /// Main page with navigation to different views
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LibraryPage));
        }

        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EditorPage));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}
