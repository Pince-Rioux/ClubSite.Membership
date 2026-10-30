# VereinMember — Architecture

## Overview

The Membership module is implemented as a **Razor Class Library (RCL)** that is referenceable by the ClubSite ASP.NET Core host project. It lives in `projects/Membership/src/VereinMember/` during development and integrates into ClubSite via solution project reference.

---

## Design Principles

1. **Separation of concerns** — own Area, own DbContext, own service layer
2. **Independent deployability** — RCL can be built, tested, and versioned independently
3. **Security by design** — no personal data in URLs, no IBAN in logs, role-gated access
4. **DSGVO compliance** — field-level purpose annotation, soft delete, audit log, export/erasure endpoints
5. **Testability** — services depend on interfaces, DbContext injectable

---

## Module Structure

```
VereinMember/
├── Areas/
│   └── Membership/
│       ├── Controllers/
│       │   ├── HomeController.cs             # redirect to /Membership/Members
│       │   ├── MembersController.cs           # CRUD + search + filter API
│       │   ├── DepartmentsController.cs       # CRUD API
│       │   ├── FeesController.cs              # CRUD API
│       │   ├── FamilyGroupsController.cs      # CRUD API
│       │   └── ExportController.cs            # PDF/CSV/SEPA export
│       ├── Pages/
│       │   ├── Members/
│       │   │   ├── Index.cshtml               # list with search/filter
│       │   │   ├── Create.cshtml              # new member form (guardian/family dropdowns)
│       │   │   ├── Edit.cshtml                # edit member form
│       │   │   ├── Details.cshtml             # member detail view + guardian/family links
│       │   │   └── Delete.cshtml              # confirm deactivation
│       │   ├── Departments/
│       │   ├── Fees/
│       │   └── FamilyGroups/
│       │       ├── Index.cshtml               # list with member count
│       │       ├── Create.cshtml
│       │       └── Edit.cshtml                 # add/remove members
│       ├── Models/
│       │   ├── MemberViewModel.cs
│       │   ├── MemberListViewModel.cs
│       │   ├── DepartmentViewModel.cs
│       │   ├── FeeViewModel.cs
│       │   └── FamilyGroupViewModel.cs
│       ├── _ViewImports.cshtml
│       └── _ViewStart.cshtml
├── Data/
│   ├── MembershipDbContext.cs             # own EF Core context
│   ├── EntityConfigurations/
│   │   ├── MemberConfiguration.cs         # Fluent API config (includes guardian + family FKs)
│   │   ├── DepartmentConfiguration.cs
│   │   ├── MembershipFeeConfiguration.cs
│   │   ├── AuditEntryConfiguration.cs
│   │   └── FamilyGroupConfiguration.cs
│   └── Migrations/                        # EF Core migrations
├── Models/                                # domain entities (Poco)
│   ├── Member.cs
│   ├── MemberStatus.cs
│   ├── Department.cs
│   ├── MembershipFee.cs
│   ├── FeeCategory.cs                     # Adult / Youth / Family
│   ├── FeeInterval.cs
│   ├── FamilyGroup.cs
│   └── AuditEntry.cs
├── Services/
│   ├── IMemberService.cs
│   ├── MemberService.cs                   # CRUD + guardian validation + family group logic
│   ├── IAuditService.cs
│   ├── AuditService.cs
│   └── Validators/
│       └── MemberValidator.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs     # DI registration helpers
├── VereinMember.csproj
└── _Imports.cs
```

---

## Layer Diagram

```
┌─────────────────────────────────────────────────┐
│                   ClubSite Host                  │
│  (Program.cs, WebAppStartup.cs, appsettings)     │
├─────────────────────────────────────────────────┤
│           VereinMember RCL                │
│                                                  │
│  ┌─────────────┐    ┌──────────────────────┐    │
│  │ Razor Pages  │    │   API Controllers    │    │
│  │ (Areas/)     │    │   (Areas/)           │    │
│  └──────┬──────┘    └──────────┬───────────┘    │
│         │                      │                 │
│  ┌──────┴──────────────────────┴───────────┐    │
│  │          Service Layer                   │    │
│  │  IMemberService   (guardian + family)   │    │
│  │  IAuditService                           │    │
│  └──────────────────────┬──────────────────┘    │
│                         │                        │
│  ┌──────────────────────┴──────────────────┐    │
│  │      MembershipDbContext                 │    │
│  │  (Schema: Membership)                    │    │
│  │  [Member] [Department] [Fee]            │    │
│  │  [FamilyGroup] [AuditLog]               │    │
│  └──────────────────────┬──────────────────┘    │
│                         │                        │
└─────────────────────────┼────────────────────────┘
                          │
              ┌───────────┴───────────┐
              │    SQL Server          │
              │  Membership schema     │
              └───────────────────────┘
```

