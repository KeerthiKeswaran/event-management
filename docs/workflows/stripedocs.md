# Stripe Payment Integration & Testing Documentation

This document provides a comprehensive overview of how Stripe payments are handled in the event ticketing platform, including the concept of escrow and testing configurations.

---

## 1. The Escrow Concept
In our marketplace platform, the money from ticket sales is not sent directly to the event organizer immediately. Instead, we use an **escrow model** managed via Stripe:

```
[Attendee (Payer)]
       |
       | 1. Charges payment (Full ticket price)
       v
[Platform Stripe Account (Escrow)]
       |
       |-- (Deducts platform commission/fixed fees)
       |
       | 2. Payouts/Transfers (Remaining balance)
       v
[Organizer Stripe Account]
```

1. **Intake**: Attendee card is debited; money sits securely in the platform's central balance.
2. **Safety**: If the event is cancelled, refunds are easily issued directly from the platform escrow balance.
3. **Payout**: Once the event succeeds, the platform triggers a transfer to move the earnings (minus commission) to the organizer's bank account.

---

## 2. Stripe Test Mode Flow (sk_test_...)
In **Test Mode**, Stripe simulates its entire banking, card, and redirect networks. No real money changes hands, but all API requests, ledger transactions, and webhook notifications behave identically to the live production environment.

---

## 3. Detailed Payment Method Handling

Here is how each payment method is processed in real production compared to how we test it in Test Mode:

### 1. Credit & Debit Cards
* **In Production**: Stripe contacts the card networks (Visa, Mastercard, American Express, etc.) and the customer's issuing bank to check for sufficient funds and perform security checks (like CVV validation and 3D Secure OTP popups).
* **In Our Test Case**: Stripe ignores bank checks. Instead, we use specific Stripe Test Cards to simulate different behaviors:
  - **To simulate success**: Enter card number `4242 4242 4242 4242` with any future expiry date and any CVV. Stripe will instantly return a successful charge.
  - **To simulate a decline**: Enter card number `4000 0000 0000 0022`. Stripe will throw a `StripeException` simulating a card decline.
  - **To simulate an expired card**: Enter card number `4000 0000 0000 0030`.

### 2. UPI (Unified Payments Interface)
* **In Production**: Because UPI requires the customer to open GPay, PhonePe, or BHIM and enter their UPI PIN, Stripe cannot charge it instantly.
  1. Stripe generates a UPI Payment Intent.
  2. The customer sees a popup or gets a redirect link on their phone.
  3. The customer approves the transaction in their UPI app.
  4. The UPI network notifies Stripe, and Stripe fires a Webhook (an HTTP POST notification) to our backend to say: *"The payment is now complete!"*
* **In Our Test Case**: Stripe provides a test UPI ID:
  - **To simulate success**: Use `success@stripeupi`. Stripe bypasses GPay/PhonePe and immediately marks the payment as succeeded.
  - **To simulate failure**: Use `fail@stripeupi`. Stripe immediately marks it as failed.

### 3. Apple Pay & Google Pay
* **In Production**: The user's phone encrypts their card details securely. Apple/Google sends a one-time cryptographic token to Stripe. Stripe decrypts it and processes it just like a normal credit card transaction.
* **In Our Test Case**: Stripe's SDK detects if you are in test mode and displays a mock Apple Pay/Google Pay sheet. When you click "Pay," it sends a mock token to our backend, which our service charges successfully without requiring face ID/fingerprint authentication.

### 4. Redirect Payments (e.g. NetBanking, PayPal, Klarna)
* **In Production**: Stripe redirects the user to their bank's login page. Once they log in and approve the transfer, the bank redirects them back to our site, and Stripe notifies our backend.
* **In Our Test Case**: When you initiate a redirect payment in test mode, Stripe redirects you to a Stripe Simulation Page. This page contains two simple buttons:
  - **[Authorize Test Payment]**: Simulates the customer logging into their bank and approving the payment.
  - **[Fail Test Payment]**: Simulates the customer rejecting the payment or closing the window.

Clicking either button will immediately trigger the corresponding success or failure callback on our backend server!

---

## 4. Summary of Test Inputs

Use the following parameters when testing different payment methods in the checkout interface:

| Payment Method | Input Parameter | Expected Test Outcome |
| :--- | :--- | :--- |
| **Credit / Debit Card** | `4242 4242 4242 4242` | Successful payment. (Use any future Expiry and any CVV) |
| **Credit / Debit Card** | `4000 0000 0000 0022` | Card declined (fails transaction). |
| **Credit / Debit Card** | `4000 0000 0000 0030` | Card expired (fails transaction). |
| **UPI** | `success@stripeupi` | Successful payment (bypasses PIN verification). |
| **UPI** | `fail@stripeupi` | Failed payment. |
| **Apple Pay / Google Pay** | Click test sheet button | Successful payment. |
| **Redirect Methods** | Select bank/NetBanking | Click **[Authorize Test Payment]** to succeed, or **[Fail Test Payment]** to fail. |

---

## 5. Postman API Request Examples

When testing the `confirm-payment` API endpoint directly in Postman (`POST http://localhost:5106/api/booking/{bookingId}/confirm-payment`), use the following JSON request bodies:

### A. Credit & Debit Cards (Successful)
```json
{
    "stripeChargeId": "tok_visa",
    "paymentMethod": "Visa - **** 4242"
}
```

### B. Credit & Debit Cards (Declined / Failed)
```json
{
    "stripeChargeId": "tok_chargeDeclined",
    "paymentMethod": "Visa - **** 0022"
}
```

### C. UPI (Successful Simulation)
```json
{
    "stripeChargeId": "tok_visa",
    "paymentMethod": "UPI - success@stripeupi"
}
```

### D. Apple Pay / Google Pay (Successful)
```json
{
    "stripeChargeId": "tok_visa",
    "paymentMethod": "Apple Pay"
}
```


