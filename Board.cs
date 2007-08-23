using System.Collections.Generic;

namespace SilverlightSudokuHelper
{
    internal class Board
    {
        // Board is 9x9
        public const int Size = 9;

        // 9x9 array of digits
        private Digit[,] _digits;

        public Board()
        {
            // Create blank board
            Initialize(null);
        }

        public Board(Board basis)
        {
            // Copy specified board
            Initialize(basis);
        }

        private void Initialize(Board basis)
        {
            // Initialize each digit
            _digits = new Digit[Size, Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    _digits[x, y] = new Digit();
                }
            }
            if (null != basis)
            {
                // Copy the digits of the specified board
                for (var y = 0; y < Size; y++)
                {
                    for (var x = 0; x < Size; x++)
                    {
                        var digit = basis.GetDigit(x, y);
                        if (digit.ValueKnown)
                        {
                            SetValue(x, y, digit.KnownValue, digit.DigitKind);
                        }
                    }
                }
            }
        }
        private IEnumerable<string> GetDigitList()
        {
            foreach (var digit in _digits)
            {
                yield return digit.ValueKnown ? digit.KnownValue.ToString() : ".";
            }
        }
        public void Solve()
        {
            var digitList=string.Join("", new List<string>(GetDigitList()).ToArray());
            var grid=LinqSudokuSolver.parse_grid(digitList);
            var solution=LinqSudokuSolver.search(grid);

            var rowIdentifiers="ABCDEFGHI";
            for(var row=0; row<Size; row++) {

                for(var col=0; col<Size; col++) {
                    var key = rowIdentifiers[row] + (col+1).ToString();
                    var solvedValue=int.Parse(solution[key]);

                    _digits[row, col].ClearValue();
                    _digits[row, col].SetValue(solvedValue, Digit.Kind.Given);
                }
            }
        }
        public Digit GetDigit(int x, int y)
        {
            // Return the specified digit
            return _digits[x, y];
        }

        public void SetValue(int x, int y, int value, Digit.Kind kind)
        {
            // Save the current digits in case something goes wrong
            var savedDigits = DuplicateDigits();
            try
            {
                try
                {
                    // Attempt to set the specified value
                    _digits[x, y].SetValue(value, kind);
                }
                catch (InvalidDigitException)
                {
                    // Digit was invalid, so board would have become invalid
                    throw new InvalidBoardException(x, y);
                }
                // Exclude the value from the rest of the row and column
                for (var i = 0; i < Size; i++)
                {
                    if (i != x)
                    {
                        Exclude(i, y, value);
                    }
                    if (i != y)
                    {
                        Exclude(x, i, value);
                    }
                }
                // Exclude the value from the rest of the block
                for (var j = (y / 3) * 3; j < ((y / 3) * 3) + 3; j++)
                {
                    for (var i = (x / 3) * 3; i < ((x / 3) * 3) + 3; i++)
                    {
                        if ((i != x) || (j != y))
                        {
                            Exclude(i, j, value);
                        }
                    }
                }
            }
            catch (InvalidBoardException)
            {
                // Recover from invalid board and re-throw
                _digits = savedDigits;
                throw;
            }
        }

        public void ClearValue(int x, int y)
        {
            // Clear the specified value
            _digits[x, y].ClearValue();
            // Create a new board to apply all the exclusions properly (vs. attempting to undo them)
            var board = new Board(this);
            // Take over the new board's digits
            _digits = board._digits;
        }

        public bool Complete
        {
            get
            {
                // true iff all values are known
                var complete = true;
                foreach (Digit digit in _digits)
                {
                    if (!digit.ValueKnown)
                    {
                        complete = false;
                        break;
                    }
                }
                return complete;
            }
        }

        private void Exclude(int x, int y, int value)
        {
            try
            {
                // Exclude the value from the specified location
                _digits[x, y].Exclude(value);
            }
            catch (InvalidDigitException)
            {
                // Digit was invalid, so board would have become invalid
                throw new InvalidBoardException(x, y);
            }
        }

        private Digit[,] DuplicateDigits()
        {
            // Return a copy of the _digits array
            var digits = new Digit[Size, Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    digits[x, y] = new Digit(_digits[x, y]);
                }
            }
            return digits;
        }

        public static Board FromString(string basis)
        {
            // Create a new board from an 81-character string that specifies the board's values left-to-right, top-to-bottom
            var board = new Board();
            int i = 0;
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (' ' != basis[i])
                    {
                        board.SetValue(x, y, basis[i] - '0', Digit.Kind.Given);
                    }
                    i++;
                }
            }
            return board;
        }
    }
}
