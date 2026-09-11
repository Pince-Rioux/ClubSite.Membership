# ClubSite Membership Module — OKRs

## Objective 1: Launch a production-ready member management system Phase1

**Owner:** ClubSite development team
**Timeframe:** Current sprint

| Key Result | Measure | Target | Status |
|---|---|---|---|
| KR 1.1 | Core entities defined (Member, Department, MembershipFee, FamilyGroup) | 4 entities with full relationships including guardian and family linking | ✅ Done |
| KR 1.2 | MembershipDbContext with own DB schema | EF Core migrations runnable, isolated from Piranha schema | ✅ Done |
| KR 1.3 | Admin CRUD pages for Member, Department, Fee, FamilyGroup | All CRUD operations work via Razor Pages in Membership Area. **Family members are added inline when FeeCategory = Family** (no separate FamilyGroup CRUD required for standard workflows) | ✅ Member + Department + Fee pages done. Family inline UI with Relationship dropdown + JoinedAt field + validation error display |
| KR 1.4 | Youth-guardian validation enforced | Creating a Youth member without guardian = validation error | ✅ Done |
| KR 1.5 | Family group linking functional | Members can be grouped into families, minimum 2 per group | ✅ Done (service validation + min-2 check) |
| KR 1.6 | Razor Class Library integrated into ClubSite solution | `dotnet build` passes (tests planned for Phase 2) | ✅ Build passes. Test project pending. |
| KR 1.7 | Documentation complete (OKR, use cases, architecture, security) | 4 docs created, reviewed, in repo | ✅ Done |
| KR 1.8 | **Admin Invite-Flow** | Admin can create member(s) and send Magic-Link via email so the new member sets their own password. No admin password handling needed. | ✅ Done. Invitation created + email sent (plain-text German template) via ClubSite's MailKit/SMTP infrastructure on admin Create. |
| KR 1.9 | **Family inline anlage beim Create** | When FeeCategory = Family, the Create form shows inline sub-member rows (name, email, birthdate, relationship, join date). FamilyGroup is created automatically on submit. Invitations sent to all sub-members. | ✅ Done. FamilyMemberItem now includes Relationship + JoinedAt. Validation errors shown inline. Invitations sent for all family members. |
| KR 1.10 | **Login/Logout UI** | Login page, logout, password-reset via RCL's own Identity (`SignInManager<MembershipUser>`). Available to all member types. | ✅ Done. Role-aware redirect: admin → Members, member → Profile. |
| KR 1.11 | **Profile editing** | Member can update their own personal info, address, contact data (except identity-critical and admin-controlled fields). Available after login. | ✅ Done. Profile page with OnPostAsync update via memberService.UpdateOwnProfileAsync. |

## Objective 2: Member data protected to the highest security and DSGVO standards

| Key Result | Measure | Target | Status |
|---|---|---|---|---|
| KR 2.1 | Data minimization enforced | Only required fields stored; configurable field sets | ✅ Done |
| KR 2.2 | Purpose limitation documented per field | Field-level annotation in data dictionary | ✅ Done |
| KR 2.3 | Retention and deletion policy defined | Documented policy + soft-delete with purge scheduling | 🟡 Policy documented + soft-delete implemented. Scheduled purge: Phase 2. |
| KR 2.4 | Access control: role-based authorization | Minimum 3 roles (Admin, Editor, Viewer) enforced on all endpoints | ✅ Done (4 policies, 14 `[Authorize]` attributes applied) |
| KR 2.5 | DSGVO data export & erasure endpoints | REST endpoints for member data export (JSON) and erasure | ❌ Phase 2 |
| KR 2.6 | Encryption at rest (SQL Server TDE or column-level) | Documented and verified | 🟡 IBAN value converter present (base64 stub). Production AES-256 or Always Encrypted: Phase2. |
| KR 2.7 | Audit log for all member data mutations | Every create/update/delete logged with timestamp, user, field diff | ✅ Done |
| KR 2.8 | Guardian relationship data handled per DSGVO Art. 32 | Guardian links are audited; family group data protected by same retention policy | ✅ Done |

## Objective 3: Architecture enables future extraction or independent deployment

| Key Result | Measure | Target | Status |
|---|---|---|---|
| KR 3.1 | Membership is a standalone Razor Class Library | Can be built and tested without ClubSite host project | ✅ Done |
| KR 3.2 | All business logic behind service interfaces | Every controller/page depends on interface, not concrete | ✅ Done |
| KR 3.3 | Own DbContext in own DB schema | Zero table name collisions with Piranha | ✅ Done |
| KR 3.4 | REST API layer exists alongside Razor Pages | API controllers return structured JSON, not HTML redirects | ❌ Phase 2 |

## Objective 4: Maintainable, tested codebase

| Key Result | Measure | Target | Status |
|---|---|---|---|
| KR 4.1 | Unit test coverage for all service methods | ≥ 80% line coverage on Services/ | ❌ Phase 2 |
| KR 4.2 | Integration tests for DbContext queries | All queries tested against real SQL Server (or LocalDB) | ❌ Phase 2 |
| KR 4.3 | All controllers tested via WebApplicationFactory | CRUD endpoints return expected status codes | ❌ Phase 2 |
| KR 4.4 | CI pipeline build + test passes | GitHub Actions workflow succeeds | ❌ Phase 2 |

## Objective 5: Public self-service registration (no admin intervention needed)

**Owner:** ClubSite development team
**Timeframe:** Next sprint

| Key Result | Measure | Status |
|---|---|---|
| KR 5.1 | **Public registration** — Visitor goes to `/Membership/Register`, picks fee category (Adult/Youth/Family), add family members inline if Family, submits → receives Magic-Link email → sets password → **is Active immediately**. No admin approval required.| ❌ Not started |
| KR5.2 | **Admin can override** — Admin can edit, deactivate, or manually create any member. But the default flow runs without admin interaction. | ❌ Not started |
| KR 5.3| **Self-service family extension** — Logged-in member can later add additional family members to their own FamilyGroup. New members receive their own Magic- Link. | ❌ Not started |

---

*Status: OKR reviewed by Yves on 2026-09-03.*