---

## Domain Model

```
Member (Membership schema)
├── Id: int (PK)
├── FirstName, LastName, Email, Phone
├── BirthDate: date
├── Address: Street, City, ZipCode, Country
├── BankIban, BankBic (encrypted)
├── JoinedAt, LeftAt, Status
├── DepartmentId: FK → Department (nullable)
├── MembershipFeeId: FK → MembershipFee
├── GuardianMemberId: FK → Member (nullable, self-ref)
│   └── A Youth member must have a guardian. Guardian must be Adult-category.
├── FamilyGroupId: FK → FamilyGroup (nullable)
│   └── A Family member belongs to a group of ≥ 2 members.
├── Notes
├── CreatedAt, UpdatedAt, CreatedBy
└── ConcurrencyToken (rowversion)

FamilyGroup (Membership schema)
├── Id: int (PK)
├── Name: string (e.g. "Familie Müller")
├── CreatedAt
└── Members: 1-to-many via Member.FamilyGroupId

MembershipFee (Membership schema)
├── Id: int (PK)
├── Name: string (e.g. "Adult Standard", "Youth Reduced")
├── Amount: decimal
├── Interval: Monthly / Quarterly / Yearly
├── Category: FeeCategory (Adult / Youth / Family)
│   ├── Adult → no extra constraints
│   ├── Youth → requires GuardianMemberId
│   └── Family → requires FamilyGroupId with ≥ 2 members
└── IsDefault: bit
```

---

## Database Schema

### Tables in `Membership` schema

```
Membership.Members
├── Id (int, PK, identity)
├── FirstName (nvarchar(100), not null)
├── LastName (nvarchar(100), not null)
├── Email (nvarchar(200), not null, unique index)
├── Phone (nvarchar(50), null)
├── BirthDate (date, not null)
├── Street (nvarchar(200), null)
├── City (nvarchar(100), null)
├── ZipCode (nvarchar(20), null)
├── Country (nvarchar(100), null)
├── BankIban (nvarchar(34), null, encrypted)
├── BankBic (nvarchar(11), null, encrypted)
├── JoinedAt (datetime2, not null)
├── LeftAt (datetime2, null)
├── Status (nvarchar(20), not null) -- Active|Passive|Honorary|Former
├── DepartmentId (int, FK → Departments.Id, null)
├── MembershipFeeId (int, FK → MembershipFees.Id, not null)
├── GuardianMemberId (int, FK → Members.Id, null)          -- NEW
├── FamilyGroupId (int, FK → FamilyGroups.Id, null)        -- NEW
├── Notes (nvarchar(max), null)
├── CreatedAt (datetime2, not null)
├── UpdatedAt (datetime2, not null)
├── CreatedBy (nvarchar(100), not null)
├── ConcurrencyToken (rowversion)

Membership.Departments
├── Id (int, PK, identity)
├── Name (nvarchar(100), not null, unique)
├── Description (nvarchar(500), null)

Membership.MembershipFees
├── Id (int, PK, identity)
├── Name (nvarchar(100), not null, unique)
├── Amount (decimal(18,2), not null)
├── Interval (nvarchar(20), not null)
├── Category (nvarchar(20), not null)      -- NEW: Adult / Youth / Family
├── IsDefault (bit, not null)

Membership.FamilyGroups                          -- NEW
├── Id (int, PK, identity)
├── Name (nvarchar(200), null)
├── CreatedAt (datetime2, not null)

Membership.AuditLog
├── Id (bigint, PK, identity)
├── EntityName (nvarchar(100), not null)
├── EntityId (int, not null)
├── Action (nvarchar(20), not null) -- Created|Updated|Deleted|Erasure
├── OldValues (nvarchar(max), null) -- JSON
├── NewValues (nvarchar(max), null) -- JSON
├── ChangedFields (nvarchar(max), null) -- JSON array
├── PerformedBy (nvarchar(100), not null)
├── PerformedAt (datetime2, not null)
```

