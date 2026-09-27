using Xunit;

namespace BarbershopReservationsUni.Services.Tests;

public class PhoneNormalizerTests
{
    [Theory]
    [InlineData("0888 123 456", "0888123456")]
    [InlineData("0888-123-456", "0888123456")]
    [InlineData("(0888) 123 456", "0888123456")]
    [InlineData("+359 888 123 456", "0888123456")]
    [InlineData("00359888123456", "0888123456")]
    [InlineData("359888123456", "0888123456")]
    [InlineData("  0888123456  ", "0888123456")]
    public void Normalize_ProducesNationalFormat(string input, string expected) =>
        Assert.Equal(expected, PhoneNormalizer.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_EmptyInput_ReturnsEmpty(string? input) =>
        Assert.Equal(string.Empty, PhoneNormalizer.Normalize(input));

    [Theory]
    [InlineData("0888123456", true)]
    [InlineData("+359888123456", true)]
    [InlineData("029876543", true)]
    [InlineData("0888", false)]
    [InlineData("abcdefghij", false)]
    [InlineData("+1 202 555 0100", false)]
    [InlineData("", false)]
    public void IsValid_AcceptsBulgarianNumbersOnly(string input, bool expected) =>
        Assert.Equal(expected, PhoneNormalizer.IsValid(input));

    [Fact]
    public void Mask_HidesTheMiddle() =>
        Assert.Equal("0888***456", PhoneNormalizer.Mask("0888 123 456"));
}

public class OtpHasherTests
{
    [Fact]
    public void GenerateCode_IsAlwaysSixDigits()
    {
        for (var i = 0; i < 500; i++)
            Assert.Matches(@"^\d{6}$", OtpHasher.GenerateCode());
    }

    [Fact]
    public void Verify_AcceptsCorrectAndRejectsWrongValues()
    {
        var secret = new string('s', 40);
        var hash = OtpHasher.Hash(secret, "0888123456", "123456");

        Assert.True(OtpHasher.Verify(secret, "0888123456", "123456", hash));
        Assert.False(OtpHasher.Verify(secret, "0888123456", "654321", hash));
        Assert.False(OtpHasher.Verify(secret, "0899000111", "123456", hash));
        Assert.False(OtpHasher.Verify(new string('t', 40), "0888123456", "123456", hash));
    }
}
