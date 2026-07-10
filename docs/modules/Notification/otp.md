# OTP Verification Module

This module governs the security verification operations utilizing One-Time Passwords (OTP) sent to user emails, backed by a Redis-based distributed cache for verification and automatic expiration.

## 1. OTP Scope & Purpose Keys
To prevent cross-flow reuse and race conditions, the platform implements unique, purpose-scoped Redis keys:

| Flow Context | Trigger Point | Verification Point | Redis Key Format | TTL Expiry |
|---|---|---|---|---|
| **User Registration** | `POST api/auth/user/send-otp` | `POST api/auth/user/register` | `otp:registration:{email}` | 10 Minutes |
| **User Password Reset** | `POST api/auth/user/send-otp` | `POST api/auth/user/reset-password` | `otp:password-reset:{email}` | 10 Minutes |
| **Finance Login** | `POST api/auth/finance/login` | `POST api/auth/finance/login/verify` | `otp:finance-login:{email}` | 10 Minutes |
| **Admin/Finance PW Reset** | `POST api/auth/forgot-password` | `POST api/auth/reset-password` | `otp:admin-password-reset:{email}` | 10 Minutes |

---

## 2. Activity & State Flowcharts

### I. Registration Verification Flow
```text
                         [ START REGISTRATION ]
                                    │
                                    ▼
                      [ Send OTP code to Email ]
                      - Scope: "registration"
                                    │
                                    ▼
                      [ Cache in Redis (10 Min) ]
                      - Key: otp:registration:{email}
                                    │
                                    ▼
                     [ Enter OTP at /register Endpoint ]
                                    │
                                    ▼
                     [ Verify via OtpService ]
                                    │
                                    ▼
                     { OTP Matches and Active? }
                      /                       \
             [No]    /                         \ [Yes]
                    ▼                           ▼
            (Throw Unauthorized)         [ Delete Key in Redis ]
                                                │
                                                ▼
                                         [ Register User ]
                                                │
                                                ▼
                                         [ END / Registered ]
```

---

### II. Password Reset Flow (User)
```text
                       [ START USER RESET PW ]
                                  │
                                  ▼
                      [ Send OTP code to Email ]
                      - Scope: "password-reset"
                                  │
                                  ▼
                      [ Cache in Redis (10 Min) ]
                      - Key: otp:password-reset:{email}
                                  │
                                  ▼
                   [ Enter OTP at /reset-password ]
                                  │
                                  ▼
                     [ Verify via OtpService ]
                                  │
                                  ▼
                     { OTP Matches and Active? }
                      /                       \
             [No]    /                         \ [Yes]
                    ▼                           ▼
            (Throw Unauthorized)         [ Delete Key in Redis ]
                                                │
                                                ▼
                                         [ Save New Hash ]
                                                │
                                                ▼
                                         [ END / Success ]
```

---

### III. Finance Login Verification Flow
```text
                        [ START FINANCE AUTH ]
                                  │
                                  ▼
                     [ POST api/auth/finance/login ]
                     - Triggers OTP generation
                     - Scope: "finance-login"
                                  │
                                  ▼
                      [ Cache in Redis (10 Min) ]
                      - Key: otp:finance-login:{email}
                                  │
                                  ▼
                   [ Enter OTP at /login/verify ]
                                  │
                                  ▼
                     [ Verify via OtpService ]
                                  │
                                  ▼
                     { OTP Matches and Active? }
                      /                       \
             [No]    /                         \ [Yes]
                    ▼                           ▼
            (Throw Unauthorized)         [ Delete Key in Redis ]
                                                │
                                                ▼
                                         [ Issue Admin JWT ]
                                                │
                                                ▼
                                         [ END / Success ]
```

---

### IV. Admin / Finance Password Reset Flow
```text
                      [ START ADMIN RESET PW ]
                                  │
                                  ▼
                     [ POST api/auth/forgot-password ]
                     - Triggers OTP generation
                     - Scope: "admin-password-reset"
                                  │
                                  ▼
                      [ Cache in Redis (10 Min) ]
                      - Key: otp:admin-password-reset:{email}
                                  │
                                  ▼
                   [ Enter OTP at /reset-password ]
                                  │
                                  ▼
                     [ Verify via OtpService ]
                                  │
                                  ▼
                     { OTP Matches and Active? }
                      /                       \
             [No]    /                         \ [Yes]
                    ▼                           ▼
            (Throw Unauthorized)         [ Delete Key in Redis ]
                                                │
                                                ▼
                                         [ Update Password ]
                                                │
                                                ▼
                                         [ END / Success ]
```
