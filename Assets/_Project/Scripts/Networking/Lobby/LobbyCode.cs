using System;

namespace Nova.Networking.Lobby
{
    /// <summary>
    /// Human-typed match codes of the sprint-14 lobby (D-092): six characters
    /// from a 32-symbol alphabet that excludes the look-alikes 0/O and 1/I/L,
    /// displayed as "XXX-XXX". Pure string logic, shared between the UI (input
    /// validation) and <see cref="LobbyClient"/> (normalization before send).
    /// </summary>
    public static class LobbyCode
    {
        /// <summary>Allowed characters: A-Z minus I/L/O, digits 2-9.</summary>
        public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>Characters without the display dash.</summary>
        public const int Length = 6;

        /// <summary>
        /// Lenient parse of user input: trims whitespace, accepts any case and
        /// an optional dash, and produces the canonical "XXX-XXX" form.
        /// Rejects wrong lengths, misplaced dashes and characters outside
        /// <see cref="Alphabet"/>.
        /// </summary>
        public static bool TryNormalize(string input, out string normalized)
        {
            normalized = null;
            if (input == null)
            {
                return false;
            }

            string trimmed = input.Trim();
            string six;
            if (trimmed.Length == Length)
            {
                six = trimmed;
            }
            else if (trimmed.Length == Length + 1 && trimmed[3] == '-')
            {
                six = trimmed.Substring(0, 3) + trimmed.Substring(4, 3);
            }
            else
            {
                return false;
            }

            char[] upper = new char[Length];
            for (int i = 0; i < Length; i++)
            {
                char c = char.ToUpperInvariant(six[i]);
                if (Alphabet.IndexOf(c) < 0)
                {
                    return false;
                }
                upper[i] = c;
            }

            normalized = new string(upper, 0, 3) + "-" + new string(upper, 3, 3);
            return true;
        }

        /// <summary>Forgiving validity check, same acceptance rules as <see cref="TryNormalize"/>.</summary>
        public static bool IsValidCode(string input)
        {
            return TryNormalize(input, out _);
        }

        /// <summary>Canonical "XXX-XXX" display form of six code characters (any case).</summary>
        /// <exception cref="ArgumentException">Not six characters of <see cref="Alphabet"/>.</exception>
        public static string Format(string sixChars)
        {
            if (!TryNormalize(sixChars, out string normalized))
            {
                throw new ArgumentException("A match code is six characters of " + Alphabet + ".", nameof(sixChars));
            }
            return normalized;
        }
    }
}
