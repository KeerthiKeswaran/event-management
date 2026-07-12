# User Authentication Module

This module handles attendee registration, email verification (OTP), login authentication, and password recovery.

## 1. Files & Components Involved

### Controllers
* **UserAuthController.cs**
  * **Path:** [UserAuthController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/UserAuthController.cs)
  * **Endpoints:**
    * `POST api/auth/user/send-otp`
    * `POST api/auth/user/register`
    * `POST api/auth/user/login`
    * `POST api/auth/user/reset-password`

### Contracts & Interfaces
* **IUserAuthService.cs**
  * **Path:** [IUserAuthService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IUserAuthService.cs)
  * **Methods:**
    * `Task<string?> RegisterUserAsync(User user, string password, string otp)`
    * `Task<string?> LoginUserAsync(string email, string password)`
    * `Task<string> ResetUserPasswordAsync(string email, string otp, string newPassword)`

### Services
* **UserAuthService.cs**
  * **Path:** [UserAuthService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/UserAuthService.cs)
  * **Details:** Implementation of the user registry and credential validation logic.
* **OtpService.cs**
  * **Path:** [OtpService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/OtpService.cs)
  * **Details:** Coordinates OTP generation, memory store storage, email notification dispatch, and code validation.

### Helpers & Security Utilities
* **PasswordHasher.cs**
  * **Path:** [PasswordHasher.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Helpers/PasswordHasher.cs)
  * **Details:** Uses PBKDF2/SHA256 with random salt to securely hash and verify passwords.
* **JwtTokenGenerator.cs**
  * **Path:** [JwtTokenGenerator.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Helpers/JwtTokenGenerator.cs)
  * **Details:** Generates signed JWT tokens with standard claims (`sub`, `email`, `jti`, and `Role: User`).

### DTOs & Models
* **DTOs Path:** [Event.Models/DTOs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Models/DTOs)
  * Models: `SendOtpRequest`, `RegisterRequest`, `LoginRequest`, `ResetPasswordRequest`, `EmailTemplateDto`.
* **Models Path:** [Event.Models/Models](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Models/Models)
  * Main Entity: `User`.

---

## 2. Activity & State Flowcharts

### I. OTP Dispatch Activity Flow
This activity flow validates email formats and checks database state conditions based on the requested purpose (`registration`, `password-reset`, or `admin-password-reset`) before generating and dispatching the OTP code.

```text
                       [ START SEND OTP ]
                                │
                                ▼
                   [ Read Email and Purpose DTO ]
                                │
                                ▼
                     { Is Email Null/Empty? }
                      /                  \
             [Yes]  /                      \ [No]
                   ▼                        ▼
           (Throw Validation)        { Check Purpose }
                                    /        |        \
                                  /          |          \
                 ["registration"]/           |           \ ["admin-password-reset"]
                                ▼            |            ▼
                       [Query User DB]       |     [Query Admin DB]
                              │      ["password-reset"]   │
                              ▼              │            ▼
                     { User exists? }        ▼     { Admin exists? }
                       /          \   [Query User DB]   /          \
               [Yes]  /        [No]\         │    [No] /        [Yes]\
                     ▼              ▼        ▼        ▼              ▼
             (Throw Conflict)       │  { User exists? }      (Throw NotFound)
                                    │    /         \                 │
                                    │[No]/         \[Yes]            │
                                    │   ▼           ▼                │
                                    │(Throw NF)     │                │
                                    ▼               ▼                ▼
                                    └───────┬───────┘                │
                                            └────────────────────────┘
                                                        │
                                                        ▼
                                             [ Generate 6-Digit OTP ]
                                                        │
                                                        ▼
                                             [ Cache OTP in Memory ]
                                             - Key: Email address
                                             - TTL: 10 minutes
                                                        │
                                                        ▼
                                             [ Build HTML Mail Body ]
                                             - Compile OtpEmailTemplate.html
                                             - Add purpose and expiration placeholders
                                                        │
                                                        ▼
                                             [ Send Email via SMTP ]
                                             - Relay via EmailService/Brevo
                                                        │
                                                        ▼
                                                 [ END / Success ]
```

