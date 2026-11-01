using Drone.Core.Authorization;
using Drone.Core.Flight;
using UnityEngine.InputSystem;

namespace Drone.Frontend {

    public sealed class OperatorControls {

        private const int MAX_PROVIDER_KEYS = 9;

        private readonly AuthorizationGate _gate;
        private readonly FlightController _controller;
        private readonly FlightInputRouter _router;
        private readonly IAuthorizationProvider[] _providers;
        private readonly IFlightInputSource[] _sources;

        private int _sourceIndex;

        public OperatorControls(
            AuthorizationGate gate,
            FlightController controller,
            FlightInputRouter router,
            IAuthorizationProvider[] providers,
            IFlightInputSource[] sources) {
            _gate = gate;
            _controller = controller;
            _router = router;
            _providers = providers;
            _sources = sources;

            _router.SetSource(_sources[_sourceIndex]);
        }

        // wasPressedThisFrame is only reliable once per rendered frame, so call this from Update, not FixedUpdate.
        public void Tick() {
            var keyboard = Keyboard.current;
            if (keyboard == null) {
                return;
            }

            for (var i = 0; i < _providers.Length && i < MAX_PROVIDER_KEYS; i++) {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame) {
                    _gate.Request(_providers[i]);
                }
            }

            if (keyboard.rKey.wasPressedThisFrame) {
                _gate.Revoke();
            }

            if (keyboard.fKey.wasPressedThisFrame) {
                _controller.Arm();
            }

            if (keyboard.gKey.wasPressedThisFrame) {
                _controller.Disarm();
            }

            if (keyboard.tabKey.wasPressedThisFrame) {
                _sourceIndex = (_sourceIndex + 1) % _sources.Length;
                _router.SetSource(_sources[_sourceIndex]);
            }
        }
    }
}
