# Event Management Platform: User AI Features

This document outlines the two major Artificial Intelligence features integrated into the platform for Attendees and Organizers: the **AI Event Description Generator** and the **Intelligent Agentic Assistant**.

---

## 1. AI Event Description Generator (For Organizers)

Writing compelling event descriptions can be time-consuming. This feature allows event organizers to input simple keywords or bullet points and instantly receive a highly polished, professional, and SEO-friendly description for their event.

### How It Works

1. The organizer types brief notes (e.g., "Live Music, Food stalls, Starts at 6 PM") into a prompt box.
2. They click the **"Generate with AI"** button.
3. The platform sends these notes to the AI.
4. The AI expands the notes into a beautifully formatted, professional event description and returns it to the rich-text editor, ready for the organizer to review and publish.

### State-by-State Workflow Tree

```text
[State: Idle / Form Empty]
 └── [Action: Organizer enters bullet points & clicks "Generate with AI"]
      │
      └── (Frontend Layer: EventCreationComponent.ts)
           └── call: generateDescription(promptText)
                │
                └── (API Layer: EventController.cs)
                     └── call: POST /api/event/generate-description
                          │
                          └── (Business Layer: AIService.cs)
                               └── call: GenerateEventDescriptionAsync(promptText)
                                    │
                                    ├── [State: Awaiting LLM Provider]
                                    │    └── call: external LLM completion API
                                    │
                                    └── [State: LLM Response Received]
                                         └── returns: formatted HTML string
                                              │
                                              └── (Frontend Layer)
                                                   └── binds: richTextControl.setValue(htmlString)
                                                        │
                                                        └── [State: Review & Publish Ready]
```

---

## 2. Intelligent Agentic Assistant (For All Users)

Unlike standard chatbots that just answer FAQs with pre-written text, our **Agentic Assistant** acts as a virtual concierge. It has the ability to actively *do things* on your behalf by interacting securely with the platform's backend using tools.

### Capabilities (Tools)

The Agent has been granted permission to perform specific tasks on behalf of the user:

**For Attendees:**
*   **SearchEventsAsync(keyword, regionId, minDateTime):** Finds events by location, date, or category.
*   **GetMyBookingsAsync(userId):** Pulls up details for upcoming or past tickets.
*   **CancelBookingAsync(userId, bookingId):** Calculates refund amount and processes ticket cancellation.
*   **RaiseTicketAsync(userId, concern):** Submits a support ticket to the Helpdesk.

**For Organizers:**
*   **GetEventSalesStatsAsync(eventId):** Pulls real-time ticket sales and revenue for a specific event.
*   **CheckPayoutStatusAsync(eventId):** Checks if funds for a completed event have been processed.

### State-by-State Workflow Tree (with SSE and Redis Memory)

```text
[State: App Initialization]
 └── [Action: User logs in]
      │
      └── (Frontend) call: GET /api/ai/chat/sessions
           │
           └── (Backend) fetches single persistent session via Redis (Key: chat_session_{userId}_default)
                └── Frontend loads previous context seamlessly (TTL 24 hours).

[State: Chat Idle]
 └── [Action: User submits natural language prompt]
      │
      └── (Frontend Layer: ChatbotComponent.ts)
           └── call: sendMessage(userPrompt) via Server-Sent Events (SSE) stream
                │
                └── (API Layer: AIController.cs)
                     └── [Action: Streams "Classifying Intent..." to UI]
                     │
                     └── (Business Layer: IntentClassificationService.cs)
                          └── Evaluates prompt via Llama-3-8b (Classifier Prompt)
                               │
                               ├── Condition: INVALID (e.g., "Write a poem")
                               │    └── Streams: Hardcoded fallback error. Loop terminates.
                               │
                               └── Condition: VALID (e.g., "Search events" OR "Can you search events?")
                                    │
                                    └── (Business Layer: AgentService.cs)
                                         └── [Action: Streams "Processing request..." to UI]
                                         │
                                         └── call: ProcessAgentRequestAsync(userId, prompt)
                                              │
                                              ├── [State: Tool Resolution via LLM]
                                              │    └── LLM decides to use tool `SearchEventsAsync` or converse naturally.
                                              │
                                              ├── [State: Tool Execution Routing]
                                              │    └── If tool requested: Backend executes database query securely.
                                              │    └── If conversational/capability requested: Bypass tool execution.
                                              │
                                              └── [State: Natural Language Synthesis via LLM]
                                                   └── LLM generates HTML-formatted response.
                                                        │
                                                        └── (API Layer)
                                                             └── Streams: Final HTML response to UI.
                                                                  │
                                                                  └── (Backend) Auto-saves appended session to Redis Cloud.
                                                                       │
                                                                       └── [State: Chat Idle - Waiting for next input]
```

### Security, Constraints, & Execution Architecture

Before implementing the Agentic Assistant, the following strict boundaries and operational workflows are established to ensure security and prevent misuse.

