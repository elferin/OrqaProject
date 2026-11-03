using Drone.Core.Session;
using UnityEngine;

namespace Drone.App.Tests {

    public sealed class FakeFrontend : MonoBehaviour, IDroneFrontend {

        public DroneSession Session { get; private set; }
        public int AttachCount { get; private set; }
        public int FixedTicks { get; private set; }
        public int FrameTicks { get; private set; }
        public int DetachCount { get; private set; }
        public bool SessionWasLiveAtDetach { get; private set; }
        public bool ThrowOnAttach { get; set; }
        public bool ThrowOnFixedTick { get; set; }
        public bool ThrowOnFrameTick { get; set; }
        public bool ThrowOnDetach { get; set; }

        public void Attach(DroneSession session) {
            AttachCount++;
            if (ThrowOnAttach) {
                throw new System.InvalidOperationException("Attach failed");
            }
            Session = session;
        }

        public void FixedTick() {
            FixedTicks++;
            if (ThrowOnFixedTick) {
                throw new System.InvalidOperationException("FixedTick failed");
            }
        }

        public void FrameTick(float deltaTime) {
            FrameTicks++;
            if (ThrowOnFrameTick) {
                throw new System.InvalidOperationException("FrameTick failed");
            }
        }

        public void Detach() {
            DetachCount++;
            SessionWasLiveAtDetach = Session != null && Session.ActiveSource != null;
            if (ThrowOnDetach) {
                throw new System.InvalidOperationException("Detach failed");
            }
        }
    }
}
