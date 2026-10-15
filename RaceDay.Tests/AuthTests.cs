using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Raceday_API__prog_part_2_.Data;
using Raceday_API__prog_part_2_.Dtos;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public AuthTests(ApiFactory f) => _f = f;

    private static RegisterRequest Org(string email) => new()
    { Email = email, Password = "Password123!", FullName = "Org One", Role = "Organiser", OrganisationName = "Club" };

    [Fact]
    public async Task Register_Organiser_Returns201()
    {
        var res = await _f.CreateClient().PostAsJsonAsync("/api/auth/register", Org("t1@test.com"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var c = _f.CreateClient();
        await c.PostAsJsonAsync("/api/auth/register", Org("dup@test.com"));
        var res = await c.PostAsJsonAsync("/api/auth/register", Org("dup@test.com"));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var req = Org("badrole@test.com");
        req.Role = "Admin";
        var res = await _f.CreateClient().PostAsJsonAsync("/api/auth/register", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ParticipantWithoutUsername_Returns400()
    {
        var req = new RegisterRequest { Email = "nouser@test.com", Password = "Password123!", FullName = "P", Role = "Participant" };
        var res = await _f.CreateClient().PostAsJsonAsync("/api/auth/register", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Password_IsStoredHashed()
    {
        await _f.CreateClient().PostAsJsonAsync("/api/auth/register", Org("hash@test.com"));
        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = db.Organisers.Single(o => o.Email == "hash@test.com").PasswordHash;
        Assert.NotEqual("Password123!", stored);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndRole()
    {
        var c = _f.CreateClient();
        await c.PostAsJsonAsync("/api/auth/register", Org("login@test.com"));
        var res = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "login@test.com", Password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Organiser", body.User.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var c = _f.CreateClient();
        await c.PostAsJsonAsync("/api/auth/register", Org("wrongpw@test.com"));
        var res = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "wrongpw@test.com", Password = "nope-nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var res = await _f.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}