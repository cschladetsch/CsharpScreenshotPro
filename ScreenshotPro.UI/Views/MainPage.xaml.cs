using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class MainPage : Page
    {
        private readonly Dictionary<string, Type> _routes = new()
        {
            ["library"] = typeof(LibraryPage),
            ["editor"] = typeof(EditorPage),
            ["settings"] = typeof(SettingsPage)
        };

        public MainPage()
        {
            InitializeComponent();
        }

        private void RootNav_Loaded(object sender, RoutedEventArgs e)
        {
            if (RootNav.MenuItems.Count > 0)
            {
                RootNav.SelectedItem = RootNav.MenuItems[0];
            }

            NavigateTo("library");
        }

        private void RootNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
            {
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string tag)
        {
            if (_routes.TryGetValue(tag, out var pageType))
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
