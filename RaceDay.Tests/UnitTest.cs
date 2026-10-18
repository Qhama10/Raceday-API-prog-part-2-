using Raceday_API__prog_part_2_.Services;

namespace RaceDay.Tests;

public class LocationLookupTests
{
    [Theory]
    [InlineData("Durban", "Durban")]
    [InlineData("Moses Mabhida Stadium, durban", "Durban")]
    [InlineData("SOWETO", "Soweto")]
    [InlineData("Cape Town Waterfront", "Cape Town")]
    public void Find_KnownPlace_ReturnsMatchingCoordinates(string input, string expected)
    {
        var place = LocationLookup.Find(input);
        Assert.NotNull(place);
        Assert.Equal(expected, place!.Name);
    }

    [Theory]
    [InlineData("Somewhere Unlisted")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Find_UnknownOrEmpty_ReturnsNull(string? input)
    {
        Assert.Null(LocationLookup.Find(input));
    }

    [Fact]
    public void Find_Durban_HasSouthernHemisphereCoordinates()
    {
        var place = LocationLookup.Find("Durban")!;
        Assert.InRange(place.Latitude, -31, -29);
        Assert.InRange(place.Longitude, 30, 32);
    }
}

public class PasswordHashingTests
{
    [Fact]
    public void BCrypt_HashIsNotThePlainPassword_AndVerifies()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        Assert.NotEqual("Password123!", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("WrongPassword", hash));
    }

    [Fact]
    public void BCrypt_SamePassword_GivesDifferentHashes()
    {
        var a = BCrypt.Net.BCrypt.HashPassword("Password123!");
        var b = BCrypt.Net.BCrypt.HashPassword("Password123!");
        Assert.NotEqual(a, b);
    }
}