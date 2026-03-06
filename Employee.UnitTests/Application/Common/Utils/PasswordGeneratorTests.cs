using Employee.Application.Common.Utils;
using System;

namespace Employee.UnitTests.Application.Common.Utils
{
  public class PasswordGeneratorTests
  {
    // ─── Length contract ──────────────────────────────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(32)]
    [InlineData(128)]
    public void Generate_ShouldReturnPasswordOfExactRequestedLength(int length)
    {
      var password = PasswordGenerator.Generate(length);
      Assert.Equal(length, password.Length);
    }

    [Fact]
    public void Generate_DefaultLength_ShouldBeTwelveCharacters()
    {
      var password = PasswordGenerator.Generate();
      Assert.Equal(12, password.Length);
    }

    [Fact]
    public void Generate_LengthBelowMinimum_ShouldThrowArgumentOutOfRangeException()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.Generate(7));
    }

    [Fact]
    public void Generate_LengthAboveMaximum_ShouldThrowArgumentOutOfRangeException()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.Generate(129));
    }

    [Fact]
    public void Generate_LengthZero_ShouldThrowArgumentOutOfRangeException()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.Generate(0));
    }

    // ─── Character class constraints ──────────────────────────────────────

    [Fact]
    public void Generate_ShouldContainAtLeastOneLowercaseLetter()
    {
      // Run multiple times to reduce flakiness probability
      for (int i = 0; i < 20; i++)
      {
        var password = PasswordGenerator.Generate();
        Assert.True(password.Any(char.IsLower),
            $"No lowercase letter in: {password}");
      }
    }

    [Fact]
    public void Generate_ShouldContainAtLeastOneUppercaseLetter()
    {
      for (int i = 0; i < 20; i++)
      {
        var password = PasswordGenerator.Generate();
        Assert.True(password.Any(char.IsUpper),
            $"No uppercase letter in: {password}");
      }
    }

    [Fact]
    public void Generate_ShouldContainAtLeastOneDigit()
    {
      for (int i = 0; i < 20; i++)
      {
        var password = PasswordGenerator.Generate();
        Assert.True(password.Any(char.IsDigit),
            $"No digit in: {password}");
      }
    }

    [Fact]
    public void Generate_WithNonAlphanumericCount2_ShouldContainAtLeastTwoSpecialChars()
    {
      string punctuations = "!@#$%^&*()_-+=[{]};:>|./?";
      for (int i = 0; i < 20; i++)
      {
        var password = PasswordGenerator.Generate(numberOfNonAlphanumericCharacters: 2);
        int specialCount = password.Count(c => punctuations.Contains(c));
        Assert.True(specialCount >= 2,
            $"Expected ≥2 special chars, found {specialCount} in: {password}");
      }
    }

    [Fact]
    public void Generate_WithZeroNonAlphanumeric_ShouldStillMeetLength()
    {
      var password = PasswordGenerator.Generate(12, 0);
      Assert.Equal(12, password.Length);
    }

    // ─── Uniqueness / randomness ──────────────────────────────────────────

    [Fact]
    public void Generate_CalledTwice_ShouldProduceDifferentPasswords()
    {
      var p1 = PasswordGenerator.Generate();
      var p2 = PasswordGenerator.Generate();

      // Statistically the probability of two identical 12-char passwords is negligible
      Assert.NotEqual(p1, p2);
    }

    [Fact]
    public void Generate_BulkCallsShouldBeUnique()
    {
      var passwords = Enumerable.Range(0, 50)
          .Select(_ => PasswordGenerator.Generate())
          .ToHashSet();

      // With 50 passwords there should be no collisions
      Assert.Equal(50, passwords.Count);
    }

    // ─── Valid character set ──────────────────────────────────────────────

    [Fact]
    public void Generate_ShouldOnlyContainPrintableAsciiCharacters()
    {
      for (int i = 0; i < 20; i++)
      {
        var password = PasswordGenerator.Generate();
        Assert.True(password.All(c => c >= 33 && c <= 126),
            $"Non-printable char found in: {password}");
      }
    }
  }
}
