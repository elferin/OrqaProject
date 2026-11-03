using System;

namespace Drone.Core.Flight {

    [Serializable]
    public struct FlightProfile {

        public float Acceleration;
        public float MaxSpeed;
        public float LandingSpeed;
        public float YawSpeed;

        public static FlightProfile Default => new () {
            Acceleration = 10f,
            MaxSpeed = 5f,
            LandingSpeed = 2f,
            YawSpeed = 2f
        };

        public bool IsValid => IsPositive(Acceleration)
            && IsPositive(MaxSpeed)
            && IsPositive(LandingSpeed)
            && IsPositive(YawSpeed);

        private static bool IsPositive(float value) => float.IsFinite(value) && value > 0f;
    }
}
