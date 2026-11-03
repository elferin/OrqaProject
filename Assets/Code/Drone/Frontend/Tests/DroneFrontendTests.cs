using System.Collections;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Drone.Frontend.Tests {

    public class DroneFrontendTests {

        private DroneSession _session;
        private DroneFrontend _frontend;

        private static DroneSession CreateSession(params IAuthorizationProvider[] providers) {
            var gate = new AuthorizationGate();
            var router = new FlightInputRouter();
            var controller = new FlightController(gate, router, FlightProfile.Default);
            return new DroneSession(gate, router, controller, providers);
        }

        [SetUp]
        public void SetUp() {
            _session = CreateSession();
            _frontend = new GameObject("Frontend").AddComponent<DroneFrontend>();
            _frontend.Attach(_session);
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            _frontend.Detach();
            Object.Destroy(_frontend.gameObject);
            _session.Dispose();
            yield return null;
        }

        [Test]
        public void Attach_AddsTheKeyboardAsASource() {
            Assert.IsInstanceOf<KeyboardInputSource>(_session.ActiveSource);
        }

        [Test]
        public void Attach_CreatesTheDroneBody() {
            Assert.IsNotNull(GameObject.Find("Drone"));
        }

        [UnityTest]
        public IEnumerator Detach_DestroysTheDroneBody() {
            _frontend.Detach();

            yield return null;

            Assert.IsNull(GameObject.Find("Drone"));
        }
    }
}
