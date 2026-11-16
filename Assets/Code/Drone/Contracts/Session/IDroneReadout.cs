using System;

namespace Drone.Contracts {

    public interface IDroneReadout {
        AuthState AuthState { get; }
        string FailureReason { get; }
        TelemetrySnapshot Telemetry { get; }
        string ActiveSource { get; }
        bool InputFaulted { get; }
        event Action AuthStateChanged;
        event Action FlightStateChanged;
        event Action InputChanged;
    }
}
