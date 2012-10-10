using System;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace SilverlightSudokuHelper
{
    public partial class Page : Canvas
    {
        private enum SoundEffect { New, Move, Conflict, Complete };

        private const double FadeSecondsNormal = 0.5;
        private const double FadeSecondsLoading = 3;
        private const string BoardBlank =
            "         " +
            "         " +
            "         " +
            "         " +
            "         " +
            "         " +
            "         " +
            "         " +
            "         ";
        private const string BoardWikipediaSample =
            "53  7    " +
            "6  195   " +
            " 98    6 " +
            "8   6   3" +
            "4  8 3  1" +
            "7   2   6" +
            " 6    28 " +
            "   419  5" +
            "    8  79";
        private const string BoardSudopediaSample =
            "7  1 682 " +
            "  3      " +
            "  8 9 4  " +
            "  79     " +
            "    53 1 " +
            "1 92  6  " +
            "      93 " +
            "   5    2" +
            "   4   7 ";
        private const string BoardAlmostFinished =
            "123456789" +
            "456789123" +
            "789123456" +
            "231564897" +
            "5648 7231" +
            "897231564" +
            "312645978" +
            "645978312" +
            "978312645";

        // _primaryBoardDisplay renders the current board
        private BoardDisplay _primaryBoardDisplay;
        // _fadingBoardDisplay renders the previous board and fades away when changes are made
        private BoardDisplay _fadingBoardDisplay;
        private double _defaultVolume;
        private Content BrowserHost;

        public Page()
        {
            InitializeComponent();
        }

        public void Page_Loaded(object o, EventArgs e)
        {
            // Initialize variables
            _defaultVolume = mediaElement.Volume;
            BrowserHost = App.Current.Host.Content;

            // Initialize UI
            _primaryBoardDisplay = new BoardDisplay();
            Children.Add(_primaryBoardDisplay);
            _fadingBoardDisplay = new BoardDisplay();
            Children.Add(_fadingBoardDisplay);

            // Initialize handlers
            KeyUp += new KeyEventHandler(HandleKeyUp);
            MouseLeftButtonDown += new MouseButtonEventHandler(HandleMouseLeftButtonDown);
            BrowserHost.Resized += new EventHandler(HandleResize);

            // Create the starting board, play the "new" sound, and fade it in
            _primaryBoardDisplay.Board = Board.FromString(BoardWikipediaSample);
            PlaySoundEffect(SoundEffect.New);
            _fadingBoardDisplay.Fade(FadeSecondsLoading);
        }

        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape: // Escape
                    _primaryBoardDisplay.Solve();
                    break;
                case Key.Left: // Left arrow
                case Key.Up: // Up arrow
                case Key.Right: // Right arrow
                case Key.Down: // Down arrow
                    // Move the marker
                    var markerPosition = _primaryBoardDisplay.MarkerPosition;
                    switch (e.Key)
                    {
                        case Key.Left: markerPosition.X--; break;
                        case Key.Up: markerPosition.Y--; break;
                        case Key.Right: markerPosition.X++; break;
                        case Key.Down: markerPosition.Y++; break;
                    }
                    _primaryBoardDisplay.MarkerPosition = markerPosition;
                    _fadingBoardDisplay.MarkerPosition = markerPosition;
                    break;
                case Key.Delete: // Delete
                    // Clear the cell's value
                    PrepareFade();
                    if (_primaryBoardDisplay.ChangeSelectedValue(Digit.Unknown, Digit.Kind.Normal))
                    {
                        // Play the appropriate sound and fade it
                        PlaySoundEffect(SoundEffect.Move);
                        _fadingBoardDisplay.Fade(FadeSecondsNormal);
                    }
                    break;
                case Key.D1: // 1
                case Key.D2: // 2
                case Key.D3: // 3
                case Key.D4: // 4
                case Key.D5: // 5
                case Key.D6: // 6
                case Key.D7: // 7
                case Key.D8: // 8
                case Key.D9: // 9
                    // Set the cell's value
                    PrepareFade();
                    if (_primaryBoardDisplay.ChangeSelectedValue(e.Key - Key.D0, ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? Digit.Kind.Given : ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ? Digit.Kind.Guess : Digit.Kind.Normal))))
                    {
                        // Normal move; play the appropriate sound and fade it
                        PlaySoundEffect(_primaryBoardDisplay.Board.Complete ? SoundEffect.Complete : SoundEffect.Move);
                        _fadingBoardDisplay.Fade(FadeSecondsNormal);
                    }
                    else
                    {
                        // Invalid move, play the conflict sound
                        PlaySoundEffect(SoundEffect.Conflict);
                    }
                    break;
                case Key.B: // B
                case Key.F: // F
                case Key.S: // S
                case Key.W: // W
                    // Switch to the specified board
                    PrepareFade();
                    var boardString = "";
                    switch (e.Key)
                    {
                        case Key.B: boardString = BoardBlank; break;
                        case Key.F: boardString = BoardAlmostFinished; break;
                        case Key.S: boardString = BoardSudopediaSample; break;
                        case Key.W: boardString = BoardWikipediaSample; break;
                    }
                    _primaryBoardDisplay.Board = Board.FromString(boardString);
                    PlaySoundEffect(SoundEffect.New);
                    _fadingBoardDisplay.Fade(FadeSecondsNormal);
                    break;
                case Key.C: // C
                    // Toggle the candidate display
                    PrepareFade();
                    _primaryBoardDisplay.CandidatesVisible = !_primaryBoardDisplay.CandidatesVisible;
                    _fadingBoardDisplay.Fade(FadeSecondsNormal);
                    break;
            }
        }

        private void HandleMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            // Set the marker position to the cell under the specified location
            var position = e.GetPosition(null);
            var markerPosition = new Point { X = (position.X / Width) * Board.Size, Y = (position.Y / Height) * Board.Size };
            _primaryBoardDisplay.MarkerPosition = markerPosition;
            _fadingBoardDisplay.MarkerPosition = markerPosition;
        }

        // Prepare to fade by syncing _fadingBoardDisplay with _primaryBoardDisplay
        private void PrepareFade()
        {
            _fadingBoardDisplay.Board = _primaryBoardDisplay.Board;
            _fadingBoardDisplay.CandidatesVisible = _primaryBoardDisplay.CandidatesVisible;
        }

        private void PlaySoundEffect(SoundEffect soundEffect)
        {
            // Prepare to play the sound
            mediaElement.Volume = _defaultVolume;
            var soundFile = "";
            switch (soundEffect)
            {
                case SoundEffect.New:
                    soundFile = "WindowsLogonSound.wma";
                    break;
                case SoundEffect.Move:
                    soundFile = "WindowsNavigationStart.wma";
                    mediaElement.Volume *= 0.5;  // Volume for this sound is lowered some to keep it the same as the others
                    break;
                case SoundEffect.Conflict:
                    soundFile = "WindowsCriticalStop.wma";
                    break;
                case SoundEffect.Complete:
                    soundFile = "Tada.wma";
                    mediaElement.Volume = 1.0;  // Success sound is full volume to celebrate
                    break;
            }
            // Set the source and play the sound
            mediaElement.Source = new Uri(HtmlPage.Document.DocumentUri, soundFile);
            mediaElement.Play();
        }

        private void HandleResize(object sender, EventArgs e)
        {
            // If this is a valid resize
            if ((0 < BrowserHost.ActualWidth) && (0 < BrowserHost.ActualHeight))
            {
                // Size the root element to fill the host
                var root = _primaryBoardDisplay.Parent as Canvas;
                root.Width = BrowserHost.ActualWidth;
                root.Height = BrowserHost.ActualHeight;
                // Size each board to match
                foreach (var boardDisplay in new BoardDisplay[] { _primaryBoardDisplay, _fadingBoardDisplay })
                {
                    boardDisplay.Width = BrowserHost.ActualWidth;
                    boardDisplay.Height = BrowserHost.ActualHeight;
                    boardDisplay.Layout();
                }
            }
        }
    }
}
