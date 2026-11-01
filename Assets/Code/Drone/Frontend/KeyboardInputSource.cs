using System.Numerics;
using Drone.Core.Flight;
using UnityEngine.InputSystem;

namespace Drone.Frontend {

    public sealed class KeyboardInputSource : IFlightInputSource {

        public FlightInput Sample() {
            var keyboard = Keyboard.current;
            if (keyboard == null) {
                return default;
            }

            var move = new Vector3(
                Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed),
                Axis(keyboard.spaceKey.isPressed, keyboard.leftShiftKey.isPressed),
                Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed));
            var yaw = Axis(keyboard.eKey.isPressed, keyboard.qKey.isPressed);

            return new FlightInput(move, yaw);
        }

        private static float Axis(bool positive, bool negative) {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
