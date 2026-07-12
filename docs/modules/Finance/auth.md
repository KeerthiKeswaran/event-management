# Finance Authentication Module

This module governs all operations relating to Finance team authentication. Unlike the Admin portal, Finance login is a **two-step process**: credentials are validated first, then a One-Time Password (OTP) is dispatched to the registered email and must be verified to obtain a JWT session token.

## 1. Files & Components Involved

### Controllers
* **DeptAuthController.cs**
  * **Path:** [DeptAuthController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/DeptAuthController.cs)
  * **Finance Authentication Endpoints:**
    * `POST api/auth/finance/login` (Step 1 — validates credentials and dispatches OTP)
    * `POST api/auth/finance/login/verify` (Step 2 — verifies OTP and returns JWT)
    * `POST api/auth/forgot-password` (Sends password reset OTP)
    * `POST api/auth/reset-password` (Performs password reset using verification OTP)

### Contracts & Interfaces
* **IDeptAuthService.cs** -> [IDeptAuthService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IDeptAuthService.cs)
* **ICacheService.cs** -> [ICacheService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/ICacheService.cs)
* **IAdminRepository.cs** -> [IAdminRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/IAdminRepository.cs)

### Services
* **DeptAuthService.cs** -> [DeptAuthService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/DeptAuthService.cs)
* **OtpService.cs** -> [OtpService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/OtpService.cs)
* **CacheService.cs** -> [CacheService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/CacheService.cs)

---

## 2. ID Convention & Security Rules

Both Administrator and Finance team accounts are stored in the same `Admins` table but are **distinguished by their ID prefix**:

| Role    | ID Prefix | Example        |
|---------|-----------|----------------|
| Admin   | `ADM`     | `ADM_10042`    |
| Finance | `FIN`     | `FIN_20817`    |

> [!IMPORTANT]
> **Cross-role login is strictly forbidden.** The service enforces this as the **very first step** of every Finance auth call — before any database query is executed and, critically, **before any OTP is sent**:
> - A Finance login attempt (`/finance/login`) using an `ADM_` prefixed ID is immediately rejected with `401 Unauthorized` — no OTP is dispatched.
> - A Finance OTP verification attempt (`/finance/login/verify`) using an `ADM_` prefixed ID is immediately rejected with `401 Unauthorized` — the OTP cache is never consulted.

This design prevents Admin credentials from being used to trigger OTP emails and ensures the Finance login portal is exclusively accessible to `FIN_` accounts.

---

## 3. Activity & State Flowcharts

### I. Finance Login — Step 1: Credential Validation & OTP Dispatch

The first step validates the Finance ID and password. If both are correct, a 6-digit OTP is sent to the registered email address. **No JWT is returned at this stage.**

```text
                        [ START FINANCE LOGIN — STEP 1 ]
                                        │
                                        ▼
                        [ Enter FinanceId & Password ]
                                        │
                                        ▼
                ┌────────────────────────────────────────────┐
                │  STEP 1: ID Prefix Guard (Pre-DB Check)   │
                │  Does FinanceId start with "FIN"?         │
                └────────────────────────────────────────────┘
                          /                          \
                 [No]    /                            \ [Yes]
                        ▼                              ▼
            (401 Unauthorized)           [ Fetch Finance Record by ID ]
        "Admin credentials cannot                     │
         be used on finance portal"                   ▼
                                         { Finance Record Found? }
                                          /                      \
                                 [No]    /                        \ [Yes]
                                        ▼                          ▼
                             (401 Unauthorized)         [ Verify Password Hash ]
                         "Invalid credentials"           /                    \
                                                [No]    /                      \ [Yes]
                                                       ▼                        ▼
                                           (401 Unauthorized)   [ Generate 6-digit OTP ]
                                       "Invalid credentials"              │
                                                                          ▼
                                                             [ Store OTP in Redis ]
                                                             - Key: otp:finance-login:{email}
                                                             - Expiration: 10 minutes
                                                                          │
                                                                          ▼
                                                             [ Send OTP Email via Brevo ]
                                                                          │
                                                                          ▼
                                                         [ Return { OtpRequired: true } ]
                                                                          │
                                                                          ▼
                                                              [ END STEP 1 / Proceed to Step 2 ]
```

---

### II. Finance Login — Step 2: OTP Verification & JWT Issuance

The second step verifies the OTP submitted by the Finance user. If valid and within the expiry window, a signed JWT with the `finance` role claim is returned.

```text
                     [ START FINANCE LOGIN — STEP 2 ]
                                     │
                                     ▼
                      [ Enter FinanceId & OTP Code ]
                                     │
                                     ▼
                ┌──────────────────────────────────────────┐
                │  STEP 1: ID Prefix Guard (Pre-DB Check) │
                │  Does FinanceId start with "FIN"?       │
                └──────────────────────────────────────────┘
                          /                         \
                 [No]    /                           \ [Yes]
                        ▼                             ▼
            (401 Unauthorized)         [ Fetch Finance Record by ID ]
      "Only finance accounts can                     │
       complete OTP verification"                    ▼
                                        { Finance Record Found? }
                                         /                      \
                                [No]    /                        \ [Yes]
                                       ▼                          ▼
                            (401 Unauthorized)       [ Verify OTP via Redis Cache ]
                        "Invalid credentials"        - Key: otp:finance-login:{email}
                                                      /                          \
                                             [No]    /                            \ [Yes / Not Expired]
                                                    ▼                              ▼
                                        (401 Unauthorized)            [ Invalidate OTP in Redis ]
                                     "Invalid or expired OTP"                     │
                                                                                  ▼
                                                                    [ Generate JWT Token ]
                                                                    - Role claim: "finance"
                                                                                  │
                                                                                  ▼
                                                                    [ Return JWT Token ]
                                                                                  │
                                                                                  ▼
                                                                     [ END / Login Success ]
```

---

### III. Finance Password Reset Flow

Manages requesting a verification code and resetting the Finance account password securely via a Redis cache.

> [!NOTE]
> The password reset flow is shared across both Admin and Finance roles. It operates via email address rather than account ID, and uses the `admin-password-reset` OTP context.

```text
                       [ START PASSWORD RESET ]
                                   │
                                   ▼
                     [ Send Request to ForgotPassword ]
                     - Requires registered email address
                                   │
                                   ▼
                    [ Generate 6-digit OTP ]
                                   │
                                   ▼
                    [ Store OTP in Redis ]
                    - Key: otp:admin-password-reset:{email}
                    - Expiration: 10 minutes
                                   │
                                   ▼
                    [ Send OTP Email via Brevo ]
                                   │
                                   ▼
                    [ Enter OTP & New Password ]
                                   │
                                   ▼
                    [ Verify via Redis Cache ]
                    - Compare with cached OTP
                                   │
                                   ▼
                    { OTP Matches & Not Expired? }
                     /                             \
            [No]    /                               \ [Yes]
                   ▼                                 ▼
        (Throw Unauthorized)             [ Delete Key from Redis ]
    "Invalid or expired OTP"                        │
                                                    ▼
                                           [ Hash New Password ]
                                                    │
                                                    ▼
                                           [ Fetch Account by Email ]
                                                    │
                                                    ▼
                                           [ Update Password Hash ]
                                                    │
                                                    ▼
                                            [ END / Reset Success ]
```
