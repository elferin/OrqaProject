using System.Collections;
using Drone.Core.Flight;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Drone.Frontend.Tests {

    // Play Mode only, because DroneView uses Object.Destroy, which Unity refuses in Edit Mode.
    public class DroneViewTests {

        private DroneView _view;

        [SetUp]
        public void SetUp() {
            _view = new DroneView();
        }

        // Destroy only takes effect at the end of the frame, so wait or the next test finds this cube.
        [UnityTearDown]
        public IEnumerator TearDown() {
            _view.Dispose();
            yield return null;
        }

        private static TelemetrySnapshot SnapshotAt(float x, float heading = 0f) {
            return new TelemetrySnapshot(FlightState.Flying, 0f, 0f, new System.Numerics.Vector3(x, 0f, 0f), heading);
        }

        [UnityTest]
        public IEnumerator Constructor_RemovesTheCollider() {
            yield return null;

            Assert.IsNull(GameObject.Find("Drone").GetComponent<Collider>());
        }

        [Test]
        public void Render_AfterTwoEqualCaptures_DrawsThatPose() {
            _view.Capture(SnapshotAt(4f, Mathf.PI / 2f));
            _view.Capture(SnapshotAt(4f, Mathf.PI / 2f));

            _view.Render();

            var transform = GameObject.Find("Drone").transform;
            Assert.AreEqual(4f, transform.position.x, 0.001f);
            Assert.AreEqual(90f, transform.eulerAngles.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator Render_AtTickTime_DrawsThePreviousCapture() {
            _view.Capture(SnapshotAt(2f));
            _view.Capture(SnapshotAt(10f));

            yield return new WaitForFixedUpdate();
            _view.Render();

            Assert.AreEqual(2f, GameObject.Find("Drone").transform.position.x, 0.001f);
        }

        [UnityTest]
        public IEnumerator Render_BetweenTicks_StaysBetweenTheLastTwoCaptures() {
            _view.Capture(SnapshotAt(2f));
            _view.Capture(SnapshotAt(10f));

            yield return null;
            _view.Render();

            var x = GameObject.Find("Drone").transform.position.x;
            Assert.GreaterOrEqual(x, 2f);
            Assert.LessOrEqual(x, 10f);
        }

        [UnityTest]
        public IEnumerator Dispose_DestroysTheBody() {
            _view.Dispose();

            yield return null;

            Assert.IsNull(GameObject.Find("Drone"));
        }
    }
}
