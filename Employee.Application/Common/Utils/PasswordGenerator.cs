using System.Security.Cryptography;
using System.Text;

namespace Employee.Application.Common.Utils
{
    public static class PasswordGenerator
    {
        private static readonly char[] Punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

        public static string Generate(int length = 12, int numberOfNonAlphanumericCharacters = 2)
        {
      if (length < 8 || length > 128) throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 8 and 128");

      using var rng = RandomNumberGenerator.Create();

      var chars = new List<char>
            {
                GetRandomChar("abcdefghijklmnopqrstuvwxyz", rng),
                GetRandomChar("ABCDEFGHIJKLMNOPQRSTUVWXYZ", rng),
                GetRandomChar("0123456789", rng)
            };

      for (int i = 0; i < numberOfNonAlphanumericCharacters; i++)
      {
        chars.Add(GetRandomChar(new string(Punctuations), rng));
      }

      string allChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" + new string(Punctuations);
      while (chars.Count < length)
      {
        chars.Add(GetRandomChar(allChars, rng));
            }

      // Shuffle the characters
      for (int i = chars.Count - 1; i > 0; i--)
            {
        var rngBytes = new byte[4];
        rng.GetBytes(rngBytes);
        int j = (int)(BitConverter.ToUInt32(rngBytes, 0) % (uint)(i + 1));

        var temp = chars[i];
        chars[i] = chars[j];
        chars[j] = temp;
      }

      return new string(chars.ToArray());
    }

    private static char GetRandomChar(string validChars, RandomNumberGenerator rng)
    {
      // Dùng RandomNumberGenerator.GetInt32 để tránh modulo bias
      return validChars[RandomNumberGenerator.GetInt32(validChars.Length)];
        }
    }
}
