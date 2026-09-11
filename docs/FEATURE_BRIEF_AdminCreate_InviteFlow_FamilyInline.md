# Feature Brief — Admin Create + Invite-Flow & Family Inline Polish

---

## Feature Name

Admin Member Creation with Invite-Flow and Enhanced Family Inline UI

## Problem

### KR 1.8: Admin Invite-Flow is missing
The admin `/Membership/Members/Create` page creates member records but sends **no invitation**. New members have no way to set their own password — the admin must manually handle credentials. The existing `DefaultMemberAccountService.SendInvitationEmailAsync()` only **logs** to console; no real email is sent.

### KR 1.3/1.9: Family inline UI needs polish
The admin Create page already has family inline sub-member rows (committed Sep 3, `2fb16fe`), but:
- No **Relationship** field (Ehepartner, Kind, etc.) — the `FamilyMemberItem` model has it, the UI doesn't
- No **JoinedAt** field for family sub-members (admin sets join date)
- Validation errors for sub-members aren't displayed clearly
- After creating a family, no invitations are sent to sub-members

## User / Audience

- **Admin** (Admin/Editor roles) — creates one or multiple members and sends Magic-Link invitations
- **New members** — receive an email with a Magic-Link to set their password and activate their account

## Flow (Step by Step)

### Flow: Admin creates member(s) and sends invitations

1. Admin navigates to `/Membership/Members/Create`
2. Form shows main member fields (first name, last name, email, birthdate, phone, fee category)
3. **Conditional: FeeCategory = Youth** → Guardian dropdown appears (select from existing Adult members, or info text if none available)
4. **Conditional: FeeCategory = Family** → Family section appears with:
   - Inline sub-member rows (each with: first name, last name, email, birthdate, **relationship** dropdown)
   - "+ Add family member" button (as exists)
   - Minimum 2 members validation (main + at least 1 sub)
5. Admin fills data, submits
6. **Validation:**
   - Main member: all existing validation rules
   - Youth → GuardianMemberId required
   - Family → min 2 members, all sub-members validated individually
   - Email uniqueness checked per member
7. **On success for standard (non-family):**
   - Member created via `memberService.CreateAsync()` (as today)
   - `Invitation` record created via `invitationService.CreateInvitationAsync()`
   - Magic-Link URL generated via `Url.PageLink("Activate", ...)`
   - **Email sent** to member with the link
   - Toast: "Mitglied Max Mustermann angelegt. Einladungs-E-Mail wurde versendet."
   - Redirect to member list
8. **On success for Family:**
   - All members created via `memberService.CreateFamilyAsync()` (as today)
   - For each sub-member: Invitation created + email sent
   - Toast: "3 Familienmitglieder angelegt. Einladungs-E-Mails wurden versendet."
   - Redirect to member list
9. Member clicks Magic-Link → lands on `/Membership/Activate?token=...` (already exists)
10. Sets password → token consumed → ready to login

## What It Looks Like

### Admin Create Form — Enhanced Family Section

```
┌─ Beitragsgruppe ──────────────────────────────────────────┐
│ [▼ Familie (45 €) ────────────────────────────────]       │
└───────────────────────────────────────────────────────────┘

┌─ Familienmitglieder ─────────────────────────────────────┐
│ ✔ Hauptmitglied: Max Mustermann (wird automatisch         │
│   als erstes Familienmitglied angelegt)                    │
│                                                            │
│ · Mitglied 2                                               │
│   Vorname * [________]  Nachname * [________]              │
│   E-Mail *  [________]  Geburtsdatum * [__/__/____]       │
│   Verhältnis [▼ Kind ─────────────]                        │
│   Eintrittsdatum * [08.09.2026]                            │
│   [− Entfernen]                                            │
│                                                            │
│ · Mitglied 3                                               │
│   [...]                                                     │
│                                                            │
│ [+ Familienmitglied hinzufügen]                            │
└───────────────────────────────────────────────────────────┘
```

### Magic-Link Email (plain-text)

```
Betreff: Willkommen beim VC Neusäß -- Registrierung abschließen

Hallo Max Mustermann,

Sie wurden beim Volleyballclub Neusäß e. V. registriert.
Bitte klicken Sie auf den folgenden Link, um Ihr Passwort zu setzen
und Ihren Account zu aktivieren:

https://clubsite/Membership/Activate?token=abc123...

Der Link ist 7 Tage gültig.

Sportliche Grüße,
Ihr VC Neusäß
```

