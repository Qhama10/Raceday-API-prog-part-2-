| **Module:**         | Programming                                  |
| ------------------- | -------------------------------------------- |
| **Assignment:**     | RaceDay — Part 2: RESTful API Development    |
| **Student Name:**   | Qhama Hobongwana                             |
| **Student Number:** | ST10465226                                   |

# RaceDay API (Part 2)

ASP.NET Core Web API (.NET 8) with Entity Framework Core (Code-First) on SQL Server. It implements the endpoint plan from Part 1 and is the back end for the MVC front end in Part 3.

## Setup and run

**Requirements:** .NET 8 SDK, Visual Studio 2022 (or the `dotnet` CLI), SQL Server or LocalDB.

1. Clone the repo and open `Raceday API (prog part 2).sln`.
2. Check the connection string in `appsettings.json` (`DefaultConnection`). The default points at LocalDB, database `RaceDay`.
3. Set your own JWT secret (at least 32 characters) in `Jwt:Key`. For anything beyond coursework, keep it out of source control with `dotnet user-secrets set "Jwt:Key" "<long random value>"`.
4. Create the database from the EF Core model. In Visual Studio's Package Manager Console, with the API project as the default project:
```
   Update-Database
```
   or from a terminal in the project folder:
```
   dotnet tool install --global dotnet-ef
   dotnet ef database update
```
5. Run the API (`F5`, or `dotnet run`). Swagger opens at `https://localhost:<port>/swagger`.
6. In Swagger: call `POST /api/auth/register`, then `POST /api/auth/login`, click **Authorize** and paste the token.

**Run the tests:** `dotnet test` from the solution folder. The tests use an in-memory database and a fake weather service, so no SQL Server or internet is needed.

## The two roles

| Role | What they can do |
| ---- | ---------------- |
| **Organiser** | Create, update and delete **their own** events; define categories for their events; view and export enrolments for their events; record finish times and positions. Can also view events, categories, maps and weather, and manage their profile. |
| **Participant** | Browse events and categories; enrol in an event by choosing a category; view their own enrolments and results; manage their profile. Cannot create or change events, categories or results. |

The role is chosen at registration (`role` field) and decides which table (`Organisers` or `Participants`) the account lives in. The role is then stored inside the login token, so each request is checked against it: wrong role gives `403 Forbidden`, and a missing or invalid token gives `401 Unauthorized`. Organisers can only modify events they own.

## Authentication

`POST /api/auth/login` returns a signed JWT containing the user's id and role. Sending it as `Authorization: Bearer <token>` is the authenticated "session" for all later requests. Passwords are hashed with BCrypt and are never stored or returned in plain text.

## Endpoints

| Area | Endpoint | Access |
| ---- | -------- | ------ |
| Auth | `POST /api/auth/register`, `POST /api/auth/login` | Public |
| Profile | `GET/PUT /api/users/me` | Authenticated |
| Events | `GET /api/events`, `GET /api/events/{id}`, `GET /api/events/upcoming` | Authenticated |
| Events | `POST /api/events`, `PUT/DELETE /api/events/{id}` | Organiser (owner) |
| Map and weather | `GET /api/events/{id}/map`, `GET /api/events/{id}/weather` | Authenticated |
| Categories | `GET /api/events/{eventId}/categories` | Authenticated |
| Categories | `POST /api/events/{eventId}/categories`, `PUT/DELETE /api/categories/{id}` | Organiser (owner) |
| Enrolments | `POST /api/events/{eventId}/enrol`, `GET /api/users/me/enrolments` | Participant |
| Enrolments | `GET /api/events/{eventId}/enrolments`, `GET /api/events/{eventId}/enrolments/export` (CSV) | Organiser (owner) |
| Results | `PUT /api/enrolments/{enrolmentId}/result` | Organiser (owner) |
| Results | `GET /api/users/me/results` | Participant |

## Map and weather

Event locations are matched against a fixed list of known places (Durban, Cape Town, Soweto, Johannesburg, Pretoria, Gqeberha). For a matching event:

- `/map` returns the coordinates, a Google Maps link and an OpenStreetMap embed URL.
- `/weather` returns the current temperature, wind speed and conditions from Open-Meteo (free, no API key).

An event at an unknown location returns `404`. If the weather service is unreachable the API returns `503`.

## Database

The database is created from the EF Core model (code-first, `InitialCreate` migration). To view the data in Visual Studio: View → SQL Server Object Explorer → (localdb)\MSSQLLocalDB → Databases → RaceDay → Tables → right-click a table → View Data.

## CI/CD

GitHub Actions (`.github/workflows/part2-ci.yml`) restores, builds and runs all unit tests on every push.

**Build status:**

![CI build](docs/ci-green-build.png)

## Deviations from the Part 1 plan

1. **`categoryDistance` added** to the Categories `POST`/`PUT` bodies, because the database requires `category_distance` (Part 1 known issue #6).
2. **Schema fixes made in the EF model** (Part 1 known issues #2–#4): `Results` has a single `updated_by` foreign key to `Organisers`; `Enrolments` has a unique `(participant_id, category_id)` index; all foreign keys are `NOT NULL`. `Results.enrolment_id` is also unique (one result per enrolment). The Part 1 SQL script and ERD have been updated to match.
3. **Event deletion cascades in code**: `Enrolments → Categories` is not a cascade in the schema, so the API removes results, enrolments and categories explicitly before the event.
4. **Withdrawn enrolments** do not block re-enrolling in the same event, and recording a result moves the enrolment status from `Registered` to `Completed`.
5. **Token-based session**: the authenticated session is a JWT (as planned in Part 1) rather than a server-side cookie session.
6. **Map and weather endpoints added** (`/map`, `/weather`) using known hard-coded locations.

## Video

Unlisted YouTube link: _add here_
