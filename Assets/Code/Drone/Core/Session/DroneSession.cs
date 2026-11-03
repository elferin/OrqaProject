using System;
using System.Collections.Generic;
using Drone.Core.Authorization;
using Drone.Core.Flight;

namespace Drone.Core.Session {
    
    public sealed class DroneSession : IDisposable, IDroneReadout {
        public AuthorizationGate Gate { get; }
        public FlightController Controller { get; }
        public IReadOnlyList<IAuthorizationProvider> Providers { get; }
        private readonly FlightInputRouter _router;
        private readonly List<IFlightInputSource> _inputSources = new();
        private int _activeSourceIndex;

        public AuthState AuthState => Gate.State;
        public string FailureReason => Gate.LastFailureReason;
        public TelemetrySnapshot Telemetry => Controller.Telemetry;
        public IFlightInputSource ActiveSource => _router.Source;
        public bool InputFaulted => _router.SourceFaulted;

        public DroneSession(AuthorizationGate gate, FlightInputRouter router, FlightController controller, IReadOnlyList<IAuthorizationProvider> providers) {

            Gate = gate;
            _router = router;
            Controller = controller;
            Providers = providers;
        }

        public void AddInputSource(IFlightInputSource flightInputSource) {
            
            _inputSources.Add(flightInputSource);
            
            if (_inputSources.Count == 1) {
                _router.SetSource(flightInputSource);
            }
        }

        public void SelectNextInputSource() {

            if (_inputSources.Count == 0) {
                return;
            }

            _activeSourceIndex = (_activeSourceIndex + 1) % _inputSources.Count;
            _router.SetSource(_inputSources[_activeSourceIndex]);
        }

        public void Tick(float deltaTime) {
            
            Gate.Pump();
            Controller.Tick(deltaTime);
        }

        public void Dispose() {
            
            _router.SetSource(null);
            Gate.Dispose();
        }
    }
}