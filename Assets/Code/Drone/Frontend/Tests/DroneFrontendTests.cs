using System.Collections;
using Drone.Core.Authorization;
using Drone.Core.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Drone.Frontend.Tests {

    public class DroneFrontendTests {

        private DroneSession _session;
        private DroneFrontend _frontend;

        [SetUp]
        public void SetUp() {
            _session = new DroneSession(new IAuthorizationProvider[0]);
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