### After creation -- Toast message

For single member: "Mitglied Max Mustermann angelegt. Einladungs-E-Mail wurde an max@example.com versendet."

For family (3 members): "3 Familienmitglieder angelegt. Einladungs-E-Mails wurden versendet."

## Architecture Decision: Email Sending

ClubSite already has a full mail infrastructure:

| Component | What it provides |
|---|---|
| `IMailService` / `MailService` | Uses **MailKit** + SMTP. Configured via `MailSettings` in credentials files. Supports File mode (Dev) and SMTP (Production). Already used by ContactPage + TournamentRegistration. |
| `MailSettings` | Server, Port, Username, Password, Timeout, DefaultFrom, Organization |
| `Sender.MessageOutput` | `"File"` in Development (writes to disk), `"Smtp"` in Production |

**Architecture used:**

```
Membership RCL                     ClubSite (host)
+------------------+              +------------------------------+
|  IEmailSender     |  <-wired by-> |  ClubSiteEmailSender         |
|  (interface)      |    DI       |  wraps IMailService/MailKit  |
|  LogOnlyEmailSender|             |                              |
|  (RCL fallback)   |              |  Adds club email template    |
|  DefaultMemberAccountService     |  (subject, body text)        |
|    -> uses IEmailSender           |                              |
+------------------+              +------------------------------+
```

- RCL defines `IEmailSender` interface with `SendAsync(to, subject, body)`
- RCL registers **`LogOnlyEmailSender`** as fallback (with `TryAdd` -- overridable)
- ClubSite registers **`ClubSiteEmailSender : IEmailSender`** that wraps `IMailService`
- `DefaultMemberAccountService.SendInvitationEmailAsync()` composes the German email body with Magic-Link URL, then delegates to `IEmailSender`
- Development mode: writes emails to disk (File output). No real SMTP needed for testing.

## What Does NOT Change

- **Existing admin CRUD list/edit/delete pages** -- untouched
- **Self-service Register (`/Membership/Register`)** -- already has family inline + invitation flow; minor polish only
- **Activate page (`/Membership/Activate`)** -- already works; no changes needed
- **Member model, FamilyGroup, FeeCategory, GuardianMemberId** -- no schema changes
- **Fee-category validation rules** -- Youth->guardian, Family->min 2, Adult->standalone
- **Existing InvitationService** -- token generation/validation/consumption already works
- **Existing tests (62)** -- must still pass

## Why This Approach?

The existing INVITE_FLOW.md draft already documented this design comprehensively. The Sep 4 Identity refactoring made the RCL self-contained, so the magic-link flow just needs final wiring:
1. **Admin Create page** -> add `IInvitationService` + `IMemberAccountService` calls after successful member creation
2. **Email sending** -> `IEmailSender` interface in RCL, `ClubSiteEmailSender` in host wrapping existing `IMailService` (MailKit + SMTP config)
3. **Family inline UI** -> add the missing relationship field + JoinedAt + polish validation

No new pages, no new database tables (Invitations already exist), no new models. Pure wiring + UI polish using ClubSite's existing mail infrastructure.

## Dependencies

- ClubSite already has `IMailService` with full SMTP/File support -- no new NuGet packages, no new config
- RCL's `IEmailSender` wired via `TryAddScoped` -- ClubSite overrides by registering `ClubSiteEmailSender`
- ClubSite `WebAppStartup.cs` updated -- registers `ClubSiteEmailSender` after `AddClubSiteMembership`
- Credentials files already contain SMTP settings (Development: File output, Production: SMTP)
- ClubSite build references the RCL (project reference already exists)

## Out of Scope (Explicit)

- **Separate "Invite" page** -- using the existing Create page is simpler; no new URL needed
- **Public self-service registration improvements** -- that's Objective 5; this is Objective 1 admin workflow
- **Password reset flow** -- only invitation-based password set
- **Bulk invite (CSV/Excel import)**
- **Family management UI for members** (member editing their own family)
- **Invitation status dashboard** (which invites are pending/expired)
- **Resend invitation** button (can be a follow-up)

---

*Status: Approved -- axuno review complete. Implementation done.*