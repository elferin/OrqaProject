using Drone.Core.Authorization;
using Drone.Core.Flight;
using TMPro;
using UnityEngine;

namespace Drone.Dashboard {

    public sealed class TelemetryDashboard : MonoBehaviour {

        private const float REFRESH_INTERVAL = 0.1f;
        private const int TENTHS = 10;

        [SerializeField] private TMP_Text authText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text flightText;
        [SerializeField] private TMP_Text altitudeText;
        [SerializeField] private TMP_Text speedText;

        private float _timeUntilRefresh;
        private AuthState? _shownAuthState;
        private string _shownReason;
        private FlightState? _shownFlightState;
        private int? _shownAltitudeTenths;
        private int? _shownSpeedTenths;

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

        // A switch rather than ToString, which allocates a new string on each call.
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
