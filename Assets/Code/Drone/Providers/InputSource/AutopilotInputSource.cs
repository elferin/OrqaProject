using System;
using System.Numerics;
using Drone.Core.Flight;

namespace Drone.Providers {

    public sealed class AutopilotInputSource : IFlightInputSource {

        private const float SLOWDOWN_DISTANCE = 2f;

        private readonly Func<float> _getAltitude;
        private readonly float _targetAltitude;

        public AutopilotInputSource(Func<float> getAltitude, float targetAltitude) {
            _getAltitude = getAltitude;
            _targetAltitude = targetAltitude;
        }

        public FlightInput Sample() {
            var climb = Math.Clamp((_targetAltitude - _getAltitude()) / SLOWDOWN_DISTANCE, -1f, 1f);
            return new FlightInput(new Vector3(0f, climb, 0f), 0f);
        }
    }
}
