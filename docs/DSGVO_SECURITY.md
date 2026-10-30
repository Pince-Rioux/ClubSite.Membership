# VereinMember — DSGVO & Security Concept

## Compliance Framework

This module handles **personenbezogene Daten** (Art. 4(1) DSGVO) of Verein members. The following measures implement the **DSGVO principles** (Art. 5) and **security requirements** (Art. 32).

---

## 1. Data Minimization (Art. 5(1)(c))

| Measure | Implementation |
|---|---|
| Only essential fields | Data dictionary defines required vs optional fields. `Notes` field discouraged for sensitive data |
| No redundant storage | Member data stored only in Membership schema. No duplicate contact info in Piranha tables |
| Configurable form fields | Optional fields can be hidden via configuration — only show what the Verein actually needs |
| Guardian link stored only for Youth | `GuardianMemberId` populated only when the member's fee category is Youth |
| Family group data stored only for Family | `FamilyGroupId` populated only when the member's fee category is Family |

---

## 2. Purpose Limitation (Art. 5(1)(b))

Every field in the data dictionary has a documented purpose:

- **member management** — Name, Contact, BirthDate, JoinedAt
- **dues & contributions** — BankIban, BankBic, MembershipFee
- **guardian relationship** — GuardianMemberId (Youth safety / legal representation)
- **family grouping** — FamilyGroupId (family discount / joint communication)
- **communication** — Email, Phone
- **statistics** — BirthDate, Department, FeeCategory (aggregated/anonymized)
- **audit** — CreatedAt, UpdatedAt, CreatedBy

No field is stored "just in case."

---

## 3. Storage Limitation (Art. 5(1)(e))

| Data category | Retention | After retention |
|---|---|---|
| Active member data | Duration of membership + 3 years (statute of limitations for dues claims) | Hard erasure of personal fields |
| Former member data | 3 years after `LeftAt` | Hard erasure of personal fields |
| Guardian link data | Retained with the Youth member's data | Erased with dependent |
| Family group data | Retained until last family member is erased | Group dissolved automatically |
| Audit log entries | 3 years (accountability) | Anonymous aggregation (entity counts only) |
| SEPA mandate data | Duration + 3 years (permanent retention required by ZKA) | Keep reference but no payment execution |

**Implementation:**
- Background job (Hangfire / IHostedService) checks weekly for expired records
- Purge sets fields to NULL/empty and logs the erasure
- Audit log retains only: "Member #123 was erased on 2026-08-25 by Admin"

---

## 4. Access Control (Art. 5(1)(f) — Integrity & Confidentiality)

### Role Model

| Role | Scope | DSGVO-level access |
|---|---|---|
| **Admin** | Full access, including erasure | All fields readable |
| **Editor** | Create, update, soft-delete members | All fields except sensitive audit metadata |
| **Viewer** | Read-only member list and details | All fields visible but IBAN masked (`DE89 **** **** **** **** 00`) |
| **System** | Automated jobs only | Read access to IBAN for SEPA export only |

**Enforcement:**
- Server-side `[Authorize]` attributes on all controllers/pages
- Policy-based: `MemberView`, `MemberWrite`, `MemberDelete`, `AdminOnly`
- Policy checks performed by middleware, not UI hiding
- CSRF protection via `[AutoValidateAntiforgeryToken]` on all POST endpoints

---

## 5. Encryption (Art. 32(1)(a))

### At Rest

| Level | Implementation |
|---|---|
| Database-level | SQL Server TDE (operational responsibility of host — documented requirement) |
| Column-level (IBAN/BIC) | `Always Encrypted` or custom AES-256-GCM encryption at application level |
| Connection string | Stored in encrypted configuration file, never hardcoded |

### In Transit

| Channel | Measure |
|---|---|
| Web UI | HTTPS enforced (ClubSite already redirects to HTTPS) |
| API | HTTPS only |
| DB connection | `Encrypt=True; TrustServerCertificate=False` in connection string |

---

## 6. Audit Logging (Art. 5(2) — Accountability)

Every mutation to personal data is logged:

