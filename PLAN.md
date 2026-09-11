# Membership Module — Architecture Cleanup Plan

## Current Problem

Two competing membership implementations exist:

1. **ClubSite-embedded** (`ClubSite/Data/ClubContext`, etc.) — old, duplicated, wired to `ClubSite_Membership` DB
2. **Membership RCL** (`projects/Membership/src/ClubSite.Membership/`) — richer, proper Razor Class Library, but uses Piranha ISecurity for login

Neither is truly independent. The RCL pages currently depend on Piranha's `ISecurity` for login.

## Target Architecture

```
ClubSite web app
 ├── Piranha CMS + Identity (ClubSite_RealCopy DB)
 │    └── Always active, admins only
 │
 └── Membership Module (RCL) [config toggle: FeatureManagement:EnableMembership]
      ├── ClubSite_Membership DB (schema: Membership)
      │    ├── Identity tables (AspNetUsers, AspNetRoles, etc.)
      │    ├── Business tables (Member, Department, MembershipFee, etc.)
      │    └── Audit/Invitation tables
      ├── Own SignInManager / UserManager (no Piranha dependency)
      ├── Own Login/Register/Profile/Admin pages
      └── 62 passing tests
```

## Phases

### Phase 1: Plan (this doc) ✅

### Phase 2: Add Self-Contained Identity to Membership RCL
- [ ] 2.1: Create `MembershipUser : IdentityUser` entity
- [ ] 2.2: Convert `MembershipDbContext` to `IdentityDbContext<MembershipUser>` with `Membership` schema
- [ ] 2.3: Create EF migration for Identity tables
- [ ] 2.4: Implement `DefaultMemberAccountService : IMemberAccountService` inside RCL
- [ ] 2.5: Rewrite Login page to use `SignInManager<MembershipUser>` (remove Piranha dependency)
- [ ] 2.6: Update `ServiceCollectionExtensions` to register Identity + services
- [ ] 2.7: Build & verify RCL tests still pass

### Phase 3: Clean Up Embedded Code from ClubSite
- [ ] 3.1: Remove ClubSite/Data/ClubContext.cs, Models/User.cs, Role.cs, etc.
- [ ] 3.2: Remove ClubSite/Services/DefaultMembershipService.cs + IMembershipService.cs
- [ ] 3.3: Remove ClubSite/Pages/MemberRegistration.cshtml, MemberProfile.cshtml, ManageMembers.cshtml
- [ ] 3.4: Remove migration 20260719182847_InitialMembership
- [ ] 3.5: Strip membership references from WebAppStartup.cs
- [ ] 3.6: Verify build still passes (no more duplicate references)

### Phase 4: Wire RCL into ClubSite Behind Config Toggle
- [ ] 4.1: Add `<ProjectReference>` to ClubSite.csproj
- [ ] 4.2: Add `"FeatureManagement:EnableMembership"` to AppSettings.json
- [ ] 4.3: In WebAppStartup, conditionally call `services.AddClubSiteMembership(configuration)`
- [ ] 4.4: Apply RCL migrations to ClubSite_Membership database
- [ ] 4.5: Recreate test members (Member123 etc.) using new Identity system
- [ ] 4.6: Build, restart server, verify login on iPhone

### Phase 5: Database Cleanup
- [ ] 5.1: Drop old AspNetUsers/AspNetRoles tables from old embedded approach
- [ ] 5.2: Verify data integrity

### Phase 6: OKR Review
- [ ] 6.1: Update OKR.md status
- [ ] 6.2: Run full test suite
- [ ] 6.3: Verify all 62+ tests pass under new architecture

## OKR Mapping

| KR | What changes | Status after cleanup |
|----|-------------|---------------------|
| KR 1.1 | Core entities → RCL models | ✅ Already in RCL |
| KR 1.2 | Own DB schema | ✅ Membership schema in ClubSite_Membership |
| KR 1.3 | Admin CRUD pages | ✅ RCL has Members/Departments/Fees CRUD |
| KR 1.4 | Youth-guardian validation | ✅ RCL MemberService tests |
| KR 1.5 | Family group linking | ✅ RCL MemberService tests |
| KR 1.6 | RCL integrated into ClubSite | 🟡 In progress (Phase 4) |
| KR 1.7 | Documentation | ✅ OKR, use cases, architecture |
| KR 1.8 | Admin Invite-Flow | 🟡 RCL InvitationService done, needs admin UI |
| KR 1.9 | Family inline beim Create | ✅ RCL Register page has family section |
| KR 1.10 | Login/Logout UI | 🟡 Page exists, needs Identity wiring |
| KR 1.11 | Profile editing | 🟡 Page exists, needs Identity+login |
| KR 4.1-4.4 | Testing | ✅ 62 unit/integration tests (Phase 2) |