using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using TMPro;
using UnityEngine;

namespace Drone.Dashboard {

    public sealed class TelemetryDashboard : MonoBehaviour, IDroneDisplay {

        private const float REFRESH_INTERVAL = 0.1f;
        private const int TENTHS = 10;
        private const string FAULT_SUFFIX = " (fault)";

        [SerializeField] private TMP_Text authText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text flightText;
        [SerializeField] private TMP_Text inputText;
        [SerializeField] private TMP_Text altitudeText;
        [SerializeField] private TMP_Text speedText;

        private IDroneReadout _readout;

        private bool _statusDirty;
        private float _timeUntilRefresh;
        private int? _shownAltitudeTenths;
        private int? _shownSpeedTenths;


        public void Attach(IDroneReadout readout) {
            _readout = readout;
            _readout.AuthStateChanged += MarkStatusDirty;
            _readout.FlightStateChanged += MarkStatusDirty;
            _readout.InputChanged += MarkStatusDirty;
            _statusDirty = true;
        }

        public void FrameTick(float deltaTime) {
            if (_statusDirty) {
                _statusDirty = false;
                ShowStatus();
            }

            var telemetry = _readout.Telemetry;
            TickNumbers(deltaTime, telemetry.Altitude, telemetry.Speed);
        }

        public void Detach() {
            _readout.AuthStateChanged -= MarkStatusDirty;
            _readout.FlightStateChanged -= MarkStatusDirty;
            _readout.InputChanged -= MarkStatusDirty;
            _readout = null;
        }

        public void TickNumbers(float deltaTime, float altitude, float speed) {
            _timeUntilRefresh -= deltaTime;
            if (_timeUntilRefresh > 0f) {
                return;
            }
            _timeUntilRefresh = REFRESH_INTERVAL;

            var altitudeTenths = Mathf.RoundToInt(altitude * TENTHS);
            if (_shownAltitudeTenths != altitudeTenths) {
                _shownAltitudeTenths = altitudeTenths;
                altitudeText.SetText("Altitude: {0:1} m", (float)altitudeTenths / TENTHS);
            }

            var speedTenths = Mathf.RoundToInt(speed * TENTHS);
            if (_shownSpeedTenths != speedTenths) {
                _shownSpeedTenths = speedTenths;
                speedText.SetText("Speed: {0:1} m/s", (float)speedTenths / TENTHS);
            }
        }

        private void MarkStatusDirty() => _statusDirty = true;

        private void ShowStatus() {
            authText.text = AuthLabel(_readout.AuthState);
            reasonText.text = _readout.FailureReason;
            flightText.text = FlightLabel(_readout.Telemetry.FlightState);
            inputText.text = InputLabel(_readout.ActiveSource, _readout.InputFaulted);
        }

        private static string InputLabel(IFlightInputSource source, bool faulted) {
            if (source == null) {
                return "Input: None";
            }

            return "Input: " + source.DisplayName + (faulted ? FAULT_SUFFIX : string.Empty);
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
