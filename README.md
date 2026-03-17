# Distributed Pixel Board System

This system provides a complete infrastructure for learning about REST APIs, local development practices, and OAuth 2.0 authorization.

### Tech Stack

The project utilizes a diverse set of technologies:
- ASP.NET Core for the main server and Razor Pages
- NestJS for the progressive Node.js backend services
- Angular for the frontend application interface
- Redis for shared state management
- Keycloak for OIDC-based identity and access management
- Postgres for supporting the Keycloak database
- Docker for containerized deployment

### Project Components

The system is divided into several main parts:
- **Main Server**: Built with ASP.NET Core, it manages the core game logic and provides a Swagger API. It includes services for board management, player tracking, and real-time updates via WebSockets and GraphQL.
- **Backend Service**: A NestJS application that serves as a scalable server-side framework.
- **Frontend**: An Angular application for user interaction and visualization.
- **Infrastructure**: A Docker configuration that orchestrates Keycloak, Redis, and the database.

### Getting Started

To run the full environment locally, you will need Docker installed.

1. **Start Teacher Infrastructure**
   Run the following command in the directory containing the docker-compose file:
   `docker-compose up`

2. **Initialize the Board**
   Navigate to http://localhost:5085/Admin and select "Start Game". This requires setting up an admin user in Keycloak under the "pixelboard-test" realm.

3. **Run the Backend**
   Install dependencies and start the Node.js server:
   `npm install`
   `npm run start`

4. **Run the Frontend**
   Start the Angular development server:
   `ng serve`

### Core URLs
- Main Server and API: http://localhost:5085
- Keycloak Admin: http://localhost:18080
- Angular Frontend: http://localhost:4200
- Backend API: http://localhost:3000
