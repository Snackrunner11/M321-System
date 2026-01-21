# Distributed Pixel Board

An educational project used in distributed systems classes.

The intended audience knows a bit of C# and ASP.NET Core.

Teams of students implement a client application to read and modify a shared
pixel board. Most of the course is done in local development, without the shared
state. But the end goal is to play an interactive mini game on the pixel board
where each team tries to score points by drawing specific shapes in their team's
color while preventing other teams to draw theirs. A functioning client is
essential to be competitive.

## Concepts

- REST API basics
- Local development practices with test and production environments
- Publish / subscribe concept
- OAuth 2.0 (code authorization)

## Tech Stack

For students:

- ASP.NET Core, specifically Razor Pages web apps
- Redis (client view only)

Additional tech for teachers:

- Hosting of Redis, Keycloack, and a ASP.NET Core based web server (docker-compose configuration provided)
- Configure Keycloak as the OIDC provider

## Quick Start (Local Dev Environment)

In the class, we explore components one by one and learn how to run them one by
one. Doing it manually takes more time but helps to understand what happens. But
if you just want to try out the final project and run it locally, follow these
steps.

### 1 Select a git branch

Use `no-auth` for testing without authentication. This branch is used at the
start of the course. Use `main` for the full project.

### 2 Start all teacher-side components

```bash
docker-compose build
docker-compose up
```

This runs 4 docker containers (Keycloak, Postgres, Redis, ASP.NET API server)
which form the infrastructure to develop a client application against.

You should now be able to load a page on http://localhost:5085/. The pixel board
has not been initialized, so you will only see the title "Pixel Board" on an
empty page.

To initialize the board state, go to http://localhost:5085/Admin and click on
"Start Game". If building with auth enabled, you must complete step 3 first to
gain access.

The board is now ready for tasks that don't require authentication. If you are
on the `no-auth` branch, you are all set up.

### 3 Configure Keycloak

_Only necessary if you want to use the authenticated APIs. Use the `no-auth`
branch otherwise._

1. Open keycloak under http://localhost:18080/.
2. Open "Administration Console" and log in with `KEYCLOAK_ADMIN` and
   `KEYCLOAK_ADMIN_PASSWORD` defined in
   [docker-compose.yml](./docker-compose.yml).
3. On the top-left, select the realm dropdown (currently master selected) and
   click "Create Realm". Proceed to import the file
   [keycloak/test-realm.json](./keycloak/test-realm.json) and don't change
   anything before confirming with the "Create" button. You should now have a
   "pixelboard-test" realm.
4. Next, you want to add users in Keycloak that clients can use for
   authorization. On the user creation screen, select "Join Group" and select a
   team for the user. Being in team 0 grants access to
   `http://localhost:5085/Admin`.


## Hosting for shared used (Teacher)

As the teacher, you may want to host this for everyone in the class to use. In
such a case, here are a few things you probably want to do on top of the quick
start.

- Use [docker-compose.production.yaml](./docker-compose.production.yaml) instead
  of [docker-compose.yml](./docker-compose.yml) and make sure adjust the domain.
  Also change all secrets marked with XXX comments.
- Change the client secret inside Keycloak for for the "student_client".
  Initially it will be **********. (Literal asterisks) Also update the secret
  used in the teacher client build. For example,
  `dotnet user-secrets set "Keycloak:ClientSecret" "SECRET" --id "1625b1eb-47fe-43d5-bdcd-c89d1e43e56b"`
  and then rebuild the main_server.
- Set up specific board states for different tasks. See [./scripts](./scripts/readme.md).
