using System;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using Drone.Providers;
using UnityEngine;

namespace Drone.App {

    [DefaultExecutionOrder(-100)]
    public sealed class Bootstrapper : MonoBehaviour {

        [SerializeField] private bool autoStart;
        [SerializeField] private float autopilotAltitude = 3f;
        [SerializeField] private FlightProfileAsset flightProfile;

        [SerializeField] private AuthorizationProviderFactory[] authorizationProviderFactories = Array.Empty<AuthorizationProviderFactory>();

        private DroneClients _droneClients;
        private DroneSession _session;

        private void Awake() {

            var providers = AuthorizationProvidersHelper.CreateProviders(authorizationProviderFactories, this);
            var gate = new AuthorizationGate();
            var router = new FlightInputRouter();
            var controller = new FlightController(gate, router, FlightProfileAsset.ValidateOrDefault(flightProfile, this));
            _session = new DroneSession(gate, router, controller, providers);
            _droneClients = new DroneClients();
            var autopilot = new AutopilotInputSource(() => _session.Controller.Telemetry.Altitude, autopilotAltitude);
            _session.AddInputSource(autopilot);
            
            _droneClients.AttachAll(gameObject, _session);

            if (autoStart && providers.Count > 0) {
                _session.AuthorizeAndArm(providers[0]);
            }
        }

        private void FixedUpdate() {
            _session.Tick(Time.fixedDeltaTime);
        }

        private void OnDestroy() {
            _droneClients.DetachAll();
        }
    }
}
