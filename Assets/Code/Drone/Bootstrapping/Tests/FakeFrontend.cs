using Drone.Contracts;
using UnityEngine;

namespace Drone.App.Tests {

    public sealed class FakeFrontend : MonoBehaviour, IDroneFrontend {

        public IDroneOperator Session { get; private set; }
        public int AttachCount { get; private set; }
        public int DetachCount { get; private set; }
        public bool SessionWasLiveAtDetach { get; private set; }
        public bool ThrowOnAttach { get; set; }
        public bool ThrowOnDetach { get; set; }

        public void Attach(IDroneOperator session) {
            AttachCount++;
            if (ThrowOnAttach) {
                throw new System.InvalidOperationException("Attach failed");
            }
            Session = session;
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