**Indexes:**
- `IX_Members_Email` — unique, filtered where `LeftAt IS NULL`
- `IX_Members_Status` — for filtering
- `IX_Members_DepartmentId` — FK lookup
- `IX_Members_GuardianMemberId` — FK lookup for dependents query
- `IX_Members_FamilyGroupId` — FK lookup for family members query
- `IX_AuditLog_EntityName_EntityId` — point lookup
- `IX_AuditLog_PerformedAt` — retention purge

---

## Integration with ClubSite

### In ClubSite's `WebAppStartup.cs`

```csharp
// Register Membership DbContext
services.AddDbContext<MembershipDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("VolleyballClub")));

// Register services
services.AddScoped<IMemberService, MemberService>();
services.AddScoped<IAuditService, AuditService>();
```

Or, the Membership module provides an extension method:

```csharp
// One-liner registration
services.AddClubSiteMembership(configuration);
```

### In ClubSite's `Program.cs`

No changes needed — the RCL Areas are auto-discovered by ASP.NET Core when added as a dependency.

### Connection string

Reads `ClubMembershipDb` by default, falls back to `VolleyballClub` for backward compatibility.
Both point to the same SQL Server instance. The `ClubMembershipDb` database is already configured in the development credentials.

```csharp
var connStr = configuration.GetConnectionString("ClubMembershipDb")
              ?? configuration.GetConnectionString("VolleyballClub")
              ?? throw ...;
```

For migrations, a `DesignTimeDbContextFactory` uses the `ClubSite_Membership` database on localhost, overridable via the `MEMBERSHIP_CONNECTION_STRING` environment variable.

---

## Auth & Authorization

| Resource | Minimum Role | Policy |
|---|---|---|
| Member list (view) | Viewer | `[Authorize(Policy = "MemberView")]` |
| Member create/edit | Editor | `[Authorize(Policy = "MemberWrite")]` |
| Member delete/deactivate | Admin | `[Authorize(Policy = "MemberDelete")]` |
| Department CRUD | Admin | `[Authorize(Policy = "AdminOnly")]` |
| MembershipFee CRUD | Admin | `[Authorize(Policy = "AdminOnly")]` |
| FamilyGroup CRUD | Admin | `[Authorize(Policy = "AdminOnly")]` |
| Audit log view | Admin | `[Authorize(Policy = "AdminOnly")]` |
| Data export | Editor | `[Authorize(Policy = "MemberWrite")]` |
| DSGVO erasure | Admin | `[Authorize(Policy = "MemberDelete")]` |

Policies map to Piranha Identity roles. Admins in Piranha automatically get Membership admin.

---

## Fee Category Constraints

| Category | Guardian Req | Family Group Req | Validation |
|---|---|---|---|
| Adult | No | No | Standard validation |
| Youth | Yes (Adult guardian) | No | Guardian must be Adult-category, not self |
| Family | No | Yes (≥ 2 members) | Group must exist, min 2 members |

These constraints are enforced in `MemberService.CreateAsync` and `MemberService.UpdateAsync`.

---

## Future Extraction Path

When the module needs to become an independent service:

1. The RCL becomes a standalone ASP.NET Core web app
2. `MembershipDbContext` connection string points to same (or replicated) DB
3. Controllers become the primary interface (REST API)
4. ClubSite calls the Membership API instead of referencing the library
5. Zero entity or domain model changes required

---

## Technology Stack

| Component | Technology | Rationale |
|---|---|---|
| Framework | .NET 8 | matching ClubSite |
| ORM | Entity Framework Core 9 | matching ClubSite's EFCore version |
| Database | SQL Server | matching ClubSite |
| UI | Razor Pages + Piranha Manager theme | reuse ClubSite design language |
| Auth | Piranha Identity (ASP.NET Core Identity) | reuse existing user/role infrastructure |
| PDF export | EPPlus (already in ClubSite) + QuestPDF | for member lists and statistics |
| SEPA XML | Custom builder (XmlDocument/XElement) | no external dependency needed; spec is fixed |
| Serialization | System.Text.Json | built-in, no dependency |
| Logging | NLog (already in ClubSite) | consistent with host |
| Testing | xUnit + WebApplicationFactory | standard .NET testing stack |
| Validation | FluentValidation | cleaner than DataAnnotations for complex rules |