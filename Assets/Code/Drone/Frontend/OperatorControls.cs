using Drone.Contracts;
using UnityEngine.InputSystem;

namespace Drone.Frontend {

    public sealed class OperatorControls {

        private const int MAX_PROVIDER_KEYS = 9;

        private readonly IDroneOperator _session;

        public OperatorControls(IDroneOperator session) {
            _session = session;
        }

        public void Tick() {
            var keyboard = Keyboard.current;
            if (keyboard == null) {
                return;
            }

            var providerCount = _session.ProviderCount;
            for (var i = 0; i < providerCount && i < MAX_PROVIDER_KEYS; i++) {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame) {
                    _session.RequestAuthorization(i);
                }
            }

            if (keyboard.rKey.wasPressedThisFrame) {
                _session.Revoke();
            }

            if (keyboard.fKey.wasPressedThisFrame) {
                _session.Arm();
            }

            if (keyboard.gKey.wasPressedThisFrame) {
                _session.Disarm();
            }

            if (keyboard.tabKey.wasPressedThisFrame) {
                _session.SelectNextInputSource();
            }
        }
    }
}
