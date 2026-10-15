using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raceday_API__prog_part_2_.Data;
using Raceday_API__prog_part_2_.Dtos;

namespace RaceDay.Tests;

// Starts your real API in memory, but swaps SQL Server for a throwaway in-memory database.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _db = "Tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>)).ToList())
                services.Remove(d);
            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_db));
        });
    }
}

public static class Helpers
{
    private static int _n;

    // Registers a user, logs in, and returns a client that already carries the token.
    public static async Task<HttpClient> LoggedIn(ApiFactory f, string role)
    {
        var i = Interlocked.Increment(ref _n);
        var email = $"{role.ToLower()}{i}@test.com";
        var c = f.CreateClient();

        var reg = await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        { Email = email, Password = "Password123!", FullName = "Test User", Role = role, Username = $"user{i}" });
        reg.EnsureSuccessStatusCode();

        var login = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Password123!" });
        var body = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return c;
    }
    public static EventRequest SampleEvent(string name = "Test Marathon") => new()
    {
        EventName = name,
        EventDescription = "desc",
        EventDate = new DateOnly(2030, 5, 10),
        EventLocation = "Durban",
        Distance = 42.2m,
        EventType = "Run"
    };

    public static async Task<EventResponse> CreateEvent(HttpClient organiser, string name = "Test Marathon")
    {
        var res = await organiser.PostAsJsonAsync("/api/events", SampleEvent(name));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategory(HttpClient organiser, int eventId, string name = "10km")
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CategoryRequest { CategoryName = name, CategoryDescription = "d", CategoryDistance = "10km" });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }
}