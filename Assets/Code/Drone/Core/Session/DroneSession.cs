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
        private bool _armWhenCleared;

        public AuthState AuthState => Gate.State;
        public string FailureReason => Gate.LastFailureReason;
        public TelemetrySnapshot Telemetry => Controller.Telemetry;
        public IFlightInputSource ActiveSource => _router.Source;
        public bool InputFaulted => _router.SourceFaulted;

        public event Action AuthStateChanged {
            add => Gate.StateChanged += value;
            remove => Gate.StateChanged -= value;
        }

        public event Action FlightStateChanged {
            add => Controller.StateChanged += value;
            remove => Controller.StateChanged -= value;
        }

        public event Action InputChanged {
            add => _router.Changed += value;
            remove => _router.Changed -= value;
        }

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

        public void RemoveInputSource(IFlightInputSource flightInputSource) {

            var index = _inputSources.IndexOf(flightInputSource);
            if (index < 0) {
                return;
            }

            _inputSources.RemoveAt(index);

            if (_router.Source != flightInputSource) {
                if (index < _activeSourceIndex) {
                    _activeSourceIndex--;
                }
                return;
            }

            if (_inputSources.Count == 0) {
                _activeSourceIndex = 0;
                _router.SetSource(null);
                return;
            }

            _activeSourceIndex = index % _inputSources.Count;
            _router.SetSource(_inputSources[_activeSourceIndex]);
        }

        public bool SelectInputSource(IFlightInputSource flightInputSource) {

            var index = _inputSources.IndexOf(flightInputSource);
            if (index < 0) {
                return false;
            }

            _activeSourceIndex = index;
            _router.SetSource(flightInputSource);
            return true;
        }

        public void SelectNextInputSource() {

            if (_inputSources.Count == 0) {
                return;
            }

            _activeSourceIndex = (_activeSourceIndex + 1) % _inputSources.Count;
            _router.SetSource(_inputSources[_activeSourceIndex]);
        }

        public void AuthorizeAndArm(IAuthorizationProvider provider) {

            _armWhenCleared = true;
            Gate.Request(provider);
        }

        public void Tick(float deltaTime) {

            Gate.Pump();
            ArmIfCleared();
            Controller.Tick(deltaTime);
        }

        private void ArmIfCleared() {

            if (!_armWhenCleared) {
                return;
            }

            if (Gate.IsCleared) {
                _armWhenCleared = false;
                Controller.Arm();
                return;
            }

            if (Gate.State != AuthState.Authorizing) {
                _armWhenCleared = false;
            }
        }

        public void Dispose() {
            
            _router.SetSource(null);
            Gate.Dispose();
        }
    }
}