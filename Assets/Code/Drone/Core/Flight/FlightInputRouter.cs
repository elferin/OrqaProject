using System;

namespace Drone.Core.Flight {

    public sealed class FlightInputRouter : IFlightInputSource {

        private IFlightInputSource _inputSource;

        public IFlightInputSource Source => _inputSource;
        public bool SourceFaulted { get; private set; }
        public event Action Changed;

        public FlightInput Sample() {

            if (_inputSource is null) {
                return default;
            }

            FlightInput input;
            bool faulted;
            try {
                input = _inputSource.Sample();
                faulted = !IsFinite(input);
            }
            catch (Exception) {
                input = default;
                faulted = true;
            }

            if (SourceFaulted != faulted) {
                SourceFaulted = faulted;
                Changed?.Invoke();
            }

            return faulted ? default : input;
        }

        public void SetSource(IFlightInputSource source) {

            if (_inputSource == source && !SourceFaulted) {
                return;
            }

            _inputSource = source;
            SourceFaulted = false;
            Changed?.Invoke();
        }

        private static bool IsFinite(FlightInput input) {

            return float.IsFinite(input.MoveDirection.X)
                && float.IsFinite(input.MoveDirection.Y)
                && float.IsFinite(input.MoveDirection.Z)
                && float.IsFinite(input.Yaw);
        }
    }
}
