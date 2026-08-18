# FleetPulse — Smart Fleet Dispatch Console

**Module:** PRG2781 — Project (100 marks)
**Domain:** Smart Transport / Fleet Management

## 1. System Overview

FleetPulse simulates a dispatch centre managing a mixed fleet of **Trucks**, **Vans**
and **Buses**, plus a pool of **Drivers**, running **Route jobs** between cities.
It provides a menu-driven console interface layered on top of a background
"live" simulation:

- A background thread continuously ticks every 3 seconds, draining fuel on
  en-route vehicles, occasionally triggering breakdowns, flagging vehicles
  due for maintenance, and auto-completing a portion of in-progress routes —
  simulating real-time fleet activity independent of user input.
- The user can create vehicles/drivers/routes, assign routes, view live
  fleet/route state, pull LINQ-based reports, and save/load the entire
  fleet state to a JSON file.

**Unique elements for this build:**
- System name: **FleetPulse**
- Randomised seed data: vehicle plates, capacities, routes and priorities are
  randomly generated on startup (see `Program.SeedRandomData`)
- Domain-specific rules:
  1. A vehicle that is overdue for a service (mileage ≥ its service
     threshold) cannot be dispatched on a new route.
  2. A driver cannot be assigned a route that would push their cumulative
     hours for the day past a 10-hour daily limit.
- Custom feature (not covered in course material): a colour-coded live event
  feed (breakdowns in red, fuel warnings in yellow, maintenance due in
  magenta, deliveries in green) driven entirely by the custom event/delegate
  system, plus the auto-completing route simulation running on its own
  background thread.

## 2. How to Run

Requires the .NET 10 SDK.

```bash
cd FleetPulse
dotnet run
```

On first run FleetPulse seeds a small random fleet (1 truck, 1 van, 1 bus),
2 drivers, and 3 pending routes, then starts background monitoring
automatically. Use the numbered menu to interact with the system.

