using Drone.Core.Session;
using UnityEngine;

namespace Drone.App.Tests {

    public sealed class FakeDisplay : MonoBehaviour, IDroneDisplay {

        public IDroneReadout Readout { get; private set; }
        public int FrameTicks { get; private set; }
        public int DetachCount { get; private set; }

        public void Attach(IDroneReadout readout) => Readout = readout;

        public void FrameTick(float deltaTime) => FrameTicks++;

        public void Detach() => DetachCount++;
    }
}
