# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OrderProcessingSystem is an ASP.NET Core Web API built on .NET 10.0. This is a containerized API application with OpenAPI support for API documentation and testing.

## Architecture

- **Framework**: ASP.NET Core Web API (.NET 10.0)
- **API Pattern**: Controller-based routing with `[ApiController]` attributes
- **Configuration**: Standard appsettings.json pattern with environment-specific overrides
- **Containerization**: Docker support with multi-stage builds for optimized production images
- **Security**: User secrets configured (ID: c7f3c94a-5779-444d-95ef-6dbcce13b6ea) for local development

### Key Components

- **Program.cs**: Application entry point and service configuration
- **Controllers/**: API endpoint controllers
- **Properties/launchSettings.json**: Development server profiles and environment configuration

## Development Commands

### Building and Running

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application (Development mode, HTTPS)
dotnet run

# Run with specific profile
dotnet run --launch-profile http    # HTTP only on port 5139
dotnet run --launch-profile https   # HTTPS on 7298, HTTP on 5139
```

### Docker

```bash
# Build Docker image
docker build -t orderprocessingsystem .

# Run container
docker run -p 8080:8080 -p 8081:8081 orderprocessingsystem
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with verbosity
dotnet test --verbosity normal
```

## Development URLs

- **HTTP**: http://localhost:5139
- **HTTPS**: https://localhost:7298
- **OpenAPI**: Available in Development environment at `/openapi/v1.json`

## Configuration Notes

- User secrets are enabled for local development - use `dotnet user-secrets` command to manage
- Docker target OS is Linux
- OpenAPI/Swagger is enabled only in Development environment
- HTTPS redirection is enabled globally
