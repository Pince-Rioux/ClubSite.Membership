# Feature Brief — Invite & Self-Service Registration

*Phase 2 feature: member self-registration and admin invitation flow.*
*Adapted for the existing Membership RCL data model (Member, FamilyGroup, FeeCategory, GuardianMemberId).*

---

## Feature Name

Self-Service Registration + Admin Invite-Flow

## Problem

Currently, members can only be added by an Admin via the CRUD form (`/Membership/Members/Create`). There is **no way for a new member to register themselves** online, and **no way for an Admin to send an invitation link** that lets the member set their own password.

Missing flows:
- **Visitor** sees the club website and wants to join — has to email or call the board
- **Admin** captures a member via paper form — has to manually create the user with no self-activation
- **Family registration** — Admin has to create each family member individually, no bulk flow

## User / Audience

- **Visitors** — register themselves via public form (self-service)
- **Admins (Admin/Editor roles)** — enter member data and send invitation links
- **New members** — receive an email with a Magic-Link to set their password and activate their account

## Flow (Step by Step)

### Flow A: Self-Service Registration (Visitor)

1. Visitor clicks "Mitglied werden" → lands on `/Membership/Register`
2. Form shows:
   - Vorname, Nachname, E-Mail (all required)
   - Geburtsdatum (required — determines Youth)
   - Telefon (optional), Straße, PLZ, Stadt
   - Beitragsgruppe: Dropdown of available `MembershipFee` options (filtered by FeeCategory)
3. **Conditional:** If selected fee category is **Youth** → Guardian fields become visible (dropdown of existing Adult members or "My guardian is not yet a member" option)
4. **Conditional:** If selected fee category is **Family** → additional inline fields for each family member (name, email, birthdate, relationship)
5. Visitor submits
6. **Validation:**
   - E-Mail unique
   - Youth → guardian must be selected or created separately
   - Family → minimum 2 members total
7. **On success:**
   - For each member: `Member` created with `Status = MemberStatus.Active`, `JoinedAt = now`
   - Guardian FK set for Youth members
   - FamilyGroup created (or assigned) for Family members
   - Identity user created in Piranha shared auth (linked to member via email)
   - **Invitation record** created with cryptographically secure token (7-day expiry)
   - **Email sent** to each member with Magic-Link
8. Visitor sees confirmation: "Registrierung erfolgreich! Bitte prüfen Sie Ihr E-Mail-Postfach und klicken Sie auf den Bestätigungslink."
9. Member clicks Magic-Link → lands on `/Membership/Activate?token=...`
10. Form shows read-only data (name, email) → Member sets password → submits
11. Token consumed, account active, member redirected to login → logs in → sees profile

### Flow B: Admin Invite-Flow

1. Admin navigates to `/Membership/Members/Invite`
2. Same form as Flow A, but with an additional "Eintrittsdatum" field (default today)
3. Admin selects FeeCategory → conditional fields appear (Guardian for Youth, Family members for Family)
4. Admin submits → identical logic to Flow A steps 7-8
5. **Toast:** "3 Mitglieder angelegt. Einladungs-E-Mails wurden versendet."
6. Redirect to member list — new members visible (Status: Active but not yet activated in Identity — login gated)

### Flow C: Magic-Link Acceptance (shared by A & B)

1. Recipient opens email → clicks `https://clubsite/Membership/Activate?token=abc123...`
2. Token validated (exists, not expired, not consumed)
3. Form shows: Vorname, Nachname, E-Mail (read-only), plus:
   - Passwort (required, min 8 chars)
   - Passwort wiederholen
   - Telefon (optional, pre-filled if admin provided)
4. Submit → Identity user password set → token consumed → login cookie set
5. Redirect to `/Membership/Profile`

## What It Looks Like (Text-Mockup)

### Admin Invite Form (`/Membership/Members/Invite`)

```
┌──────────────────────────────────────────────────────────┐
│  Mitglied(er) anlegen & einladen                         │
│                                                          │
│  Hauptmitglied                                           │
│  ──────────────────────────────────────────────────      │
│  Vorname *   [_________________]  Nachname * [______]    │
│  E-Mail *    [_________________]                         │
│  Geburtsdatum * [__/__/____]  → (Alter: 42)             │
│  Telefon     [_________________]                         │
│                                                          │
│  Adresse                                                 │
│  ──────────────────────────────────────────────────      │
│  Straße      [_________________]                         │
│  PLZ         [______]  Stadt    [_________________]      │
│                                                          │
│  Beitragsgruppe                                          │
│  ──────────────────────────────────────────────────      │
│  Typ * [▼ Familie ─────────────────────────────────]     │
│                                                          │
│  Eintrittsdatum * [01.09.2026]                           │
│                                                          │
│  ┌─ Familienmitglieder ───────────────────────────┐      │
│  │ Mitglied 1 (Hauptmitglied) ✅                   │      │
│  │                                                 │      │
│  │ [−] Mitglied 2                                  │      │
│  │   Vorname * [______]  Nachname * [________]     │      │
│  │   E-Mail *  [______]  Geburtsdatum * [__/__]    │      │
│  │   Verhältnis: [▼ Kind]                         │      │
│  │                                                 │      │
│  │ [−] Mitglied 3                                  │      │
│  │   Vorname * [______]  Nachname * [________]     │      │
│  │   E-Mail *  [______]  Geburtsdatum * [__/__]    │      │
│  │   Verhältnis: [▼ Ehepartner]                   │      │
│  │                                                 │      │
│  │ [+ Familienmitglied hinzufügen]                 │      │
│  └─────────────────────────────────────────────────┘      │
│                                                          │
│  [✓ Mitglied(er) anlegen & einladen]  [Abbrechen]        │
└──────────────────────────────────────────────────────────┘
```

