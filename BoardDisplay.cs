using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverlightSudokuHelper
{
    internal class BoardDisplay : Control
    {
        private const int MarkerMargin = 2;
        private const string FontFamily = "Arial";

        private bool _candidatesVisible = true;
        private Canvas _root;
        private Storyboard _fadeStoryboard;
        private DoubleAnimation _fadeAnimation;
        private Rectangle _frame;
        private Line[] _verticalLines;
        private Line[] _horizontalLines;
        private TextBlock[,] _values;
        private TextBlock[, ,] _candidates;
        private Marker _marker;
        private Point _markerPosition;
        private Marker _conflict;
        private Point? _conflictPosition;
        private Board _board;

        public BoardDisplay()
        {
            // Initialize from XAML and get references to child elements
            _root = InitializeFromXaml(Utility.GetXamlResource("SilverlightSudokuHelper.BoardDisplay.xaml")) as Canvas;
            _fadeStoryboard = _root.FindName("FadeStoryboard") as Storyboard;
            _fadeAnimation = _root.FindName("FadeAnimation") as DoubleAnimation;

            // Create the board frame
            _frame = new Rectangle();
            _frame.Fill = new SolidColorBrush { Color = Colors.White };
            _frame.Stroke = new SolidColorBrush { Color = Colors.Black };
            _frame.StrokeThickness = 2;
            _frame.RadiusX = 5;
            _frame.RadiusY = 5;
            _root.Children.Add(_frame);

            // Create the board lines
            _verticalLines = new Line[Board.Size - 1];
            _horizontalLines = new Line[Board.Size - 1];
            for (var i = 0; i < _verticalLines.Length; i++)
            {
                bool majorLine = (0 == ((i + 1) % 3));
                var verticalLine = new Line();
                verticalLine.Stroke = new SolidColorBrush { Color = (majorLine ? Colors.Black : Colors.Gray) };
                verticalLine.StrokeThickness = 2;
                verticalLine.SetValue(Canvas.ZIndexProperty, (majorLine ? 1 : 0));
                _root.Children.Add(verticalLine);
                _verticalLines[i] = verticalLine;
                var horizontalLine = new Line();
                horizontalLine.Stroke = new SolidColorBrush { Color = (majorLine ? Colors.Black : Colors.Gray) };
                horizontalLine.StrokeThickness = 2;
                horizontalLine.SetValue(Canvas.ZIndexProperty, (majorLine ? 1 : 0));
                _root.Children.Add(horizontalLine);
                _horizontalLines[i] = horizontalLine;
            }

            // Create the selection marker
            _marker = new Marker("SilverlightSudokuHelper.MarkerSelection.xaml");
            _root.Children.Add(_marker);
            _markerPosition = new Point(4, 4);

            // Create the conflict marker
            _conflict = new Marker("SilverlightSudokuHelper.MarkerConflict.xaml");
            _root.Children.Add(_conflict);

            // Create the value and candidate text blocks
            _values = new TextBlock[Board.Size, Board.Size];
            _candidates = new TextBlock[Board.Size, Board.Size, 9];
            for (var y = 0; y < Board.Size; y++)
            {
                for (var x = 0; x < Board.Size; x++)
                {
                    var value = new TextBlock { FontFamily = FontFamily };
                    _root.Children.Add(value);
                    _values[x, y] = value;
                    for (var z = 0; z < 9; z++)
                    {
                        var candidate = new TextBlock { FontFamily = FontFamily, Foreground = new SolidColorBrush { Color = Colors.Gray } };
                        _root.Children.Add(candidate);
                        _candidates[x, y, z] = candidate;
                    }
                }
            }

            // Handle the Loaded event to do layout
            Loaded += new EventHandler(HandleLoaded);
        }

        private void HandleLoaded(object sender, EventArgs e)
        {
            // Do layout
            Layout();
        }

        public void Layout()
        {
            // Resize the frame and lines to fill the control
            _frame.Width = Width;
            _frame.Height = Height;
            for (var i = 0; i < _verticalLines.Length; i++)
            {
                var verticalLine = _verticalLines[i];
                verticalLine.X1 = Math.Round(Width * ((double)(i + 1) / (_verticalLines.Length + 1)));
                verticalLine.Y1 = _frame.StrokeThickness;
                verticalLine.X2 = verticalLine.X1;
                verticalLine.Y2 = Height - _frame.StrokeThickness;
                var horizontalLine = _horizontalLines[i];
                horizontalLine.X1 = _frame.StrokeThickness;
                horizontalLine.Y1 = Math.Round(Height * ((double)(i + 1) / (_horizontalLines.Length + 1)));
                horizontalLine.X2 = Width - _frame.StrokeThickness;
                horizontalLine.Y2 = horizontalLine.Y1;
            }
            // Experimentally compute the largest acceptable font size for value text
            var valueBoundingWidth = Width / Board.Size;
            var valueBoundingHeight = (Height / Board.Size) * 0.85;
            var valueProbe = _values[0, 0];
            valueProbe.FontSize = 50;
            while ((0 < valueProbe.FontSize) && ((valueBoundingWidth <= valueProbe.ActualHeight) || (valueBoundingHeight <= valueProbe.ActualHeight)))
            {
                valueProbe.FontSize--;
            }
            // Experimentally compute the largest acceptable font size for candidate text
            var candidateBoundingWidth = valueBoundingWidth / 3;
            var candidateBoundingHeight = valueBoundingHeight / 3;
            var candidateProbe = _candidates[0, 0, 0];
            candidateProbe.FontSize = 50;
            while ((0 < candidateProbe.FontSize) && ((candidateBoundingWidth <= candidateProbe.ActualHeight) || (candidateBoundingHeight <= candidateProbe.ActualHeight)))
            {
                candidateProbe.FontSize--;
            }
            for (var y = 0; y < Board.Size; y++)
            {
                for (var x = 0; x < Board.Size; x++)
                {
                    // Update the value's size, styling, and position
                    var value = _values[x, y];
                    value.FontSize = valueProbe.FontSize;
                    if (null != _board)
                    {
                        value.FontWeight = (Digit.Kind.Given == _board.GetDigit(x, y).DigitKind) ? FontWeights.Bold : FontWeights.Normal;
                        value.FontStyle = (Digit.Kind.Guess == _board.GetDigit(x, y).DigitKind) ? FontStyles.Italic : FontStyles.Normal;
                    }
                    var cellRect = GetCellRect(x, y);
                    Utility.CenterTextBlock(value, cellRect.Left, cellRect.Top, cellRect.Width, cellRect.Height);
                    for (var z = 0; z < 9; z++)
                    {
                        // Update the candidate's visibility, size, and position
                        var candidate = _candidates[x, y, z];
                        if (_candidatesVisible)
                        {
                            candidate.Visibility = Visibility.Visible;
                            candidate.FontSize = candidateProbe.FontSize;
                            var candidateWidth = cellRect.Width / 3;
                            var candidateLeft = cellRect.Left + ((z % 3) * candidateWidth);
                            var candidateHeight = cellRect.Height / 3;
                            var candidateTop = cellRect.Top + ((z / 3) * candidateHeight);
                            Utility.CenterTextBlock(candidate, candidateLeft, candidateTop, candidateWidth, candidateHeight);
                        }
                        else
                        {
                            candidate.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            // Layout the markers
            LayoutMarker(_marker, _markerPosition);
            if (_conflictPosition.HasValue)
            {
                LayoutMarker(_conflict, _conflictPosition.Value);
            }
            else
            {
                _conflict.Visibility = Visibility.Collapsed;
            }
        }

        private void LayoutMarker(Marker marker, Point position)
        {
            // Update the marker's size and position
            marker.Visibility = Visibility.Visible;
            var cellRect = GetCellRect((int)position.X, (int)position.Y);
            marker.SetValue(Canvas.LeftProperty, cellRect.Left + MarkerMargin);
            marker.SetValue(Canvas.TopProperty, cellRect.Top + MarkerMargin);
            marker.Width = cellRect.Width - (2 * MarkerMargin);
            marker.Height = cellRect.Height - (2 * MarkerMargin);
            // Tell the marker to lay itself out
            marker.Layout();
        }

        // Get/set the board being displayed
        public Board Board
        {
            get { return _board; }
            set
            {
                _conflictPosition = null;
                UpdateDisplay(value);
            }
        }

        // Get/set the visibility of the candidates
        public bool CandidatesVisible
        {
            get { return _candidatesVisible; }
            set
            {
                _candidatesVisible = value;
                _conflictPosition = null;
                Layout();
            }
        }

        // Fade the display out
        public void Fade(double seconds)
        {
            _root.Opacity = 1.0;
            _fadeAnimation.Duration = TimeSpan.FromSeconds(seconds);
            _fadeStoryboard.Begin();
        }

        // Get the dimensions of the specified cell
        private Rect GetCellRect(int x, int y)
        {
            var valueTop = Math.Round(Height * ((double)y / Board.Size));
            var valueBottom = Math.Round(Height * ((double)(y + 1) / Board.Size));
            var valueHeight = valueBottom - valueTop;
            var valueLeft = Math.Round(Width * ((double)x / Board.Size));
            var valueRight = Math.Round(Width * ((double)(x + 1) / Board.Size));
            var valueWidth = valueRight - valueLeft;
            return new Rect(valueLeft, valueTop, valueWidth, valueHeight);
        }

        private void UpdateDisplay(Board board)
        {
            // Update the value and candidate numbers according to the specified board
            for (var y = 0; y < Board.Size; y++)
            {
                for (var x = 0; x < Board.Size; x++)
                {
                    var digit = board.GetDigit(x, y);
                    _values[x, y].Text = (digit.ValueKnown ? digit.KnownValue.ToString() : " ");
                    for (var z = 0; z < 9; z++)
                    {
                        _candidates[x, y, z].Text = (digit.ValueKnown ? " " : (digit.CouldBe(z + 1) ? (z + 1).ToString() : " "));
                    }
                }
            }
            // Incorporate the specified board and layout
            _board = board;
            Layout();
        }

        // Get/set the marker position
        public Point MarkerPosition
        {
            get { return _markerPosition; }
            set
            {
                _markerPosition.X = Math.Max(0, Math.Min(Board.Size - 1, value.X));
                _markerPosition.Y = Math.Max(0, Math.Min(Board.Size - 1, value.Y));
                LayoutMarker(_marker, _markerPosition);
            }
        }

        public bool ChangeSelectedValue(int value, Digit.Kind kind)
        {
            var changed = false;
            // Get the current position
            _conflictPosition = null;
            var x = (int)_markerPosition.X;
            var y = (int)_markerPosition.Y;
            if (Digit.Unknown == value)
            {
                // Clear the digit's value if set
                if (_board.GetDigit(x, y).ValueKnown)
                {
                    _board.ClearValue(x, y);
                    changed = true;
                }
            }
            else
            {
                try
                {
                    // Set the cell's value
                    _board.SetValue(x, y, value, kind);
                    changed = true;
                }
                catch (InvalidBoardException e)
                {
                    // Would have created an invalid board; identify the location of the conflict
                    _conflictPosition = new Point(e.X, e.Y);
                }
            }
            // Update the display
            UpdateDisplay(_board);
            return changed;
        }
    }
}