```json
{
  "entityName": "Member",
  "entityId": 42,
  "action": "Updated",
  "changedFields": ["Phone", "Street", "GuardianMemberId"],
  "oldValues": {
    "Phone": "+49 173 1111111",
    "Street": "Alte Str. 1",
    "GuardianMemberId": null
  },
  "newValues": {
    "Phone": "+49 173 2222222",
    "Street": "Neue Str. 10",
    "GuardianMemberId": 17
  },
  "performedBy": "admin@verein.de",
  "performedAt": "2026-08-25T08:00:00Z"
}
```

**Special cases:**
- **Erasure:** Logs "action": "Erasure" with no old/new values — just reference ID and timestamp
- **Guardian changes:** Always logged as changed field (sensitive relationship data)
- **Family group changes:** Always logged as changed field
- **View/read:** Not logged (would create excessive data)
- **Export:** Logged as "action": "Exported" with entity count and format

The audit log is **append-only** — no UPDATE or DELETE operations allowed on AuditLog table.

---

## 7. Data Breach Response (Art. 33-34)

| Level | Process |
|---|---|
| Detection | Audit log reviewed for anomalies; failed auth attempts logged |
| Notification (Art. 33) | Automated alert to Admin email on 5+ failed access attempts or bulk export |
| Documentation | Incident template documented; all breaches logged internally regardless of notification obligation |

---

## 8. Data Subject Rights (Art. 15-21)

| Right | Endpoint | SLA |
|---|---|---|
| Right of access (Art. 15) | `GET /api/members/{id}/export` | Within 48h (automated) |
| Right to rectification (Art. 16) | `PUT /api/members/{id}` | Immediate |
| Right to erasure (Art. 17) | `DELETE /api/members/{id}/erasure` | Within 1 month; automated with confirmation |
| Right to data portability (Art. 20) | `GET /api/members/{id}/export?format=json` | Same as access |

**Guardian-specific:** Erasure of a guardian is blocked while dependent Youth members exist (Art. 17(3)(b) — legal obligation).

---

## 9. Privacy by Design & Default (Art. 25)

- New members default to **Active** status — no excessive data collection
- Bank details collected only when the Verein uses SEPA
- Guardian field only shown when fee category is Youth (conditional UI)
- Family group field only shown when fee category is Family (conditional UI)
- IBAN masked in all UI views (Viewer role sees only last 4 digits)
- Search does not index `Notes` field
- Export formats exclude fields the requesting role cannot see

---

## 10. Processing Register (Art. 30)

The required "Verarbeitungsverzeichnis" entry for this module:

```
Verantwortlicher: [Vereinsname] e.V., vertreten durch den Vorstand
Zweck: Mitgliederverwaltung, Beitragseinzug (SEPA), 
       Jugendbetreuung (Erziehungsberechtigten-Nachweis), 
       Familienrabatt-Verwaltung, Vereinskommunikation
Kategorien: Stammdaten, Kontaktdaten, Bankdaten, Beitrittsdaten,
            Erziehungsberechtigten-Zuordnung, Familiengruppen-Zuordnung
Empfänger: Keine Weitergabe an Dritte; Vorstand und beauftragte Funktionäre
Speicherdauer: Mitgliedschaft + 3 Jahre
Technisch-organisatorische Maßnahmen: Siehe dieses Dokument
```

---

## 11. Security Checklist (Operational)

| Check | Status | Verified |
|---|---|---|
| HTTPS only | ClubSite default | ✓ |
| HSTS enabled | ClubSite default | ✓ |
| CSRF protection on all POST | `AutoValidateAntiforgeryToken` | planned |
| SQL injection prevention | Parameterized queries (EF Core) | ✓ (by design) |
| XSS protection | Razor auto-encoding | ✓ (by design) |
| Rate limiting on auth endpoints | TBD | planned |
| Session timeout | 60 min (ClubSite default) | ✓ |
| Secure cookies | SameSite=Strict, HttpOnly | ✓ |
| No sensitive data in URLs | All IDs via POST body or route params | planned |
| Guardian re-assignment before erasure | Blocked if dependents exist | planned |
| Dependency vulnerability scanning | `dotnet list package --vulnerable` | CI planned |
| Penetration testing | TBD | post-Phase 2 |