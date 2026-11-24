using Drone.Contracts;
using UnityEngine;

namespace Drone.Frontend {

    public sealed class DroneFrontend : MonoBehaviour, IDroneFrontend {

        private IDroneOperator _session;
        private KeyboardInputSource _keyboard;
        private OperatorControls _controls;
        private DroneView _view;

        public void Attach(IDroneOperator session) {
            _session = session;
            _keyboard = new KeyboardInputSource();
            _session.AddInputSource(_keyboard);
            _controls = new OperatorControls(_session);
            _view = new DroneView();
        }

        private void Update() {
            if (_session == null) {
                return;
            }
            
            _controls.Tick();
            _view.Render();
        }

        private void FixedUpdate() {
            if (_session == null) {
                return;
            }
            
            _view.Capture(_session.Telemetry);
        }

        public void Detach() {
            if (_session == null) {
                return;
            }

            _session.RemoveInputSource(_keyboard);
            _view?.Dispose();
            _session = null;
            _keyboard = null;
            _controls = null;
            _view = null;
        }

        private void OnDestroy() => Detach();
    }
}
