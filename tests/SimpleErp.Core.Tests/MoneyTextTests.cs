using System.Globalization;
using SimpleErp.Core.ViewModels;

namespace SimpleErp.Core.Tests;

public class MoneyTextTests
{
    private static readonly CultureInfo English = new("en-US");
    private static readonly CultureInfo Russian = new("ru-RU");

    [Theory]
    [InlineData("6200", 6200)]
    [InlineData("6,200.50", 6200.50)]
    [InlineData(" $7,000 ", 7000)]
    [InlineData("1 250", 1250)]
    public void Parses_english_input(string text, decimal expected)
    {
        Assert.True(MoneyText.TryParse(text, out var value, English));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("6 200,50", 6200.50)]
    [InlineData("6 200", 6200)]
    [InlineData("6,200.50", 6200.50)]
    public void Parses_russian_input_and_falls_back_to_invariant(string text, decimal expected)
    {
        Assert.True(MoneyText.TryParse(text, out var value, Russian));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(null)]
    public void Rejects_non_numbers(string? text) => Assert.False(MoneyText.TryParse(text, out _, English));

    [Fact]
    public void Formats_with_group_separators_and_no_trailing_zeros()
    {
        Assert.Equal("6,200", MoneyText.Format(6200m, English));
        Assert.Equal("6,200.5", MoneyText.Format(6200.50m, English));
    }
}
