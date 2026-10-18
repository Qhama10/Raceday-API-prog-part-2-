using System.Net;
using System.Net.Http.Json;
using Raceday_API__prog_part_2_.Dtos;


namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public EnrolmentTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);
        var cat = await Helpers.CreateCategory(org, ev.EventId);

        var res = await part.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = cat.CategoryId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var mine = await (await part.GetAsync("/api/users/me/enrolments")).Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        var e = Assert.Single(mine!);
        Assert.Equal(ev.EventId, e.EventId);
        Assert.Equal(cat.CategoryId, e.CategoryId);
        Assert.Equal("Registered", e.Status);
    }

    [Fact]
    public async Task Enrolling_TwiceInSameEvent_Returns400()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);
        var c1 = await Helpers.CreateCategory(org, ev.EventId, "10km");
        var c2 = await Helpers.CreateCategory(org, ev.EventId, "21km");

        await part.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = c1.CategoryId });
        var res = await part.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = c2.CategoryId });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_Returns400()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev1 = await Helpers.CreateEvent(org, "A");
        var ev2 = await Helpers.CreateEvent(org, "B");
        var catOfEv2 = await Helpers.CreateCategory(org, ev2.EventId);

        var res = await part.PostAsJsonAsync($"/api/events/{ev1.EventId}/enrol", new EnrolRequest { CategoryId = catOfEv2.CategoryId });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_MissingEvent_Returns404()
    {
        var part = await Helpers.LoggedIn(_f, "Participant");
        var res = await part.PostAsJsonAsync("/api/events/99999/enrol", new EnrolRequest { CategoryId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Roles_AreRejectedOnEnrolmentEndpoints()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);
        var cat = await Helpers.CreateCategory(org, ev.EventId);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await org.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = cat.CategoryId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.GetAsync($"/api/events/{ev.EventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/users/me/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/users/me/results")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolments_OnlyForOwnEvent()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var other = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);
        var cat = await Helpers.CreateCategory(org, ev.EventId);
        await part.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = cat.CategoryId });

        var list = await (await org.GetAsync($"/api/events/{ev.EventId}/enrolments")).Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Single(list!);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{ev.EventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Organiser_RecordsResult_AndParticipantSeesOwnResultOnly()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var p1 = await Helpers.LoggedIn(_f, "Participant");
        var p2 = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);
        var cat = await Helpers.CreateCategory(org, ev.EventId);

        var e1 = await (await p1.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = cat.CategoryId }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();
        await p2.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = cat.CategoryId });

        var put = await org.PutAsJsonAsync($"/api/enrolments/{e1!.EnrolmentId}/result",
            new ResultRequest { FinishTime = new TimeSpan(0, 45, 23), Position = 12 });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var mine = await (await p1.GetAsync("/api/users/me/results")).Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Equal(12, Assert.Single(mine!).Position);

        var theirs = await (await p2.GetAsync("/api/users/me/results")).Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Empty(theirs!);
    }

    [Fact]
    public async Task Result_RulesAreEnforced()
    {
        var org = await Helpers.LoggedIn(_f, "Organiser");
        var otherOrg = await Helpers.LoggedIn(_f, "Organiser");
        var part = await Helpers.LoggedIn(_f, "Participant");
        var ev = await Helpers.CreateEvent(org);
        var cat = await Helpers.CreateCategory(org, ev.EventId);
        var enrol = await (await part.PostAsJsonAsync($"/api/events/{ev.EventId}/enrol", new EnrolRequest { CategoryId = cat.CategoryId }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();
        var url = $"/api/enrolments/{enrol!.EnrolmentId}/result";
        var body = new ResultRequest { FinishTime = new TimeSpan(1, 0, 0), Position = 1 };

        Assert.Equal(HttpStatusCode.Forbidden, (await part.PutAsJsonAsync(url, body)).StatusCode);       // participant
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOrg.PutAsJsonAsync(url, body)).StatusCode);   // not their event
        Assert.Equal(HttpStatusCode.NotFound, (await org.PutAsJsonAsync("/api/enrolments/99999/result", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await org.PutAsJsonAsync(url, new ResultRequest())).StatusCode);
    }
}