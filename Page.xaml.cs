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

        public void Page_Loaded(object o, EventArgs e)
        {
            // Initialize variables
            InitializeComponent();
            _defaultVolume = mediaElement.Volume;

            // Initialize UI
            _primaryBoardDisplay = new BoardDisplay();
            Children.Add(_primaryBoardDisplay);
            _fadingBoardDisplay = new BoardDisplay();
            Children.Add(_fadingBoardDisplay);

            // Initialize handlers
            KeyUp += new KeyboardEventHandler(HandleKeyUp);
            MouseLeftButtonDown += new MouseEventHandler(HandleMouseLeftButtonDown);
            BrowserHost.Resize += new EventHandler(HandleResize);

            // Create the starting board, play the "new" sound, and fade it in
            _primaryBoardDisplay.Board = Board.FromString(BoardWikipediaSample);
            PlaySoundEffect(SoundEffect.New);
            _fadingBoardDisplay.Fade(FadeSecondsLoading);
        }

        private void HandleKeyUp(object sender, KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case 8: // Escape
                    _primaryBoardDisplay.Solve();
                    break;
                case 14: // Left arrow
                case 15: // Up arrow
                case 16: // Right arrow
                case 17: // Down arrow
                    // Move the marker
                    var markerPosition = _primaryBoardDisplay.MarkerPosition;
                    switch (e.Key)
                    {
                        case 14: markerPosition.X--; break;
                        case 15: markerPosition.Y--; break;
                        case 16: markerPosition.X++; break;
                        case 17: markerPosition.Y++; break;
                    }
                    _primaryBoardDisplay.MarkerPosition = markerPosition;
                    _fadingBoardDisplay.MarkerPosition = markerPosition;
                    break;
                case 19: // Delete
                    // Clear the cell's value
                    PrepareFade();
                    if (_primaryBoardDisplay.ChangeSelectedValue(Digit.Unknown, Digit.Kind.Normal))
                    {
                        // Play the appropriate sound and fade it
                        PlaySoundEffect(SoundEffect.Move);
                        _fadingBoardDisplay.Fade(FadeSecondsNormal);
                    }
                    break;
                case 21: // 1
                case 22: // 2
                case 23: // 3
                case 24: // 4
                case 25: // 5
                case 26: // 6
                case 27: // 7
                case 28: // 8
                case 29: // 9
                    // Set the cell's value
                    PrepareFade();
                    if (_primaryBoardDisplay.ChangeSelectedValue(e.Key - 20, (e.Shift ? Digit.Kind.Given : (e.Ctrl ? Digit.Kind.Guess : Digit.Kind.Normal))))
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
                case 31: // B
                case 35: // F
                case 48: // S
                case 52: // W
                    // Switch to the specified board
                    PrepareFade();
                    var boardString = "";
                    switch (e.Key)
                    {
                        case 31: boardString = BoardBlank; break;
                        case 35: boardString = BoardAlmostFinished; break;
                        case 48: boardString = BoardSudopediaSample; break;
                        case 52: boardString = BoardWikipediaSample; break;
                    }
                    _primaryBoardDisplay.Board = Board.FromString(boardString);
                    PlaySoundEffect(SoundEffect.New);
                    _fadingBoardDisplay.Fade(FadeSecondsNormal);
                    break;
                case 32: // C
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
            mediaElement.Source = new Uri(HtmlPage.DocumentUri, soundFile);
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
