using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

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

        private TMP_Text Text(string lineName) {
            return _dashboard.transform.Find(lineName).GetComponent<TMP_Text>();
        }

        private string Line(string lineName) => Text(lineName).text;

        private sealed class AlphaInputSource : IFlightInputSource {
            public FlightInput Sample() => default;
        }

        private sealed class BetaInputSource : IFlightInputSource {
            public FlightInput Sample() => default;
        }

        private sealed class ThrowingInputSource : IFlightInputSource {
            public FlightInput Sample() => throw new System.InvalidOperationException("Sensor lost");
        }

        private sealed class InstantProvider : IAuthorizationProvider {
            private readonly AuthResult _result;

            public InstantProvider(AuthResult result) {
                _result = result;
            }

            public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) => Task.FromResult(_result);
        }

        [Test]
        public void FrameTick_First_ShowsAllValues() {
            using var session = CreateSession();
            _dashboard.Attach(session);

            _dashboard.FrameTick(WITHIN_INTERVAL);

            Assert.AreEqual("Auth: Unauthorized", Line("Auth"));
            Assert.AreEqual("Flight: Grounded", Line("Flight"));
            Assert.AreEqual("Input: None", Line("Input"));
            Assert.AreEqual("Altitude: 0.0 m", Line("Altitude"));
            Assert.AreEqual("Speed: 0.0 m/s", Line("Speed"));
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
            session.Gate.Request(new InstantProvider(AuthResult.Success()));
            session.Controller.Arm();
            _dashboard.Attach(session);

            session.Tick(WITHIN_INTERVAL);
            _dashboard.FrameTick(WITHIN_INTERVAL);

            Assert.AreEqual("Input: Throwing (fault)", Line("Input"));
        }

        [Test]
        public void FrameTick_AuthChanges_ShowsItWithoutWaitingForTheInterval() {
            using var session = CreateSession();
            _dashboard.Attach(session);
            _dashboard.FrameTick(PAST_INTERVAL);

            session.Gate.Request(new InstantProvider(AuthResult.Denied("No licence")));
            _dashboard.FrameTick(WITHIN_INTERVAL);

            Assert.AreEqual("Auth: Denied", Line("Auth"));
            Assert.AreEqual("No licence", Line("Reason"));
        }

        [Test]
        public void FrameTick_AfterRevoke_ClearsTheReason() {
            using var session = CreateSession();
            _dashboard.Attach(session);
            session.Gate.Request(new InstantProvider(AuthResult.Denied("No licence")));
            _dashboard.FrameTick(WITHIN_INTERVAL);

            session.Gate.Revoke();
            _dashboard.FrameTick(WITHIN_INTERVAL);

            Assert.AreEqual("Auth: Unauthorized", Line("Auth"));
            Assert.IsTrue(string.IsNullOrEmpty(Line("Reason")));
        }

        [Test]
        public void FrameTick_FlightStateChanges_ShowsItWithoutWaitingForTheInterval() {
            using var session = CreateSession();
            session.Gate.Request(new InstantProvider(AuthResult.Success()));
            _dashboard.Attach(session);
            _dashboard.FrameTick(PAST_INTERVAL);

            session.Controller.Arm();
            _dashboard.FrameTick(WITHIN_INTERVAL);

            Assert.AreEqual("Flight: Armed", Line("Flight"));
        }

        [Test]
        public void FrameTick_NoStateChange_DoesNotRewriteTheStatusLines() {
            using var session = CreateSession();
            _dashboard.Attach(session);
            _dashboard.FrameTick(WITHIN_INTERVAL);
            Text("Auth").text = "Untouched";

            _dashboard.FrameTick(PAST_INTERVAL);

            Assert.AreEqual("Untouched", Line("Auth"));
        }

        [Test]
        public void FrameTick_NoStateChange_AllocatesNothing() {
            using var session = CreateSession();
            session.AddInputSource(new AlphaInputSource());
            _dashboard.Attach(session);
            _dashboard.FrameTick(PAST_INTERVAL);

            Assert.That(() => _dashboard.FrameTick(PAST_INTERVAL), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void TickNumbers_First_ShowsTheValues() {
            _dashboard.TickNumbers(WITHIN_INTERVAL, 12.34f, 5f);

            Assert.AreEqual("Altitude: 12.3 m", Line("Altitude"));
            Assert.AreEqual("Speed: 5.0 m/s", Line("Speed"));
        }

        [Test]
        public void TickNumbers_WithinInterval_KeepsTheShownValues() {
            _dashboard.TickNumbers(PAST_INTERVAL, 1f, 0f);

            _dashboard.TickNumbers(WITHIN_INTERVAL, 2f, 3f);

            Assert.AreEqual("Altitude: 1.0 m", Line("Altitude"));
        }

        [Test]
        public void TickNumbers_PastInterval_ShowsTheNewValues() {
            _dashboard.TickNumbers(PAST_INTERVAL, 1f, 0f);

            _dashboard.TickNumbers(PAST_INTERVAL, 2f, 3f);

            Assert.AreEqual("Altitude: 2.0 m", Line("Altitude"));
        }

        [Test]
        public void TickNumbers_ValuesUnchanged_AllocatesNothing() {
            _dashboard.TickNumbers(PAST_INTERVAL, 3f, 1f);

            Assert.That(() => _dashboard.TickNumbers(PAST_INTERVAL, 3f, 1f), Is.Not.AllocatingGCMemory());
        }

        [Test]
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
        public void TickNumbers_NumbersChanging_AllocatesNothing() {
            var altitude = 0f;
            _dashboard.TickNumbers(PAST_INTERVAL, altitude, 1f);
            altitude += 1f;
            _dashboard.TickNumbers(PAST_INTERVAL, altitude, 1f);

            Assert.That(() => {
                altitude += 1f;
                _dashboard.TickNumbers(PAST_INTERVAL, altitude, 1f);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
