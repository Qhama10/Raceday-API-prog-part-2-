using System.Net;
using System.Net.Http.Json;
using Raceday_API__prog_part_2_.Dtos;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public EventTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", Helpers.SampleEvent());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidType_Returns400()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var req = Helpers.SampleEvent();
        req.EventType = "Swim";
        var res = await org.PostAsJsonAsync("/api/events", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var ev = await Helpers.CreateEvent(org);
        var res = await org.PutAsJsonAsync($"/api/events/{ev.EventId}", Helpers.SampleEvent("Renamed"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed", body!.EventName);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var ev = await Helpers.CreateEvent(org);
        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotChangeAnotherOrganisersEvent()
    {
        var owner = await Helpers.LoggedIn(_f, "Organiser");
        var other = await Helpers.LoggedIn(_f, "Organiser");
        var ev = await Helpers.CreateEvent(owner);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.PutAsJsonAsync($"/api/events/{ev.EventId}", Helpers.SampleEvent("Hijack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task UpdateMissingEvent_Returns404()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var res = await org.PutAsJsonAsync("/api/events/99999", Helpers.SampleEvent());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents_ButCannotManageThem()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);

        Assert.Equal(HttpStatusCode.OK, (await part.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await part.GetAsync($"/api/events/{ev.EventId}")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await part.PostAsJsonAsync("/api/events", Helpers.SampleEvent())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await part.PutAsJsonAsync($"/api/events/{ev.EventId}", Helpers.SampleEvent())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task Categories_OrganiserManages_ParticipantOnlyViews()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);
        var body = new CategoryRequest { CategoryName = "Under 20", CategoryDescription = "Youth", CategoryDistance = "10km" };

        Assert.Equal(HttpStatusCode.Forbidden, (await part.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", body)).StatusCode);
        var created = await org.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var cat = (await created.Content.ReadFromJsonAsync<CategoryResponse>())!;

        Assert.Equal(HttpStatusCode.OK, (await part.GetAsync($"/api/events/{ev.EventId}/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.DeleteAsync($"/api/categories/{cat.CategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/categories/{cat.CategoryId}")).StatusCode);
    }
}