using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using TMPro;
using UnityEngine;

namespace Drone.Dashboard {

    public sealed class TelemetryDashboard : MonoBehaviour, IDroneFrontend {

        private const float REFRESH_INTERVAL = 0.1f;
        private const int TENTHS = 10;
        private const string SOURCE_SUFFIX = "InputSource";

        [SerializeField] private TMP_Text authText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text flightText;
        [SerializeField] private TMP_Text inputText;
        [SerializeField] private TMP_Text altitudeText;
        [SerializeField] private TMP_Text speedText;

        private DroneSession _session;
        
        private float _timeUntilRefresh;
        private AuthState? _shownAuthState;
        private string _shownReason;
        private FlightState? _shownFlightState;
        private IFlightInputSource _shownSource;
        private int? _shownAltitudeTenths;
        private int? _shownSpeedTenths;
        

        public void Attach(DroneSession session) => _session = session;
        
        public void FixedTick() { }
        
        public void FrameTick(float deltaTime) {
            var source = _session.Router.Source;
            if (_shownSource != source) {
                _shownSource = source;
                inputText.text = source == null
                    ? "Input: None"
                    : "Input: " + source.GetType().Name.Replace(SOURCE_SUFFIX, string.Empty);
            }

            Tick(deltaTime, _session.Gate.State, _session.Gate.LastFailureReason, _session.Controller.Telemetry);
        }
        
        public void Detach() => _session = null;

        public void Tick(float deltaTime, AuthState authState, string failureReason, TelemetrySnapshot telemetry) {
            _timeUntilRefresh -= deltaTime;
            if (_timeUntilRefresh > 0f) {
                return;
            }
            _timeUntilRefresh = REFRESH_INTERVAL;

            if (_shownAuthState != authState) {
                _shownAuthState = authState;
                authText.text = AuthLabel(authState);
            }

            if (_shownReason != failureReason) {
                _shownReason = failureReason;
                reasonText.text = failureReason;
            }

            if (_shownFlightState != telemetry.FlightState) {
                _shownFlightState = telemetry.FlightState;
                flightText.text = FlightLabel(telemetry.FlightState);
            }

            var altitudeTenths = Mathf.RoundToInt(telemetry.Altitude * TENTHS);
            if (_shownAltitudeTenths != altitudeTenths) {
                _shownAltitudeTenths = altitudeTenths;
                altitudeText.SetText("Altitude: {0:1} m", (float)altitudeTenths / TENTHS);
            }

            var speedTenths = Mathf.RoundToInt(telemetry.Speed * TENTHS);
            if (_shownSpeedTenths != speedTenths) {
                _shownSpeedTenths = speedTenths;
                speedText.SetText("Speed: {0:1} m/s", (float)speedTenths / TENTHS);
            }
        }

        private static string AuthLabel(AuthState state) {
            switch (state) {
                case AuthState.Unauthorized: return "Auth: Unauthorized";
                case AuthState.Authorizing: return "Auth: Authorizing";
                case AuthState.Authorized: return "Auth: Authorized";
                case AuthState.Denied: return "Auth: Denied";
                case AuthState.Failed: return "Auth: Failed";
                default: return string.Empty;
            }
        }

        private static string FlightLabel(FlightState state) {
            switch (state) {
                case FlightState.Grounded: return "Flight: Grounded";
                case FlightState.Armed: return "Flight: Armed";
                case FlightState.Flying: return "Flight: Flying";
                case FlightState.Landing: return "Flight: Landing";
                default: return string.Empty;
            }
        }
    }
}
