using Raceday_API__prog_part_2_.Dtos;
using System.Net;
using System.Net.Http.Json;
using static Raceday_API__prog_part_2_.Data.RaceDayDbContext;

namespace RaceDay.Tests;

public class MapWeatherTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public MapWeatherTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task Map_ForKnownLocation_ReturnsCoordinatesAndLinks()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var ev = await Helpers.CreateEvent(org);          // location is Durban

        var res = await org.GetAsync($"/api/events/{ev.EventId}/map");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var map = await res.Content.ReadFromJsonAsync<EventMapResponse>();
        Assert.Equal("Durban", map!.KnownLocation);
        Assert.Contains("google.com/maps", map.GoogleMapsUrl);
        Assert.Contains("openstreetmap.org", map.EmbedUrl);
    }

    [Fact]
    public async Task Weather_ForKnownLocation_ReturnsWeather()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var ev = await Helpers.CreateEvent(org);

        var res = await org.GetAsync($"/api/events/{ev.EventId}/weather");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var w = await res.Content.ReadFromJsonAsync<EventWeatherResponse>();
        Assert.Equal(24.5, w!.TemperatureC);
        Assert.Equal("Open-Meteo", w.Source);
    }

    [Fact]
    public async Task Map_ForUnknownLocation_Returns404()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var req = Helpers.SampleEvent("Mystery Run");
        req.EventLocation = "Somewhere Unlisted";
        var created = await (await org.PostAsJsonAsync("/api/events", req)).Content.ReadFromJsonAsync<EventResponse>();

        var res = await org.GetAsync($"/api/events/{created!.EventId}/map");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Weather_WhenServiceIsDown_Returns503()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var ev = await Helpers.CreateEvent(org);

        _f.Weather.Fail = true;
        try
        {
            var res = await org.GetAsync($"/api/events/{ev.EventId}/weather");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
        }
        finally { _f.Weather.Fail = false; }
    }

    [Fact]
    public async Task Map_MissingEvent_Returns404()
    {
        var part = await Helpers.LoggedIn(_f, "Participant");
        var res = await part.GetAsync("/api/events/99999/map");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Map_WithoutToken_Returns401()
    {
        var anon = _f.CreateClient();
        var res = await anon.GetAsync("/api/events/1/map");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}