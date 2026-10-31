using System;
using System.Numerics;
using Drone.Core.Authorization;

namespace Drone.Core.Flight {
    
    public sealed class FlightController {
        
        private const float ACCELERATION = 10f;
        private const float MAX_SPEED = 5f;
        private const float LANDING_SPEED = 2f;
        private const float YAW_SPEED = 2f;
        
        private readonly IFlightClearance _flightClearance;
        private readonly IFlightInputSource _flightInputSource;

        private FlightState _flightState;
        private Vector3 _position;
        private Vector3 _velocity;
        private float _heading;
        public TelemetrySnapshot Telemetry => new (_flightState, _position.Y, _velocity.Length(), _position, _heading);

        public FlightController(IFlightClearance flightClearance, IFlightInputSource flightInputSource) {
            _flightClearance = flightClearance;
            _flightInputSource = flightInputSource;
        }

        public void Arm() {
            if (_flightState != FlightState.Grounded || !_flightClearance.IsCleared) {
                return;
            }
            
            _flightState = FlightState.Armed;
        }

        public void Disarm() {
            if (_flightState == FlightState.Armed) {
                _flightState = FlightState.Grounded;
                return;
            }
            
            if (_flightState == FlightState.Flying) {
                _flightState = FlightState.Landing;
            }
        }

        public void Tick(float deltaTime) {
            if (_flightState == FlightState.Grounded) {
                return;
            }

            if (!_flightClearance.IsCleared) {
                if (_flightState == FlightState.Armed) {
                    _flightState = FlightState.Grounded;
                    return;
                }

                if (_flightState == FlightState.Flying) {
                    _flightState = FlightState.Landing;
                }
            }

            if (_flightState == FlightState.Landing) {
                TickLanding(deltaTime);
                return;
            }

            var input = _flightInputSource.Sample();

            if (_flightState == FlightState.Armed) {
                if (input.MoveDirection.Y <= 0) {
                    return;
                }
                _flightState = FlightState.Flying;
            }
            
            _heading += Math.Clamp(input.Yaw, -1f, 1f) * YAW_SPEED * deltaTime;

            var moveDirection = input.MoveDirection.Length() > 1
                ? Vector3.Normalize(input.MoveDirection)
                : input.MoveDirection;
            var target = moveDirection * MAX_SPEED;
            var change = target - _velocity;
            if (change.Length() > ACCELERATION * deltaTime) {
                change = Vector3.Normalize(change) * ACCELERATION * deltaTime;
            }
            _velocity += change;
            _position += _velocity * deltaTime;
            
            if (_position.Y < 0) {
                _position.Y = 0f;
                if (_velocity.Y < 0) {
                    _velocity.Y = 0f;
                }
            }
        }

        private void TickLanding(float deltaTime) {
            _velocity = -Vector3.UnitY * LANDING_SPEED;
            _position += _velocity * deltaTime;

            if (_position.Y <= 0) {
                _velocity = Vector3.Zero;
                _position.Y = 0f;
                _flightState = FlightState.Grounded;
            }
        }
    }
}