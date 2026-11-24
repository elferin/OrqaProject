using Drone.Contracts;
using UnityEngine;

namespace Drone.App.Tests {

    public sealed class FakeDisplay : MonoBehaviour, IDroneDisplay {

        public IDroneReadout Readout { get; private set; }
        public int DetachCount { get; private set; }

        public void Attach(IDroneReadout readout) => Readout = readout;

        public void Detach() => DetachCount++;
    }
}
