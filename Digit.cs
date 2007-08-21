using System;

namespace SilverlightSudokuHelper
{
    internal class Digit
    {
        public enum Kind { Normal = 0, Given = given, Guess = guess };
        // Digit Unknown state could be any value
        public const int Unknown = (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5) | (1 << 6) | (1 << 7) | (1 << 8) | (1 << 9);

        // Additional bits track state (lower-case to avoid conflicts)
        private const int known = (1 << 0);
        private const int given = (1 << 10);
        private const int guess = (1 << 11);

        // Bitmask
        private int _bits;

        public Digit()
        {
            // Create an empty digit
            ClearValue();
        }

        public Digit(Digit digit)
        {
            // Copy the specified digit
            _bits = digit._bits;
        }

        public void ClearValue()
        {
            // Reset the digit's bits
            _bits = Unknown;
        }

        public void SetValue(int value, Kind kind)
        {
            if (ValueKnown && (value != KnownValue))
            {
                // Can't change the value of a known digit (but can change its kind)
                throw new InvalidDigitException();
            }
            // Set bits to reflect known value of specified kind
            _bits = (1 << value) | known | (int)kind;
        }

        public Kind DigitKind
        {
            get
            {
                // The kind of digit
                Kind kind;
                if (0 != (given & _bits))
                {
                    kind = Kind.Given;
                }
                else if (0 != (guess & _bits))
                {
                    kind = Kind.Guess;
                }
                else
                {
                    kind = Kind.Normal;
                }
                return kind;
            }
        }

        public bool ValueKnown
        {
            // true iff the digit's value is known
            get { return (0 != (known & _bits)); }
        }

        public int KnownValue
        {
            get
            {
                // Loop to figure out which value is being stored
                for (var i = 1; i <= 9; i++)
                {
                    if ((_bits & ~given & ~guess) == ((1 << i) | known))
                    {
                        return i;
                    }
                }
                // Error to call KnownValue if the value isn't known
                throw new InvalidOperationException();
            }
        }

        public void Exclude(int value)
        {
            // Mask-out the excluded value
            _bits &= ~(1 << value);
            // Invalid digit if no more values are possible
            if (0 == (_bits & ~known & ~given & ~guess))
            {
                throw new InvalidDigitException();
            }
        }

        public bool CouldBe(int value)
        {
            // true iff the digit could be the specified value
            return (0 != (_bits & (1 << value)));
        }
    }
}
