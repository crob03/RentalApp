---
_layout: landing
---

# RentalApp

A cross-platform rental application built with .NET MAUI, targeting Android. Backed by PostgreSQL and a remote HTTP API.

## Architecture

| Project | Role |
|---------|------|
| **RentalApp** | MAUI UI — MVVM with CommunityToolkit.Mvvm |
| **RentalApp.Database** | Data access — EF Core with PostgreSQL |
| **RentalApp.Migrations** | EF Core migration library |

## Quick Links

- [API Reference](api/RentalApp.html)
- [Decisions](DECISIONS.md)
