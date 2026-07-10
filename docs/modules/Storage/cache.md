# Distributed Caching Module (Redis)

This module governs all distributed caching operations on the platform. It abstracts data persistence for short-lived state variables (such as verification OTPs) using Redis cache to ensure scalability, thread safety, and auto-clearing expiration.

## 1. Files & Components Involved

### Contracts & Interfaces
* **ICacheService.cs**
  * **Path:** [ICacheService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/ICacheService.cs)
  * **Operations:**
    * `SetAsync<T>(string key, T value, TimeSpan? expiration)` (Serializes and persists key/value)
    * `GetAsync<T>(string key)` (Fetches and deserializes cached keys)
    * `RemoveAsync(string key)` (Removes key from cache manually)

### Services & Implementation
* **CacheService.cs**
  * **Path:** [CacheService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/CacheService.cs)
  * Implements `ICacheService` utilizing ASP.NET Core `IDistributedCache` abstraction.

### Configurations & Registration
* **Program.cs** -> [Program.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Program.cs#L69-L73)
  * Registers `AddStackExchangeRedisCache` mapping to Redis instance connection.
  * Registers `ICacheService` as scoped service.
* **appsettings.json** -> [appsettings.json](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/appsettings.json#L9-L12)
  * Configures Connection String `"Redis": "localhost:6379"`.

---

## 2. Key Architecture Details

* **Data Abstraction:** Business services only reference `ICacheService`, keeping implementation details hidden from high-level features.
* **Serialization Protocol:** Objects are converted to JSON strings using `System.Text.Json.JsonSerializer` before being stored in Redis, and parsed back during retrieval.
* **Automatic Expiration:** Lifetime management (Time To Live) is handled automatically on the Redis side via `AbsoluteExpirationRelativeToNow` settings. Once the cache entry reaches its TTL threshold, Redis automatically evicts the record.

---

## 3. Operations Flowcharts

### I. Cache Set Flow
```text
                            [ START CACHE SET ]
                                     │
                                     ▼
                      [ Receive Key, Value, Expiry ]
                                     │
                                     ▼
                            { Is Key Empty? }
                             /             \
                    [Yes]   /               \ [No]
                           ▼                 ▼
                       (Return)      [ Initialize Options ]
                                             │
                                             ▼
                                     { Expiry Provided? }
                                      /                \
                             [No]    /                  \ [Yes]
                                    ▼                    ▼
                                    │         [ Set AbsoluteExpiration ]
                                    │         [   RelativeToNow = Expiry   ]
                                    │                    │
                                    ▼                    ▼
                                    └──────────┬─────────┘
                                               │
                                               ▼
                                     [ Serialize Value ]
                                     - System.Text.Json
                                               │
                                               ▼
                                     [ Save String to Redis ]
                                     - IDistributedCache.SetStringAsync
                                               │
                                               ▼
                                        [ END / Success ]
```

---

### II. Cache Get Flow
```text
                            [ START CACHE GET ]
                                     │
                                     ▼
                               [ Receive Key ]
                                     │
                                     ▼
                            { Is Key Empty? }
                             /             \
                    [Yes]   /               \ [No]
                           ▼                 ▼
                    (Return Default)  [ Query String from Redis ]
                                      - IDistributedCache.GetStringAsync
                                             │
                                             ▼
                                      { Found String? }
                                       /             \
                              [No]    /               \ [Yes]
                                     ▼                 ▼
                              (Return Default) [ Deserialize JSON payload ]
                                               - System.Text.Json
                                                       │
                                                       ▼
                                               [ Return Value ]
                                                       │
                                                       ▼
                                                [ END / Success ]
```

---

### III. Cache Eviction / Removal Flow
```text
                          [ START CACHE EVICTION ]
                                     │
                                     ▼
                               [ Receive Key ]
                                     │
                                     ▼
                            { Is Key Empty? }
                             /             \
                    [Yes]   /               \ [No]
                           ▼                 ▼
                       (Return)      [ Send Eviction Command to Redis ]
                                     - IDistributedCache.RemoveAsync
                                             │
                                     ┌───────┴───────┐
                                     ▼               ▼
                              [ Key Deleted ]  [ Key Not Found ]
                                     └───────┬───────┘
                                             │
                                             ▼
                                     [ END / Removed ]
```
