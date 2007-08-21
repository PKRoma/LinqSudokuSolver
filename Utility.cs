using System;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace SilverlightSudokuHelper
{
    internal static class Utility
    {
        public static string GetXamlResource(string resourceName)
        {
            // Load XAML from an embedded resource by name
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static void CenterTextBlock(TextBlock textBlock, double left, double top, double width, double height)
        {
            // Center a TextBlock within the provided bounds
            textBlock.SetValue(Canvas.LeftProperty, Math.Round(left + ((width - textBlock.ActualWidth) / 2)));
            textBlock.SetValue(Canvas.TopProperty, Math.Round(top + ((height - textBlock.ActualHeight) / 2)));
        }
    }
}
