using System;
using Drone.Core.Flight;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drone.Frontend {

    public sealed class DroneView : IDisposable {

        private readonly GameObject _body;
        private readonly Transform _transform;

        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private Quaternion _previousRotation = Quaternion.identity;
        private Quaternion _currentRotation = Quaternion.identity;

        public DroneView() {
            _body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _body.name = "Drone";
            Object.Destroy(_body.GetComponent<Collider>());

            _transform = _body.transform;
            _transform.localScale = new Vector3(1f, 0.2f, 1f);
        }

        public void Capture(TelemetrySnapshot telemetry) {
            _previousPosition = _currentPosition;
            _previousRotation = _currentRotation;
            _currentPosition = new Vector3(telemetry.Position.X, telemetry.Position.Y, telemetry.Position.Z);
            _currentRotation = Quaternion.Euler(0f, telemetry.Heading * Mathf.Rad2Deg, 0f);
        }

        public void Render() {
            var blend = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            _transform.SetPositionAndRotation(
                Vector3.Lerp(_previousPosition, _currentPosition, blend),
                Quaternion.Slerp(_previousRotation, _currentRotation, blend));
        }

        public void Dispose() {
            Object.Destroy(_body);
        }
    }
}