---

### II. Attendee Registration State Flow
This flow represents the complete set of terms verification gates (ensuring the user accepts the active terms version) and optional configurations (like marketing consent opt-in) during attendee profile creation.

```text
                         [ START REGISTRATION ]
                                   │
                                   ▼
             [ Read Name, Email, Password, Otp, ConsentedTermsId ]
                                   │
                                   ▼
                       [ Step 1: Verify OTP Code ]
                                   │
                                   ▼
                       { Is OTP Valid and Active? }
                         /                      \
                [No]   /                          \ [Yes]
                      ▼                            ▼
            (Throw Unauthorized)         [ Step 2: Retrieve Active Terms ]
                                         - Query ITermsAndConditionsRepository
                                                    │
                                                    ▼
                                         { Active Terms Defined? }
                                           /                    \
                                  [No]   /                        \ [Yes]
                                        ▼                          ▼
                              (Throw Validation)        { ConsentedTermsId matches }
                                                        {   Active Terms PK?       }
                                                           /                    \
                                                  [No]   /                        \ [Yes]
                                                        ▼                          ▼
                                              (Throw Validation)          [ Step 3: Duplicate Check ]
                                                                          - Query IUserRepository
                                                                                     │
                                                                                     ▼
                                                                          { Email already exists? }
                                                                             /                  \
                                                                    [Yes]  /                      \ [No]
                                                                          ▼                        ▼
                                                                  (Throw Conflict)        [ Step 4: Map Consents ]
                                                                                          - Terms: Mandatory PK Consent
                                                                                          - Marketing: Optional Opt-In
                                                                                          - Booking: Share info with Host
                                                                                                    │
                                                                                                    ▼
                                                                                          [ Step 5: Secure Password ]
                                                                                          - Hash via PBKDF2/SHA256
                                                                                                    │
                                                                                                    ▼
                                                                                          [ Step 6: Create User Record ]
                                                                                          - Save to Database
                                                                                                    │
                                                                                                    ▼
                                                                                          [ Step 7: Issue Session Token ]
                                                                                          - Generate signed JWT
                                                                                                    │
                                                                                                    ▼
                                                                                           [ END / Registered & Logged In ]
```

---

### III. User Login Flow
Validates the login credentials against saved database records.

```text
                         [ START USER LOGIN ]
                                  │
                                  ▼
                       [ Read Email & Password ]
                                  │
                                  ▼
                        [ Fetch User by Email ]
                                  │
                                  ▼
                           { User Found? }
                            /           \
                   [No]    /             \ [Yes]
                          ▼               ▼
                (Throw Unauthorized)   { Verify Password Hash? }
                                        /                     \
                               [No]    /                       \ [Yes]
                                      ▼                         ▼
                            (Throw Unauthorized)        [ Generate User JWT ]
                                                        - Claims: sub, email, Role: User
                                                                │
                                                                ▼
                                                         [ END / Login OK ]
```

---

### IV. Password Recovery Flow
Performs verification checks and resets user passwords.

```text
                       [ START PASSWORD RESET ]
                                  │
                                  ▼
                     [ Read Email, Otp, NewPassword ]
                                  │
                                  ▼
                         { Verify OTP Code? }
                          /                \
                 [No]    /                  \ [Yes]
                        ▼                    ▼
              (Throw Unauthorized)   [ Fetch User by Email ]
                                             │
                                             ▼
                                      { User Account Found? }
                                       /                   \
                              [No]    /                     \ [Yes]
                                     ▼                       ▼
                             (Throw NotFound)        [ Generate New Hash ]
                                                     - Hash new password
                                                             │
                                                             ▼
                                                     [ Update Database ]
                                                     - Overwrite Password_Hash
                                                             │
                                                             ▼
                                                      [ END / Success ]
```
