using System.Numerics;

namespace Drone.Core.Flight {
    
    public readonly struct FlightInput {
        
        public readonly Vector3 MoveDirection;
        public readonly float Yaw;

        public FlightInput(Vector3 moveDirection, float yaw) {
            MoveDirection = moveDirection;
            Yaw = yaw;
        }
    }
}