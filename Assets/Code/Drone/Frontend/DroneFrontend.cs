using Drone.Core.Session;
using UnityEngine;

namespace Drone.Frontend {

    public sealed class DroneFrontend : MonoBehaviour, IDroneFrontend {

        private DroneSession _session;
        private OperatorControls _controls;
        private DroneView _view;

        public void Attach(DroneSession session) {
            _session = session;
            _session.AddInputSource(new KeyboardInputSource());
            _controls = new OperatorControls(_session);
            _view = new DroneView();
        }

        public void FixedTick() {
            _view.Capture(_session.Controller.Telemetry);
        }

        public void FrameTick(float deltaTime) {
            _controls.Tick();
            _view.Render();
        }

        public void Detach() {
            _view.Dispose();
            _session = null;
        }
    }
}
