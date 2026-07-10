# Admin Authentication Module

This module governs all operations relating to Administrator authentication, password reset, and session generation.

## 1. Files & Components Involved

### Controllers
* **DeptAuthController.cs**
  * **Path:** [DeptAuthController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/DeptAuthController.cs)
  * **Admin Authentication Endpoints:**
    * `POST api/auth/admin/login` (Admin login - returns JWT immediately on success)
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
> **Cross-role login is strictly forbidden.** The service enforces this as the **very first step** of every auth call — before any database query is executed:
> - An Admin login attempt using a `FIN_` prefixed ID is immediately rejected with `401 Unauthorized`.
> - A Finance login attempt using an `ADM_` prefixed ID is immediately rejected with `401 Unauthorized`.

This ensures no information leakage (the DB is never consulted when the prefix is wrong) and completely segregates the two authentication portals.

---

## 3. Activity & State Flowcharts

### I. Admin Login Flow
Generates a secure administrative JWT session upon verification of credentials. Unlike Finance, the Admin login does not trigger an OTP step — it returns the JWT immediately after credential validation.

```text
                         [ START ADMIN LOGIN ]
                                   │
                                   ▼
                    [ Enter AdminId & Password ]
                                   │
                                   ▼
                ┌──────────────────────────────────────┐
                │  STEP 1: ID Prefix Guard             │
                │  Does AdminId start with "ADM"?      │
                └──────────────────────────────────────┘
                         /                    \
                [No]    /                      \ [Yes]
                       ▼                        ▼
           (401 Unauthorized)        [ Fetch Admin Record by ID ]
      "Finance credentials cannot                │
       be used on admin portal"                  ▼
                                    { Admin Record Found? }
                                     /                  \
                            [No]    /                    \ [Yes]
                                   ▼                      ▼
                        (401 Unauthorized)       [ Verify Password Hash ]
                    "Invalid credentials"         /                   \
                                        [No]    /                     \ [Yes]
                                               ▼                       ▼
                                   (401 Unauthorized)      [ Generate JWT Token ]
                                "Invalid credentials"      - Role claim: "admin"
                                                                       │
                                                                       ▼
                                                            [ Return JWT Token ]
                                                                       │
                                                                       ▼
                                                             [ END / Success ]
```

---

### II. Admin Password Reset Flow
Manages requesting a verification code and resetting the administrator password securely via a Redis cache.

```text
                       [ START PASSWORD RESET ]
                                   │
                                   ▼
                     [ Send Request to ForgotPassword ]
                     - Requires email address
                                   │
                                   ▼
                    [ Fetch Admin Record by Email ]
                                   │
                                   ▼
                          { Admin Exists? }
                           /             \
                  [No]    /               \ [Yes]
                         ▼                 ▼
                (Throw NotFound)   [ Generate 6-digit OTP ]
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
                                    /                         \
                           [No]    /                           \ [Yes]
                                  ▼                             ▼
                         (Throw Unauthorized)          [ Delete Key from Redis ]
                                                                │
                                                                ▼
                                                       [ Hash New Password ]
                                                                │
                                                                ▼
                                                       [ Update Admin Record ]
                                                                │
                                                                ▼
                                                        [ END / Reset Success ]
```
