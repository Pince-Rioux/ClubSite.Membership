# VereinMember

An add-on member management system for the ClubSite sports club website.

## Project Status

**Phase 1 — Foundation:** Core entities, DbContext, CRUD pages (active development)

## Structure

```
Membership/
├── docs/                   # OKR, use cases, architecture, security
│   ├── OKR.md
│   ├── USE_CASES.md
│   ├── ARCHITECTURE.md
│   └── DSGVO_SECURITY.md
├── src/
│   └── VereinMember/   # Razor Class Library
│       ├── Areas/Membership/  # Controllers, Pages, ViewModels
│       ├── Data/              # DbContext, configurations, migrations
│       ├── Models/            # Domain entities
│       ├── Services/          # Business logic interfaces + implementations
│       └── Extensions/        # DI registration helpers
└── tests/
    └── VereinMember.Tests/
```

## Development Lifecycle

1. Development in `Membership/` as standalone RCL
2. ClubSite adds project reference → integration tested in same solution
3. Stable → PR into ClubSite main branch
4. Module can later be extracted as independent service if needed

## Tech Stack

- .NET 8 + ASP.NET Core
- Entity Framework Core 9 + SQL Server
- Razor Pages + REST API
- Piranha Identity (shared auth)
- xUnit + WebApplicationFactory (testing)

## License

MIT — same as ClubSite