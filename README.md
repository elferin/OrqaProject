# Drone Flight Module

A minimal Unity prototype of a drone flight module with gatekept authorization, swappable control sources and a deletable telemetry dashboard. Built with Unity 6000.6.3f1: open `Assets/Scenes/SampleScene.unity` and press Play.

## Boundaries

Each module is its own assembly, and references point one way only:

```
Drone.App ──────┬──> Drone.Providers ──> Drone.Core
                └──────────────────────> Drone.Core
Drone.Frontend ────────────────────────> Drone.Core
Drone.Dashboard ───────────────────────> Drone.Core
```

| Assembly | Responsibility | Unity dependency |
|---|---|---|
| `Drone.Core` | Authorization gate, flight controller and profile, input router, session, interfaces | None |
| `Drone.Providers` | Guest and simulated remote authorization, autopilot | None |
| `Drone.App` | `Bootstrapper`, the single composition root, and its assets | Yes |
| `Drone.Frontend` | Keyboard source, operator controls, drone view | Yes |
| `Drone.Dashboard` | Telemetry readout | Yes |

- **One composition root, no singletons.** `Bootstrapper` builds the gate, router, controller and session, and finds clients on its children through `IDroneFrontend` and `IDroneDisplay`. It references neither frontend assembly, so deleting one needs no code changes. The keyboard source is the one exception: `DroneFrontend` creates it, because `Drone.App` can't reference that assembly.
- **Flight only knows `IFlightClearance` and `IFlightInputSource`.** It never sees a provider or where its input comes from. Sources can be added, removed and swapped at runtime.
- **Displays are read-only by type.** The dashboard is attached with `IDroneReadout` and can't reach `Arm` or `Revoke`. Operator frontends get the full session.
- **State changes are pushed, telemetry is pulled.** Auth, flight and input changes raise events. The dashboard's handler only sets a dirty flag, so it never runs UI code inside the flight tick. Altitude and speed change continuously, so it polls them at 10 Hz.
- **Hardware tuning is data.** A `FlightProfile` asset holds acceleration, speeds and yaw rate. A new airframe is a new asset assigned on `Boot`.

## Adding a third authorization provider

Implement `IAuthorizationProvider`, add a small `AuthorizationProviderFactory` subclass in `Drone.App`, create an asset from it, and add that asset to `Boot`. No flight, gate or bootstrapper code changes, and the provider gets the next digit key. A provider should honour its `CancellationToken` and own its timeout.

## Async safety and failure

- **No `await` in the gate.** It holds the provider's `Task` and checks it in `Tick`. State only changes inside the gate's own calls (`Request`, `Revoke` and `Tick`), which all run on Unity's main thread, never on the thread that finished the task.
- **The newest request wins.** It cancels the previous attempt, and late results are ignored.
- **Failures fail closed.** A thrown or faulted attempt becomes `Failed`. Losing clearance in flight makes the drone land itself.
- **Bad input becomes neutral input.** A source that throws or returns NaN or infinity is reported as a fault on the dashboard, and the drone hovers.
- **One failing client can't stop the others.** A frontend or factory that throws is logged and dropped.
- **Shutdown cancels everything.** Destroying `Boot` detaches every client, then disposes the session, which cancels any pending attempt.

## Running without UI

Delete `Assets/Code/Drone/Dashboard` with `Assets/Prefabs/Resources/Dashboard`, and `Assets/Code/Drone/Frontend` with `Assets/Prefabs/DroneFrontend`. Tick **Auto Start** on `Boot`. It calls `DroneSession.AuthorizeAndArm`, which goes through the same gate and arms once cleared.

On 2026-11-03, a Windows player with no frontend or dashboard in the scene was run with `-batchmode -nographics`, and its graphics device reported `Null`. With the guest provider it held 3.00 m from 4 s. With the simulated remote provider it waited about 3 s, then reached 3.00 m. Deleting each frontend's code was checked separately, in the Editor.

## Performance

Flight, authorization and input don't allocate per tick. The dashboard only rebuilds text when a shown value changes, and numbers use TextMesh Pro's `SetText`, which doesn't allocate in a player. That test only runs from the Test Runner's **Player** tab, because `SetText` allocates in the Editor.

## Controls

| Key | Action |
|---|---|
| 1-9 | Request that authorization provider |
| R | Revoke; a flying drone lands |
| F / G | Arm / disarm, or land when flying |
| Tab | Swap between autopilot and keyboard |
| W A S D, Space, Left Shift, Q / E | Move, climb, descend, yaw |

Press `1`, then `F`, and the autopilot climbs to 3 m.

## Tests

**EditMode** covers Core and Providers. **PlayMode** covers Frontend, Dashboard and the bootstrapper. Each frontend's tests sit in its own folder, so they're deleted with it.

## Known limits

- **Kinematic simulation:** no collisions, wind or physics engine, so Core runs and tests without Unity.
- **Requesting a slow provider in flight lands the drone,** because clearance drops while it's `Authorizing`. This fails closed.
- **Event subscribers must not throw.** Auth, flight and input events fire immediately inside whichever call changed the state, such as `Request`, `Revoke`, `Arm` or `Tick`. A throwing handler interrupts that call, and if the call came from a frontend, `Bootstrapper` drops that frontend.
- **Movement is world-relative,** and the autopilot only controls altitude.
