using System;
using Drone.Contracts;

namespace Drone.Core.Session {

    internal sealed class ReadOnlyDroneReadout : IDroneReadout {

        private readonly IDroneReadout _inner;

        public AuthState AuthState => _inner.AuthState;
        public string FailureReason => _inner.FailureReason;
        public TelemetrySnapshot Telemetry => _inner.Telemetry;
        public string ActiveSource => _inner.ActiveSource;
        public bool InputFaulted => _inner.InputFaulted;

        public event Action AuthStateChanged {
            add => _inner.AuthStateChanged += value;
            remove => _inner.AuthStateChanged -= value;
        }

        public event Action FlightStateChanged {
            add => _inner.FlightStateChanged += value;
            remove => _inner.FlightStateChanged -= value;
        }

        public event Action InputChanged {
            add => _inner.InputChanged += value;
            remove => _inner.InputChanged -= value;
        }

        public ReadOnlyDroneReadout(IDroneReadout inner) {
            _inner = inner;
        }
    }
}