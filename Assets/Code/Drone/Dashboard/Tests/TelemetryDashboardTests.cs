using System.Collections;
using System.Numerics;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Vector3 = System.Numerics.Vector3;

namespace Drone.Dashboard.Tests {

    public class TelemetryDashboardTests {

        private const string PREFAB_NAME = "Dashboard/TelemetryDashboard";
        private const float PAST_INTERVAL = 1f;
        private const float WITHIN_INTERVAL = 0.01f;

        private TelemetryDashboard _dashboard;

        private static DroneSession CreateSession(params IAuthorizationProvider[] providers) {
            var gate = new AuthorizationGate();
            var router = new FlightInputRouter();
            var controller = new FlightController(gate, router, FlightProfile.Default);
            return new DroneSession(gate, router, controller, providers);
        }

        [SetUp]
        public void SetUp() {
            var prefab = Resources.Load<TelemetryDashboard>(PREFAB_NAME);
            _dashboard = Object.Instantiate(prefab);
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            Object.Destroy(_dashboard.gameObject);
            yield return null;
        }

        private static TelemetrySnapshot Snapshot(FlightState state, float altitude, float speed) {
            return new TelemetrySnapshot(state, altitude, speed, new Vector3(0f, altitude, 0f), 0f);
        }

        private string Line(string lineName) {
            return _dashboard.transform.Find(lineName).GetComponent<TMP_Text>().text;
        }

        private sealed class AlphaInputSource : IFlightInputSource {
            public FlightInput Sample() => default;
        }

        private sealed class ThrowingInputSource : IFlightInputSource {
            public FlightInput Sample() => throw new System.InvalidOperationException("Sensor lost");
        }

        private sealed class InstantProvider : IAuthorizationProvider {
            public System.Threading.Tasks.Task<AuthResult> AuthorizeAsync(System.Threading.CancellationToken cancellationToken) {
                return System.Threading.Tasks.Task.FromResult(AuthResult.Success());
            }
        }

        private sealed class BetaInputSource : IFlightInputSource {
            public FlightInput Sample() => default;
        }

        [Test]
        public void FrameTick_SourceChanges_ShowsTheActiveSource() {
            using var session = CreateSession();
            session.AddInputSource(new AlphaInputSource());
            session.AddInputSource(new BetaInputSource());
            _dashboard.Attach(session);

            _dashboard.FrameTick(WITHIN_INTERVAL);
            Assert.AreEqual("Input: Alpha", Line("Input"));

            session.SelectNextInputSource();
            _dashboard.FrameTick(WITHIN_INTERVAL);
            Assert.AreEqual("Input: Beta", Line("Input"));
        }

        [Test]
        public void FrameTick_SourceFaults_ShowsTheFault() {
            using var session = CreateSession();
            session.AddInputSource(new ThrowingInputSource());
            session.Gate.Request(new InstantProvider());
            session.Controller.Arm();
            _dashboard.Attach(session);

            session.Tick(WITHIN_INTERVAL);
            _dashboard.FrameTick(WITHIN_INTERVAL);

            Assert.AreEqual("Input: Throwing (fault)", Line("Input"));
        }

        [Test]
        public void Tick_First_ShowsAllValues() {
            _dashboard.Tick(WITHIN_INTERVAL, AuthState.Authorized, null, Snapshot(FlightState.Flying, 12.34f, 5f));

            Assert.AreEqual("Auth: Authorized", Line("Auth"));
            Assert.AreEqual("Flight: Flying", Line("Flight"));
            Assert.AreEqual("Altitude: 12.3 m", Line("Altitude"));
            Assert.AreEqual("Speed: 5.0 m/s", Line("Speed"));
        }

        [Test]
        public void Tick_WithFailureReason_ShowsTheReasonThenClearsIt() {
            _dashboard.Tick(PAST_INTERVAL, AuthState.Failed, "Remote authorization timed out", default);
            Assert.AreEqual("Remote authorization timed out", Line("Reason"));

            _dashboard.Tick(PAST_INTERVAL, AuthState.Authorizing, null, default);
            Assert.IsTrue(string.IsNullOrEmpty(Line("Reason")));
        }

        [Test]
        public void Tick_WithinInterval_KeepsTheShownValues() {
            _dashboard.Tick(PAST_INTERVAL, AuthState.Unauthorized, null, Snapshot(FlightState.Grounded, 1f, 0f));

            _dashboard.Tick(WITHIN_INTERVAL, AuthState.Authorized, null, Snapshot(FlightState.Flying, 2f, 3f));

            Assert.AreEqual("Auth: Unauthorized", Line("Auth"));
            Assert.AreEqual("Altitude: 1.0 m", Line("Altitude"));
        }

        [Test]
        public void Tick_PastInterval_ShowsTheNewValues() {
            _dashboard.Tick(PAST_INTERVAL, AuthState.Unauthorized, null, Snapshot(FlightState.Grounded, 1f, 0f));

            _dashboard.Tick(PAST_INTERVAL, AuthState.Authorized, null, Snapshot(FlightState.Flying, 2f, 3f));

            Assert.AreEqual("Auth: Authorized", Line("Auth"));
            Assert.AreEqual("Altitude: 2.0 m", Line("Altitude"));
        }

        [Test]
        public void Tick_ValuesUnchanged_AllocatesNothing() {
            var snapshot = Snapshot(FlightState.Flying, 3f, 1f);
            _dashboard.Tick(PAST_INTERVAL, AuthState.Authorized, null, snapshot);

            Assert.That(() => _dashboard.Tick(PAST_INTERVAL, AuthState.Authorized, null, snapshot),
                Is.Not.AllocatingGCMemory());
        }

        [Test]
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
        public void Tick_NumbersChanging_AllocatesNothing() {
            var altitude = 0f;
            _dashboard.Tick(PAST_INTERVAL, AuthState.Authorized, null, Snapshot(FlightState.Flying, altitude, 1f));
            altitude += 1f;
            _dashboard.Tick(PAST_INTERVAL, AuthState.Authorized, null, Snapshot(FlightState.Flying, altitude, 1f));

            Assert.That(() => {
                altitude += 1f;
                _dashboard.Tick(PAST_INTERVAL, AuthState.Authorized, null, Snapshot(FlightState.Flying, altitude, 1f));
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
