# ADR 0002: Use ASP.NET Core For The New Site

## Status

Accepted.

## Context

The legacy site is ASP.NET Web Forms on .NET Framework 4.5. Web Forms is not a good foundation for a new Azure-hosted rebuild.

The new project needs long-term maintainability, modern hosting, modern dependency management, and clean routing.

## Decision

Use ASP.NET Core for the new application.

Preferred UI style:

- Razor Pages or MVC for server-rendered pages.

Avoid:

- Porting Web Forms pages.
- Copying Telerik Web UI controls.
- Recreating the old page lifecycle.

## Consequences

Benefits:

- Modern .NET support.
- Works well on Azure App Service.
- Simple routing and redirects.
- Good testability.
- Easier migration to future services.

Tradeoffs:

- Old VB.NET page code is reference material, not reusable UI code.
- Some old behavior must be reimplemented deliberately.

