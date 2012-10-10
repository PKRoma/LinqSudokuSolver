using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverlightSudokuHelper
{
    internal class Marker : UserControl
    {
        private Canvas _root;
        private Rectangle _background;
        private Rectangle _border;
        private Storyboard _pulse;

        public Marker(string resourceName)
        {
            // Initialize from XAML and get references to child elements
            _root = XamlReader.Load(Utility.GetXamlResource(resourceName)) as Canvas;
            _background = _root.FindName("Background") as Rectangle;
            _border = _root.FindName("Border") as Rectangle;
            _pulse = _root.FindName("Pulse") as Storyboard;  // May fail
            Content = _root;
        }

        public void Layout()
        {
            // Update the child element dimensions according to the parent
            _background.Width = Width;
            _background.Height = Height;
            _border.Width = Width;
            _border.Height = Height;
            if (null != _pulse)
            {
                // Play the pulse animation if present
                _pulse.Begin();
            }
        }
    }
}
