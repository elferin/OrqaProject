using System;
using Drone.Core.Authorization;
using Drone.Core.Flight;

namespace Drone.Core.Session {

    public interface IDroneReadout {
        AuthState AuthState { get; }
        string FailureReason { get; }
        TelemetrySnapshot Telemetry { get; }
        IFlightInputSource ActiveSource { get; }
        bool InputFaulted { get; }
        event Action AuthStateChanged;
        event Action FlightStateChanged;
        event Action InputChanged;
    }
}
