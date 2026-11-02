using System;
using System.Collections.Generic;
using Drone.Core.Authorization;
using Drone.Core.Flight;

namespace Drone.Core.Session {
    
    public class DroneSession : IDisposable {
        public AuthorizationGate Gate { get; }
        public FlightInputRouter Router { get; }
        public FlightController Controller { get; }
        public IReadOnlyList<IAuthorizationProvider> Providers { get; }
        private readonly List<IFlightInputSource> _inputSources = new();
        public IReadOnlyList<IFlightInputSource> InputSources => _inputSources;

        public DroneSession(IReadOnlyList<IAuthorizationProvider> providers) {

            Gate = new AuthorizationGate();
            Router = new FlightInputRouter();
            Controller = new FlightController(Gate, Router);

            Providers = providers;
        }

        public void AddInputSource(IFlightInputSource flightInputSource) {
            
            _inputSources.Add(flightInputSource);
            
            if (_inputSources.Count == 1) {
                Router.SetSource(flightInputSource);
            }
        }

        public void Tick(float deltaTime) {
            
            Gate.Pump();
            Controller.Tick(deltaTime);
        }

        public void Dispose() {
            
            Router.SetSource(null);
            Gate.Dispose();
        }
    }
}