# VereinMember — Use Cases

## Actors

| Actor | Description |
|---|---|
| **Admin** | Vorstand / board member with full access |
| **Editor** | Schriftführer / treasurer who manages member data |
| **Viewer** | Limited read-only access (e.g. trainer who needs contact info) |
| **Member** | Verein member who views/edits their own profile (Phase 5) |
| **System** | Automated processes (SEPA export, statistics) |

---

## UC-01: Add New Member

**Actor:** Admin, Editor  
**Priority:** P0 — Phase 1

**Preconditions:** Actor is authenticated with role Admin or Editor.

**Main flow:**
1. Actor navigates to `/Membership/Members/Create`
2. System displays the member creation form
3. Actor enters: FirstName, LastName, Email, Phone, BirthDate, Address, Department, MembershipFee
4. **Fee branch:**
   - If fee category is **Youth**: the form shows a guardian selector (dropdown of active Adult members). Actor selects the guardian. System validates the guardian exists and is an Adult-category member.
   - If fee category is **Family**: the form shows a family group selector (existing groups) or "Create new family" option.
   - If fee category is **Adult**: no extra fields.
5. Actor optionally enters BankIban/BankBic, Notes
6. System validates: Email uniqueness, required fields, IBAN format, guardian requirement
7. Actor submits
8. System saves Member record, logs creation (user, timestamp, field values)
9. System redirects to Member detail page with success message

**Postconditions:** Member exists in DB, audit log entry created. Youth member linked to guardian. Family member linked to family group.

**Security:**
- IBAN encrypted at column level
- Role check enforced server-side
- Guardian relationship audited

---

## UC-02: Register Youth Member with Guardian

**Actor:** Admin, Editor  
**Priority:** P0 — Phase 1

**Description:** Special case of UC-01. A Youth-category member must always be linked to an Adult guardian.

**Validation rules:**
- `GuardianMemberId` is required when `MembershipFee.Category == Youth`
- The guardian member must have `MembershipFee.Category == Adult`
- A guardian can have multiple Youth dependents
- Self-referencing as guardian is forbidden
- Soft-deleting a guardian is blocked while dependent Youth members exist

**Postconditions:** Youth member shows guardian reference on detail page. Guardian profile shows list of dependent members.

---

## UC-03: Create / Manage Family Groups

**Actor:** Admin  
**Priority:** P0 — Phase 1

**Description:** Family members belong to the same Family Group. A family group requires ≥ 2 members.

**Main flow:**
1. Actor navigates to `/Membership/FamilyGroups`
2. Lists all family groups with member count
3. Can create new group (optional name, auto-generated label based on member surnames)
4. Can add/remove members from a group
5. Deleting a group: system warns if members lose their family link, blocks if any family-category member would become orphaned
6. Minimum 2 members per group enforced server-side

---

## UC-04: Update Member Data

**Actor:** Admin, Editor  
**Priority:** P0 — Phase 1

**Main flow:**
1. Actor navigates to `/Membership/Members/Edit/{id}`
2. System displays pre-filled form with current data
3. Actor modifies fields
4. **If fee category changed:** Re-validate guardian/family rules
5. System validates changed fields
6. Actor submits
7. System computes field diff, creates audit log with old/new values
8. System saves and redirects to detail page

---

## UC-05: View Member List

**Actor:** Admin, Editor, Viewer  
**Priority:** P0 — Phase 1

**Main flow:**
1. Actor navigates to `/Membership/Members`
2. System displays paginated, searchable, filterable table
3. Filters: by Department, Status, Fee category (Adult/Youth/Family), free-text search
4. Actor clicks a row to view full member details
5. List shows: Name, Email, Status, Department, Fee category, Guardian (for Youth), Family Group (for Family)

---

## UC-06: Manage Membership Fees

**Actor:** Admin  
**Priority:** P1 — Phase 1

**Main flow:**
1. Actor navigates to `/Membership/Fees`
2. CRUD interface for MembershipFee (Name, Amount, Interval, Category)
3. Categories: **Adult** (standard), **Youth** (requires guardian), **Family** (grouped)
4. Default fees: Adult (standard rate), Youth (reduced rate), Family (per-person or flat rate)

---

## UC-07: Manage Departments

**Actor:** Admin  
**Priority:** P1 — Phase 1

**Main flow:**
1. Actor navigates to `/Membership/Departments`
2. CRUD interface for Department (Name, Description)
3. Delete restricted if members are still assigned

---

## UC-08: View Member Details (with relationships)

**Actor:** Admin, Editor, Viewer  
**Priority:** P0 — Phase 1

