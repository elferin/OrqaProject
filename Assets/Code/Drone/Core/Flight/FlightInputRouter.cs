using System;

namespace Drone.Core.Flight {

    public sealed class FlightInputRouter : IFlightInputSource {

        private IFlightInputSource _inputSource;

        public IFlightInputSource Source => _inputSource;
        public bool SourceFaulted { get; private set; }

        public FlightInput Sample() {

            if (_inputSource is null) {
                return default;
            }

            try {
                var input = _inputSource.Sample();
                SourceFaulted = !IsFinite(input);
                return SourceFaulted ? default : input;
            }
            catch (Exception) {
                SourceFaulted = true;
                return default;
            }
        }

        public void SetSource(IFlightInputSource source) {

            _inputSource = source;
            SourceFaulted = false;
        }

        private static bool IsFinite(FlightInput input) {

            return float.IsFinite(input.MoveDirection.X)
                && float.IsFinite(input.MoveDirection.Y)
                && float.IsFinite(input.MoveDirection.Z)
                && float.IsFinite(input.Yaw);
        }
    }
}
