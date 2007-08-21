using System;

namespace SilverlightSudokuHelper
{
    // Thrown when an invalid digit is created
    internal class InvalidDigitException : Exception
    {
    }

    // Thrown when an invalid board is created
    internal class InvalidBoardException : Exception
    {
        public readonly int X;
        public readonly int Y;

        public InvalidBoardException(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
