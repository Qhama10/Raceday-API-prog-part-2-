using System.ComponentModel.DataAnnotations;
namespace Raceday_API__prog_part_2_.Dtos;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required] public string Role { get; set; } = "";
    public string? OrganisationName { get; set; }
    public string? ContactPhone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Username { get; set; }
}
public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public record UserSummary(int Id, string Email, string FullName, string Role);
public record LoginResponse(string Token, DateTime ExpiresAt, UserSummary User);

public class EventRequest
{
    [Required, MaxLength(255)] public string EventName { get; set; } = "";
    public string? EventDescription { get; set; }
    [Required] public DateOnly? EventDate { get; set; }
    [Required, MaxLength(255)] public string EventLocation { get; set; } = "";
    [Range(0.01, 9999.99)] public decimal Distance { get; set; }
    [Required] public string EventType { get; set; } = "";
}
public class CategoryRequest
{
    [Required, MaxLength(100)] public string CategoryName { get; set; } = "";
    [Required, MaxLength(250)] public string CategoryDescription { get; set; } = "";
    [Required, MaxLength(250)] public string CategoryDistance { get; set; } = "";
}
public class EnrolRequest { [Range(1, int.MaxValue)] public int CategoryId { get; set; } }
public class ResultRequest
{
    public TimeSpan? FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? Position { get; set; }
    public string? Notes { get; set; }
}

public record CategoryResponse(int CategoryId, int EventId, string CategoryName, string CategoryDescription, string CategoryDistance);
public record EventResponse(int EventId, int OrganiserId, string EventName, string? EventDescription,
    DateOnly EventDate, string EventLocation, decimal Distance, string EventType);
public record EventDetailResponse(int EventId, int OrganiserId, string EventName, string? EventDescription,
    DateOnly EventDate, string EventLocation, decimal Distance, string EventType, List<CategoryResponse> Categories);
public record EnrolmentResponse(int EnrolmentId, DateTime EnrolmentDate, string Status, int ParticipantId,
    string? ParticipantName, string? ParticipantEmail, int EventId, string EventName, int CategoryId, string CategoryName);
public record ResultResponse(int ResultId, int EnrolmentId, TimeSpan? FinishTime, int? Position, string? Notes,
    int UpdatedBy, DateTime UpdatedAt, int EventId, string EventName, string CategoryName);

public class UpdateProfileRequest
{
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    public string? OrganisationName { get; set; }
    public string? ContactPhone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Username { get; set; }
}

public class ProfileResponse
{
    public int Id { get; set; }
    public string Role { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? OrganisationName { get; set; }
    public string? ContactPhone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Username { get; set; }
}