**Details page shows:**
- Standard member info (name, contact, status, fee, department)
- If Youth: "Guardian: [Name]" with link to guardian profile
- If Youth: "Dependents: [List]" (shown on guardian's profile)
- If Family: "Family Group: [Name]" with list of other family members
- Direct link to guardian/family group

---

## UC-09: Export Member List (PDF/CSV/vCard)

**Actor:** Admin, Editor  
**Priority:** P2 — Phase 2

**Main flow:**
1. Actor selects members via filters
2. Clicks "Export as PDF" / "Export as CSV" / "Export as vCard"
3. System generates and returns file

**DSGVO:** Export includes only fields actor is authorized to see.

---

## UC-10: DSGVO Data Export (Member Request)

**Actor:** Member (future), Admin  
**Priority:** P2 — Phase 2

**Main flow:**
1. Actor requests data export for a Member
2. System returns structured JSON with all personal data the system holds
3. Includes guardian relationship and family group membership info

---

## UC-11: DSGVO Right to Erasure

**Actor:** Admin  
**Priority:** P2 — Phase 2

**Main flow:**
1. Actor confirms identity and legal basis for erasure
2. System checks: if member has dependent Youth members, erasure is blocked until re-assignment
3. System performs hard delete of personal data fields
4. Audit log retains anonymous reference (deleted user ID, timestamp)
5. Irreversible — confirmation dialog requires explicit checkbox

---

## UC-12: SEPA Direct Debit Export

**Actor:** Admin, Editor  
**Priority:** P3 — Phase 3

**Main flow:**
1. Actor selects contribution period
2. System generates `pain.008.001.02` XML SEPA file
3. File contains: Member name, IBAN, amount, mandate reference
4. Actor downloads and uploads to bank portal

---

## UC-13: BLSV / DOSB Membership Statistics

**Actor:** Admin  
**Priority:** P3 — Phase 3

**Main flow:**
1. Actor selects reporting year
2. System generates statistics: total members, by department, by age group, by fee category, by gender
3. Exports as PDF or XML (BLSV format)

---

## UC-14: Guardian Re-assignment

**Actor:** Admin  
**Priority:** P2 — Phase 2

**Main flow:**
1. Guardian member leaves the Verein or requests deactivation
2. System blocks soft-delete: "This member is a guardian for N Youth members. Re-assign them first."
3. Admin re-assigns each dependent to a new guardian
4. After re-assignment, original guardian can be soft-deleted

---

## Non-Functional Requirements

| ID | Requirement | Rationale |
|---|---|---|
| NFR-1 | Page load < 200ms for list views | Member list should not degrade with 1000+ records |
| NFR-2 | API response < 100ms for CRUD | Acceptable UX for REST consumers |
| NFR-3 | Audit log retained min. 3 years | DSGVO Art. 5(1)(e) accountability |
| NFR-4 | Soft delete, no hard delete from UI | Prevent accidental data loss |
| NFR-5 | All DB access through parameterized queries | Prevent SQL injection |
| NFR-6 | German locale (date, number, currency format) | Verein context |

## Data Dictionary

### Member

| Field | Type | Required | DSGVO Purpose | Retention |
|---|---|---|---|---|
| Id | int (PK) | auto | internal reference | permanent |
| FirstName | nvarchar(100) | yes | member management | until erasure |
| LastName | nvarchar(100) | yes | member management | until erasure |
| Email | nvarchar(200) | yes | contact, dues communication | until erasure |
| Phone | nvarchar(50) | no | contact | until erasure |
| BirthDate | date | yes | age group stats, member ID | until erasure |
| Street | nvarchar(200) | no | postal contact | until erasure |
| City | nvarchar(100) | no | postal contact | until erasure |
| ZipCode | nvarchar(20) | no | postal contact | until erasure |
| Country | nvarchar(100) | no | postal contact | until erasure |
| BankIban | nvarchar(34) | no* | SEPA direct debit | encrypted column |
| BankBic | nvarchar(11) | no* | SEPA direct debit | encrypted column |
| JoinedAt | datetime2 | yes | membership tracking | permanent |
| LeftAt | datetime2 | no | membership tracking | permanent |
| Status | nvarchar(20) | yes | membership classification | permanent |
| DepartmentId | int (FK) | no | organizational grouping | until erasure |
| MembershipFeeId | int (FK) | yes | dues calculation | until erasure |
| **GuardianMemberId** | **int (FK → Member)** | **conditional** | **Youth guardian link (only if FeeCategory Youth)** | **until dependent erasure** |
| **FamilyGroupId** | **int (FK → FamilyGroup)** | **conditional** | **Family grouping (only if FeeCategory Family)** | **until group erasure** |
| Notes | nvarchar(max) | no | internal notes | until erasure |
| CreatedAt | datetime2 | auto | audit | permanent |
| UpdatedAt | datetime2 | auto | audit | permanent |
| CreatedBy | nvarchar(100) | auto | audit | permanent |

\* Required only when SEPA will be used.

### Department

| Field | Type | Notes |
|---|---|---|
| Id | int (PK) | auto |
| Name | nvarchar(100) | e.g. "Volleyball", "Beach", "Jugend" |
| Description | nvarchar(500) | optional |

### MembershipFee

| Field | Type | Notes |
|---|---|---|
| Id | int (PK) | auto |
| Name | nvarchar(100) | e.g. "Adult Standard", "Youth Reduced", "Family Flat" |
| Amount | decimal(18,2) | monetary amount |
| Interval | nvarchar(20) | Monthly, Quarterly, Yearly |
| **Category** | **nvarchar(20)** | **Adult / Youth / Family** |
| IsDefault | bit | first fee pre-selected in creation form |

### FamilyGroup

| Field | Type | Notes |
|---|---|---|
| Id | int (PK) | auto |
| Name | nvarchar(200) | auto-generated: "Familie Müller" or custom |
| CreatedAt | datetime2 | auto |
| Members | collection | 1-to-many FK on Member.FamilyGroupId |