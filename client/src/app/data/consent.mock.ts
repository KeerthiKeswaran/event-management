export interface ConsentDocument {
  id: number;
  title: string;
  lastUpdated: string;
  content: string;
}

export const mockTermsAndConditions: ConsentDocument = {
  id: 10000,
  title: "Terms and Conditions of Use",
  lastUpdated: "June 2026",
  content: `### 1. Acceptance of Terms
By creating an account on GetMyEvents ("Platform"), you agree to be bound by these Terms and Conditions. If you do not agree, you must not register or purchase any tickets.

### 2. Eligibility
You must be at least 18 years of age (or have explicit parental/guardian consent if the event requires) to register and use the services provided on our Platform.

### 3. User Accounts
You are responsible for safeguarding your login credentials. 
All details provided during registration must be accurate, complete, and updated. 
Multiple account creation or impersonation is strictly prohibited and will lead to deactivation.

### 4. Ticket Purchase, Convenience Fees & Cancellation
All purchases are subject to platform convenience fees. 
Cancellation refund rules are governed by the event organizers and the cancellation policy active at the time of purchase. 
Any ticket scalping, resale above face value, or unauthorized commercial use of tickets is strictly forbidden.

### 5. Code of Conduct
Attendees must behave in a respectful manner at all events, physical or virtual. Organizers reserve the right to deny check-in or request evacuation from the venue without refund if a user violates code of conduct guidelines.

### 6. Limitation of Liability
GetMyEvents is a platform facilitator. We are not responsible for venue scheduling issues, cancellations, quality of service, or physical accidents during events. All claims must be settled with the designated organizer.`
};

export const mockDataConsent: ConsentDocument = {
  id: 10001,
  title: "Data Consent and Privacy Policy",
  lastUpdated: "June 2026",
  content: `### 1. Scope of Data Processing
To facilitate ticket bookings and check-in validation, we collect and process the following: Profile Information (Name, Email, Mobile Number), Geographic Preferences (City/Region Selection), and Transaction Data (booking ticket quantities, payment reference status, platform fee cuts).

### 2. Sharing with Organizers
By registering and booking an event, you explicitly consent to sharing your Name, Email, and Check-In QR status with the specific Event Organizer for security, registration, and attendance verification purposes.

### 3. Cookies and Storage
We store configuration variables, session authentication tokens, and user preferences locally (via local storage and session variables) to authenticate requests and maintain state.

### 4. Security & Compliance (GDPR / CCPA)
Your personal details are encrypted in transit and at rest. We never sell user personal data to third parties. You have the right to request access to your personal data or close your account at any time, which purges all local storage and active session tokens immediately.`
};
