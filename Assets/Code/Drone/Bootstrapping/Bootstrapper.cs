using System.Collections.Generic;
using Drone.Core.Authorization;
using Drone.Core.Session;
using Drone.Providers;
using UnityEngine;

namespace Drone.App {
    
    public sealed class Bootstrapper : MonoBehaviour {
        
        [SerializeField] private bool autoStart;
        [SerializeField] private float autopilotAltitude = 3f;
    
        [SerializeField] private AuthorizationProviderFactory[] authorizationProviderFactories = new AuthorizationProviderFactory[0];
        
        private DroneSession _session;
        private IDroneFrontend[] _droneFrontends;
        
        private bool _autoArmPending;
        
        private void Awake() {
            
            IReadOnlyList<IAuthorizationProvider> providers = CreateProviders();
            _session = new DroneSession(providers);
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