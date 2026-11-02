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
        public bool ThrowOnDetach { get; set; }

        public void Attach(DroneSession session) {
            Session = session;
            AttachCount++;
        }

        public void FixedTick() => FixedTicks++;

        public void FrameTick(float deltaTime) => FrameTicks++;

        public void Detach() {
            DetachCount++;
            SessionWasLiveAtDetach = Session.ActiveSource != null;
            if (ThrowOnDetach) {
                throw new System.InvalidOperationException("Detach failed");
            }
        }
    }
}