A typical first session:
1. Option `1` — view the seeded fleet
2. Option `7` — view the seeded routes and their IDs
3. Option `6` — assign a route to a vehicle and driver (try an ID combo,
   watch for domain-rule exceptions if hours/mileage don't allow it)
4. Wait a few seconds and watch the background thread raise events in the
   console as fuel drops, routes auto-complete, etc.
5. Option `9` / `10` — save and reload fleet state from `fleetpulse_state.json`

## 3. Project Structure

```
The project is organized into clear, logical namespaces to separate data models, services, and user interface components:

*   **`Enums/`**: Enumerations for strict typing.
    *   `MainMenuOptions.cs`
    *   `VehicleTypeOption.cs`
*   **`Exceptions/`**: Custom exceptions enforcing strict business rules.
    *   `DriverHourLimitExceededException.cs`: Thrown when a driver exceeds maximum allowable working hours.
    *   `FleetCapacityExceededException.cs`: Thrown when fleet storage or capacity limits are reached.
    *   `InvalidRouteAssignmentException.cs`: Thrown for invalid job assignments.
*   **`Interfaces/`**: Defines contracts to ensure standardized behavior.
    *   `IMaintainable.cs`: Contract for entities requiring maintenance.
    *   `ITrackable.cs`: Contract for entities that can be tracked on a route.
*   **`Models/`**: Contains the core business entities representing the fleet.
    *   `Vehicle.cs` (Base class) with inherited classes: `Bus.cs`, `Truck.cs`, `Van.cs`
    *   `Driver.cs`
    *   `RouteJob.cs`
    *   `MaintenanceRecord.cs`
*   **`Services/`**: Contains the core business logic and background tasks.
    *   `DispatchCenter.cs`: Manages vehicle dispatching and real-time background monitoring.
    *   `FileManager.cs`: Handles data persistence (saving/loading fleet data).
    *   `ReportService.cs`: Generates operational reports.
*   **`UI/`**
    *   Handles all console interactions and rendering.
    *   `DriverUI.cs`: Interface screens for driver management.
    *   `MenuRender.cs`: Core menu drawing and navigation logic.
    *   `RouteUI.cs`: Interface screens for route assignments.
    *   `VehicleUI.cs`: Interface screens for vehicle management.
*   **Root Files**
    *   `Program.cs`: The main entry point. Initializes the application and controls the background monitoring threads.
    *   `fleetpulse_state.json`: The local data store utilized by `FileManager.cs` to persist fleet data between sessions.
    *   `dotnet-install.sh`: Environment setup script.
```

## 4. Key Design Decisions

- **Abstraction & Polymorphism:** `Vehicle` is abstract with an abstract
  `CalculateFuelConsumption(distanceKm)` method. Each subtype (Truck, Van,
  Bus) implements its own consumption formula and overrides `DisplayInfo()`
  to show type-specific detail — the dispatch logic never needs to know
  which concrete type it's holding.
- **Encapsulation:** Mutable state that must stay consistent (`Mileage`,
  `FuelLevel`) has private setters and is only changed through controlled
  methods (`Drive`, `AdjustFuel`, `ScheduleMaintenance`), preventing external
  code from putting a vehicle into an invalid state.
- **Interfaces as contracts, not decoration:** `ITrackable` and
  `IMaintainable` are implemented meaningfully by `Vehicle` — dispatch
  reporting queries `IsDueForService()` through the interface method, and
  the design leaves room to add non-vehicle trackable/maintainable entities
  later without touching `DispatchCenter`.
- **Custom exceptions carry domain meaning:** rather than generic
  `Exception`, failures are typed (`FleetCapacityExceededException`,
  `InvalidRouteAssignmentException`, `DriverHourLimitExceededException`) so
  the menu loop's `catch` blocks can react and message appropriately per
  failure type, and so the exceptions are genuinely raised for **logical**
  errors (e.g. exceeding a driver's daily hours), not just crash prevention.
- **Persistence is decoupled from the domain model:** `FileManager` uses
  plain DTOs (`VehicleSnapshot`, `DriverSnapshot`) rather than serializing
  `Vehicle` directly, so the domain classes stay focused on behaviour and
  don't need to accommodate serialization concerns. Loading a snapshot
  currently reconstructs vehicles/drivers with new IDs — a known,
  documented simplification.

## 5. Multithreading Explained

`DispatchCenter.StartMonitoring()` launches a `Task.Run` background loop
(`MonitorLoop`) that runs independently of the console's input-reading main
thread. Every 3 seconds it:
1. Takes a thread-safe snapshot of the fleet (`lock (_lock) { ... ToList() }`)
2. Drains fuel on en-route vehicles, randomly triggers breakdowns, and flags
   maintenance-due vehicles — raising events as it goes
3. Occasionally completes a random in-progress route via `CompleteRoute`,
   which itself takes its own lock only around the shared-state mutation and
   raises `DeliveryCompleted` **after** releasing the lock

All access to the four shared collections (`_fleet`, `_drivers`, `_routes`,
`_maintenanceLog`) goes through a single `lock (_lock)` — both from the main
thread (handling menu input) and the monitoring thread — so simultaneous
reads/writes never race. Events are always invoked outside the lock to avoid
a subscriber (e.g. the console logger) blocking, or deadlocking against, a
dispatch operation. `CancellationTokenSource`/`CancellationToken` allow the
thread to be stopped cleanly via the menu (option 11) or on exit, rather than
being killed abruptly.

## 6. Events & Delegates Explained

Two custom delegate types are declared explicitly (`VehicleEventHandler`,
`RouteEventHandler`) rather than relying purely on generic `Action<T>`, to
make the publisher/subscriber contract self-documenting. `DispatchCenter`
exposes four events built on them:

- `BreakdownDetected` — raised when a random in-transit failure occurs
- `FuelLowWarning` — raised when an en-route vehicle's fuel drops ≤15%
- `MaintenanceDue` — raised when a vehicle's mileage passes its service threshold
- `DeliveryCompleted` — raised when a route finishes (manually or via the background thread)

`Program.SubscribeToEvents()` wires all four to a single `LogEvent` helper
that colour-codes console output per event type. `DispatchCenter` (the
publisher) has no knowledge of `Program` (the subscriber) — it only knows it
has subscribers, which is the decoupling the brief asks for.

## 7. Known Simplifications (worth mentioning in the viva)

- Loading a saved snapshot rebuilds vehicles/drivers with fresh IDs rather
  than preserving the originals exactly — a deliberate scope decision to
  keep persistence simple, easily extendable by adding an ID-preserving
  constructor path if needed.
- Driving-hour estimates for the daily-limit rule assume a flat 60km/h
  average rather than per-route speed data.
