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

        private readonly List<IDroneClient> _clients = new();
        private readonly List<IDroneFrontend> _frontends = new();
        private DroneSession _session;

        private void Awake() {

            var providers = CreateProviders();
            var gate = new AuthorizationGate();
            var router = new FlightInputRouter();
            var controller = new FlightController(gate, router, ValidatedProfile());
            _session = new DroneSession(gate, router, controller, providers);
            var autopilot = new AutopilotInputSource(() => _session.Controller.Telemetry.Altitude, autopilotAltitude);
            _session.AddInputSource(autopilot);
            
            foreach (var frontend in GetComponentsInChildren<IDroneFrontend>()) {
                try {
                    frontend.Attach(_session);
                    _clients.Add(frontend);
                    _frontends.Add(frontend);
                }
                catch (Exception exception) {
                    Debug.LogException(exception, this);
                }
            }

            foreach (var display in GetComponentsInChildren<IDroneDisplay>()) {
                try {
                    display.Attach(_session);
                    _clients.Add(display);
                }
                catch (Exception exception) {
                    Debug.LogException(exception, this);
                }
            }

            if (autoStart && providers.Count > 0) {
                _session.AuthorizeAndArm(providers[0]);
            }
        }

        private void Update() {

            for (var i = 0; i < _clients.Count; i++) {
                var client = _clients[i];
                if (!IsAlive(client)) {
                    continue;
                }

                try {
                    client.FrameTick(Time.unscaledDeltaTime);
                }
                catch (Exception exception) {
                    i--;
                    DropClient(client, exception);
                }
            }
        }

        private void FixedUpdate() {

            _session.Tick(Time.fixedDeltaTime);

            for (var i = 0; i < _frontends.Count; i++) {
                var frontend = _frontends[i];
                if (!IsAlive(frontend)) {
                    continue;
                }

                try {
                    frontend.FixedTick();
                }
                catch (Exception exception) {
                    i--;
                    DropClient(frontend, exception);
                }
            }
        }

        private void OnDestroy() {

            foreach (var client in _clients) {
                DetachSafely(client);
            }
            _clients.Clear();
            _frontends.Clear();
            _session?.Dispose();
        }

        private void DropClient(IDroneClient client, Exception exception) {

            Debug.LogException(exception, this);
            _clients.Remove(client);
            if (client is IDroneFrontend frontend) {
                _frontends.Remove(frontend);
            }
            DetachSafely(client);
        }

        private void DetachSafely(IDroneClient client) {

            try {
                client.Detach();
            }
            catch (Exception exception) {
                Debug.LogException(exception, this);
            }
        }

        private static bool IsAlive(IDroneClient client) {

            return !(client is Object unityObject) || unityObject != null;
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

                try {
                    authorizationProviders.Add(authorizationProviderFactory.Create());
                }
                catch (Exception exception) {
                    Debug.LogException(exception, this);
                }
            }

            if (authorizationProviders.Count == 0) {
                Debug.LogError("No authorization providers configured, the drone can't be authorized", this);
            }

            return authorizationProviders;
        }
    }
}
