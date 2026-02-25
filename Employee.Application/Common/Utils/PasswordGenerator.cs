using System.Security.Cryptography;
using System.Text;

namespace Employee.Application.Common.Utils
{
    public static class PasswordGenerator
    {
        private static readonly char[] Punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

        public static string Generate(int length = 12, int numberOfNonAlphanumericCharacters = 2)
        {
            if (length < 1 || length > 128) throw new ArgumentOutOfRangeException(nameof(length));
            if (numberOfNonAlphanumericCharacters > length) throw new ArgumentOutOfRangeException(nameof(numberOfNonAlphanumericCharacters));

            using var rng = RandomNumberGenerator.Create();
            var byteBuffer = new byte[length];
            rng.GetBytes(byteBuffer);

            var characterBuffer = new char[length];

            for (var iter = 0; iter < length; iter++)
            {
                var i = byteBuffer[iter] % 62;

                if (i < 10)
                {
                    characterBuffer[iter] = (char)('0' + i);
                }
                else if (i < 36)
                {
                    characterBuffer[iter] = (char)('A' + i - 10);
                }
                else
                {
                    characterBuffer[iter] = (char)('a' + i - 36);
                }
            }

            if (numberOfNonAlphanumericCharacters > 0)
            {
                for (var iter = 0; iter < numberOfNonAlphanumericCharacters; iter++)
                {
                    int pos;
                    do
                    {
                        var indexByte = new byte[1];
                        rng.GetBytes(indexByte);
                        pos = indexByte[0] % length;
                    } while (!char.IsLetterOrDigit(characterBuffer[pos]));

                    var puncByte = new byte[1];
                    rng.GetBytes(puncByte);
                    characterBuffer[pos] = Punctuations[puncByte[0] % Punctuations.Length];
                }
            }

            return new string(characterBuffer);
        }
    }
}
