using Drone.Core.Session;
using UnityEngine.InputSystem;

namespace Drone.Frontend {

    public sealed class OperatorControls {

        private const int MAX_PROVIDER_KEYS = 9;

        private readonly DroneSession _session;

        public OperatorControls(DroneSession session) {
            _session = session;
        }

        public void Tick() {
            var keyboard = Keyboard.current;
            if (keyboard == null) {
                return;
            }

            var providers = _session.Providers;
            for (var i = 0; i < providers.Count && i < MAX_PROVIDER_KEYS; i++) {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame) {
                    _session.Gate.Request(providers[i]);
                }
            }

            if (keyboard.rKey.wasPressedThisFrame) {
                _session.Gate.Revoke();
            }

            if (keyboard.fKey.wasPressedThisFrame) {
                _session.Controller.Arm();
            }

            if (keyboard.gKey.wasPressedThisFrame) {
                _session.Controller.Disarm();
            }

            if (keyboard.tabKey.wasPressedThisFrame) {
                _session.SelectNextInputSource();
            }
        }
    }
}
