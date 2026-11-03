using System;
using System.Collections.Generic;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using Drone.Providers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drone.App {
    
    public sealed class Bootstrapper : MonoBehaviour {
        
        [SerializeField] private bool autoStart;
        [SerializeField] private float autopilotAltitude = 3f;
        [SerializeField] private FlightProfileAsset flightProfile;
    
        [SerializeField] private AuthorizationProviderFactory[] authorizationProviderFactories = Array.Empty<AuthorizationProviderFactory>();
        
        private DroneSession _session;
        private IDroneFrontend[] _droneFrontends;
        
        private bool _autoArmPending;
        
        private void Awake() {
            
            var providers = CreateProviders();
            var gate = new AuthorizationGate();
            var router = new FlightInputRouter();
            var controller = new FlightController(gate, router, ValidatedProfile());
            _session = new DroneSession(gate, router, controller, providers);
            var autopilot = new AutopilotInputSource(() => _session.Controller.Telemetry.Altitude, autopilotAltitude);
            _session.AddInputSource(autopilot);
            _droneFrontends = GetComponentsInChildren<IDroneFrontend>();
            foreach (var frontend in _droneFrontends) {
                frontend.Attach(_session);
            }

            if (autoStart && providers.Count > 0) {
                _session.Gate.Request(providers[0]);
                _autoArmPending = true;
            }
        }

        private void Update() {
            
            foreach (var frontend in _droneFrontends) {
                if (IsAlive(frontend)) {
                    frontend.FrameTick(Time.unscaledDeltaTime);
                }
            }
        }

        private void FixedUpdate() {
            
            _session.Tick(Time.fixedDeltaTime);
            
            if (_autoArmPending && _session.Gate.IsCleared) {
                _autoArmPending = false;
                _session.Controller.Arm();
            }
            
            foreach (var frontend in _droneFrontends) {
                if (IsAlive(frontend)) {
                    frontend.FixedTick();
                }
            }
            
        }

        private void OnDestroy() {
            
            try {
                foreach (var frontend in _droneFrontends) {
                    frontend.Detach();
                }
            }
            finally {
                _session.Dispose();
            }
        }

        private static bool IsAlive(IDroneFrontend frontend) {

            return !(frontend is Object unityObject) || unityObject != null;
        }

        private FlightProfile ValidatedProfile() {

            if (flightProfile == null) {
                Debug.LogError("No flight profile assigned, using the default", this);
                return FlightProfile.Default;
            }

            if (flightProfile.Profile.IsValid) {
                return flightProfile.Profile;
            }

            Debug.LogError("Invalid flight profile, using the default", this);
            return FlightProfile.Default;
        }

        private IReadOnlyList<IAuthorizationProvider> CreateProviders() {
            
            var authorizationProviders = new List<IAuthorizationProvider>();
            foreach (var authorizationProviderFactory in authorizationProviderFactories) {
                if (authorizationProviderFactory == null) {
                    Debug.LogError("Empty authorization provider factory slot, digit keys will shift", this);
                    continue;
                }

                authorizationProviders.Add(authorizationProviderFactory.Create());
            }

            if (authorizationProviders.Count == 0) {
                Debug.LogError("No authorization providers configured, the drone can't be authorized", this);
            }

            return authorizationProviders;
        }
    }
}