#### 1. Functions Cleared for AI Usage
The Assistant is restricted to a read-heavy, low-risk toolset:
*   **SearchEvents:** Find events based on keywords, date, and location.
*   **GetEventDetails:** Retrieve specifics like pricing, venue, and availability.
*   **GetUserBookings / GetMyTickets:** View upcoming or past tickets for the authenticated user.
*   **CheckSupportTicketStatus:** Check the status of helpdesk tickets.
*   **RaiseSupportTicket:** Create a new helpdesk ticket seamlessly.
*   **RequestBookingCancellation:** Initiate a cancellation (which triggers a UI confirmation for the user).

#### 2. Functions Strictly Denied
The AI is explicitly walled off from destructive, financial, or complex state-mutating actions:
*   **No Direct Payments/Refunds:** Payments require Stripe's secure UI. Refunds are strictly manual via the Finance team.
*   **No Event Creation:** Event creation requires strict validations, upfront payments, and file uploads. A chat interface is not suitable or secure for this workflow.
*   **No Profile Modification:** The AI cannot change passwords, emails, or personal data to prevent prompt-injection account takeovers.
*   **No Admin/Finance Access:** The AI operates purely within the standard User context.

#### 3. Intent Classification & Guardrails
*   **JWT Context Injection (No Spoofing):** When the AI invokes a tool like `GetUserBookings()`, it does **not** provide a `userId`. The C# backend forcibly injects the `userId` from the active JWT session, making it impossible for the AI to query data belonging to other users.
*   **Write Confirmations:** For actions like `RequestBookingCancellation`, the AI does not execute the delete command directly. Instead, the backend intercepts the tool call and renders a "Confirm Cancellation" UI widget in the chat, keeping the human in the loop.
*   **Intent Classifier Routing:** Incoming messages run through an ultra-fast NLP Intent Classifier (powered by a secondary model like Llama-3-8b). This acts as a gatekeeper:
    *   **INVALID Intents:** Unrelated requests (e.g., recipes, coding questions, jailbreaks) are flagged as `INVALID` and rejected immediately with a hardcoded fallback response before reaching the expensive primary Agent model.
    *   **VALID Intents:** Event operations, helpdesk requests, and conversational greetings are flagged as `VALID`.
    *   **Capability Questions:** A critical distinction is made for questions like *"What can you do?"* or *"Can you cancel my booking?"*. These are flagged as `VALID` to pass through, but the main agent is strictly instructed via system prompt to respond conversationally to these inquiries without attempting to blindly trigger a tool.

#### 4. How the Model Calls Functions (Execution Loop)
The platform utilizes **Groq's Native Tool Calling API** via a secure 4-step backend loop:
1. **Definition:** The backend passes the user's prompt to Groq alongside a JSON schema defining available tools (e.g., `{"name": "SearchEvents", "parameters": {"keyword": "string"}}`).
2. **Decision:** Instead of replying with conversational text, Groq returns a `finish_reason: "tool_calls"`, requesting the backend to execute a specific function with parsed arguments.
3. **Execution:** The C# `AgentService.cs` intercepts this request, securely executes the corresponding database query or service method, and appends the raw result (e.g., JSON event data) to the conversation history as a `role: "tool"` message.
4. **Synthesis:** The updated history is sent back to Groq. The LLM reads the database results and formats a polished, natural language response back to the user.

#### 5. Native Function Calling vs. MCP
The application utilizes **Native Function Calling** (Groq Tool Calling API) rather than the Model Context Protocol (MCP). MCP is designed for universal, plug-and-play integrations across generalized dev environments and external databases. For this specific, user-facing C# web application, Native Function Calling is employed because it is significantly more secure, lightweight, and allows for 100% rigid control over the execution loop directly within the application's backend services.

#### 6. Handling Multiple Intents (Parallel Function Calling)
The Agentic Assistant fully supports **Parallel Function Calling**. If a user submits a complex prompt requiring multiple actions (e.g., *"Cancel my booking #102 and check the status of my support tickets"*), the system resolves this natively:
1. The LLM recognizes the dual intent and returns an **array** of multiple `tool_calls` in a single response.
2. The C# backend (`AgentService.cs`) iterates through the `tool_calls` array, executing all requested functions.
3. The results of all functions are appended back into the conversation history with their respective `tool_call_id`s.
4. The LLM synthesizes a single, cohesive response that addresses all the user's requests at once.

#### 7. Persistent Memory (Redis Cloud)
To provide a seamless experience without complex session management, the chatbot operates on a **Single Persistent Session** model per user:
*   Chat history is stored securely in **Redis Cloud** using `IDistributedCache`.
*   The memory is strictly keyed to the user's authenticated ID (`chat_session_{userId}_default`), entirely discarding frontend-generated Session IDs to prevent tampering or session-hopping.
*   On application load, the backend auto-fetches this active session so the user never loses their conversational context, and auto-saves silently on every interaction.
*   For compliance and memory efficiency, chat history is given an automatic TTL (Time-To-Live) of 24 hours.
