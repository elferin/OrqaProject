using System.Numerics;

namespace Drone.Core.Flight {
    
    public readonly struct TelemetrySnapshot {
        
        public readonly FlightState FlightState;
        public readonly float Altitude;
        public readonly float Speed;
        public readonly Vector3 Position;
        public readonly float Heading;

        public TelemetrySnapshot(FlightState flightState, float altitude, float speed, Vector3 position, float heading) {
            FlightState = flightState;
            Altitude = altitude;
            Speed = speed;
            Position = position;
            Heading = heading;
        }
    }
}