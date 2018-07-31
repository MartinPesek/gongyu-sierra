using System;
using System.Text;

namespace Web.Services
{
    public static class StringGenerator
    {
        private const int DefaultStringLength = 8;

        private static readonly char[] Vowels = {'a', 'e', 'y', 'u'};
        private static readonly char[] Consonants = {'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z'};
        private static readonly char[] Numbers = {'2', '3', '4', '5', '6', '7', '8', '9'};

        private enum GeneratorMode
        {
            Consonant,
            Vowel,
            VowelSecond,
            Number
        }

        public static string GetRandomString(int length = DefaultStringLength)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Value cannot be negative.");
            }

            var random = new Random(Guid.NewGuid().GetHashCode());

            var result = new StringBuilder();

            var mode = GeneratorMode.Consonant;

            for (var i = 0; i < length; i++)
            {
                switch (mode)
                {
                    case GeneratorMode.Consonant:
                        result.Append(Consonants[random.Next(Consonants.Length)]);
                        mode = GeneratorMode.Vowel;
                        break;

                    case GeneratorMode.Vowel:
                        result.Append(Vowels[random.Next(Vowels.Length)]);
                        mode = random.Next(2) == 1 ? GeneratorMode.VowelSecond : GeneratorMode.Number;
                        break;

                    case GeneratorMode.VowelSecond:
                        result.Append(Vowels[random.Next(Vowels.Length)]);
                        mode = GeneratorMode.Consonant;
                        break;

                    case GeneratorMode.Number:
                        result.Append(Numbers[random.Next(Numbers.Length)]);
                        mode = GeneratorMode.Consonant;
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            return result.ToString();
        }
    }
}