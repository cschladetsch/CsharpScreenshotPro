using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class OcrResultWindow : Window
    {
        private string _extractedText = string.Empty;

        public OcrResultWindow(string extractedText, float confidence, string fileName = "")
        {
            InitializeComponent();

            _extractedText = extractedText;

            // Configure window properties
            Title = string.IsNullOrEmpty(fileName) ? "OCR Result" : $"OCR Result - {fileName}";

            // Set window size and make it resizable
            if (AppWindow != null)
            {
                AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 500));
                AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
            }

            // Configure for dark title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);

            // Set content
            HeaderText.Text = $"Extracted Text ({extractedText.Length} characters, Confidence: {confidence:F1}%)";

            // Set RichTextBlock content - remove hard line breaks for proper wrapping
            var cleanedText = extractedText.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            // Remove multiple spaces
            while (cleanedText.Contains("  "))
            {
                cleanedText = cleanedText.Replace("  ", " ");
            }

            var paragraph = new Paragraph();
            var run = new Run { Text = cleanedText.Trim() };
            paragraph.Inlines.Add(run);
            TextContent.Blocks.Clear();
            TextContent.Blocks.Add(paragraph);

            // Center the window
            CenterOnScreen();
        }



        private void CenterOnScreen()
        {
            if (AppWindow != null)
            {
                // Get the display area
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var centerX = (displayArea.WorkArea.Width - AppWindow.Size.Width) / 2;
                    var centerY = (displayArea.WorkArea.Height - AppWindow.Size.Height) / 2;

                    AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                }
            }
        }

        private void CopyAgainButton_Click(object sender, RoutedEventArgs e)
        {
            // Copy text to clipboard
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(_extractedText);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}