### Magic-Link Acceptance (`/Membership/Activate?token=***`)

```
┌──────────────────────────────────────────────┐
│  Registrierung abschließen                   │
│                                              │
│  Vorname:     [Max] ✔                       │
│  Nachname:    [Mustermann] ✔                │
│  E-Mail:      [max@example.com] ✔            │
│  *Diese Daten wurden von der Verwaltung      │
│   hinterlegt und können hier nicht geändert   │
│   werden. Bei Fehlern wenden Sie sich an den  │
│   Vorstand.*                                  │
│                                              │
│  Passwort *   [_________________________]    │
│  Passwort wiederholen * [_______________]    │
│                                              │
│  Telefon (optional) [___________________]    │
│                                              │
│  [✓ Registrierung abschließen]               │
└──────────────────────────────────────────────┘
```

## Data Model Impact

**Neu einzuführen:**

```csharp
// Neue Entität: Invitation
public class Invitation
{
    public Guid Id { get; set; }
    public int MemberId { get; set; }          // FK → Member
    public string Token { get; set; }           // crypto-random, e.g. 256-bit URL-safe base64
    public DateTime ExpiresAt { get; set; }    // CreatedAt + 7 days
    public DateTime? CompletedAt { get; set; } // null until activated
    public Member Member { get; set; }
}

// Neue Tabelle: eine Migration erstellt `Membership.Invitations`.
```

**Keine Änderungen an bestehenden Modellen** — alle nötigen Beziehungen existieren bereits:
- `Member.GuardianMemberId` (FK → Member) für Youth
- `Member.FamilyGroupId` (FK → FamilyGroup) für Family
- `Member.MembershipFeeId` (FK → MembershipFee) mit `FeeCategory`

**Neue Migration:** nur `Invitations`-Tabelle.

## What Does NOT Change

- **Admin CRUD (`/Membership/Members/Create`)** bleibt wie bisher — der direkte Admin-Weg
- **FamilyGroup-Verwaltung** bleibt eigene Seite (Admin erstellt Gruppen separat)
- **Fee-Category-Validierung** bleibt wie implementiert (Youth → guardian required, Family → min 2 members)
- **Kein Zahlungs-/Beitrags-Flow** — separates Feature
- **DSGVO: Erasure/Export** bleibt Phase 2

## Why This Approach?

Das bestehende Membership-Modul hat bereits ein robustes Datenmodell mit FamilyGroup, Guardian-Self-Reference und FeeCategory-Validierung. Der Invite-Flow baut darauf auf, ohne bestehende Strukturen zu ändern.

Der Magic-Link-Ansatz ist sicherer als temporäre Passwörter: der Token ist zeitlich begrenzt, kryptographisch zufällig, und setzt voraus, dass der Empfänger Zugriff auf sein Postfach hat.

Piranha Identity wird für das Shared Auth genutzt — der Member existiert als Identity User, sobald er sein Passwort gesetzt hat. Bis dahin existiert der Member-Datensatz bereits (Status Active), aber der Identity User hat kein Passwort.

## Dependencies

- Datenbank-Migration für `Invitations`-Tabelle
- `InvitationService` (Token generieren/validieren/consumieren)
- E-Mail-Versand (nutzt existierenden ClubSite-MailService)
- `MembersController.Invite` — neue Action (POST)
- `Activate` — neue Razor Page `/Membership/Activate`
- JavaScript für Conditional-Fields: Geburtsdatum → Guardian (Youth), FeeCategory=Family → inline sub-members
- Piranha Identity User Creation beim Token-Accept (Member existiert bereits, Identity User wird verknüpft)

## Out of Scope (Explicit)

- Family-Management-UI für Member (eigene FamilyGroup-Editierbarkeit)
- Beitrags-/Zahlungsintegration
- Mitgliedsausweis-Generierung (PDF)
- Import per CSV/Excel
- DSGVO Export/Erasure Endpoints (separate Phase 2 KR)
- Historie der Admin-Aktionen (wer hat Invite gesendet)
- Soft-Delete / Papierkorb (bereits vorhanden)
- Bild-Upload (Mitgliedsfoto)

---

*Status: Draft — ready for axuno review*