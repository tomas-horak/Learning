# User Management API

A small ASP.NET Core minimal API for creating, listing, retrieving, updating, and deleting users. User data is held in memory, so it resets when the application stops.

## Run

From this directory:

```bash
dotnet run
```

The Development launch profile listens at `http://localhost:5080`. The development-only demo bearer token is `course-demo-token`.

For another environment, configure `ApiToken` through configuration or an environment variable. Do not use the demo token as a production credential.

Every API route requires this header:

```text
Authorization: Bearer course-demo-token
```

## Endpoints

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/users` | List users |
| GET | `/api/users/{id}` | Retrieve one user; returns 404 if missing |
| POST | `/api/users` | Create a user; requires a non-empty name and valid email |
| PUT | `/api/users/{id}` | Replace a user’s name and email; returns 404 if missing |
| DELETE | `/api/users/{id}` | Delete a user; returns 204 or 404 |

The middleware pipeline is ordered as required by the exercise: exception handling, token authentication, then request/response logging. Authentication failures are also logged before returning 401. Unhandled exceptions are logged and returned as a consistent JSON 500 response.

Open `UserManagementAPI.http` in an editor with an HTTP client extension to try the requests.
