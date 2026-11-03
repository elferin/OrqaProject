using System.Numerics;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using NUnit.Framework;

namespace Drone.Tests {

    public class DroneSessionTests {

        private static DroneSession CreateSession(params IAuthorizationProvider[] providers) {
            var gate = new AuthorizationGate();
            var router = new FlightInputRouter();
            var controller = new FlightController(gate, router, FlightProfile.Default);
            return new DroneSession(gate, router, controller, providers);
        }

        [Test]
        public void Events_ForwardGateControllerAndRouterChanges() {
            using var session = CreateSession();
            var provider = new FakeAuthorizationProvider();
            var auth = 0;
            var flight = 0;
            var input = 0;
            session.AuthStateChanged += () => auth++;
            session.FlightStateChanged += () => flight++;
            session.InputChanged += () => input++;

            session.Gate.Request(provider);
            provider.Complete(AuthResult.Success());
            session.Tick(0.02f);
            session.Controller.Arm();
            session.AddInputSource(new FakeInputSource());

            Assert.AreEqual(2, auth);
            Assert.AreEqual(1, flight);
            Assert.AreEqual(1, input);
        }

        [Test]
        public void Events_AfterUnsubscribing_StopFiring() {
            using var session = CreateSession();
            var auth = 0;
            void Handler() => auth++;
            session.AuthStateChanged += Handler;

            session.AuthStateChanged -= Handler;
            session.Gate.Request(new FakeAuthorizationProvider());

            Assert.AreEqual(0, auth);
        }

        [Test]
        public void AddInputSource_First_BecomesActive() {
            using var session = CreateSession();
            var source = new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.5f) };

            session.AddInputSource(source);

            Assert.AreEqual(0.5f, session.ActiveSource.Sample().Yaw, 0.001f);
        }

        [Test]
        public void AddInputSource_Second_KeepsTheFirstActive() {
            using var session = CreateSession();
            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.5f) });

            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.9f) });

            Assert.AreEqual(0.5f, session.ActiveSource.Sample().Yaw, 0.001f);
        }

        [Test]
        public void SelectNextInputSource_TwoSources_CyclesAndWraps() {
            using var session = CreateSession();
            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.5f) });
            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.9f) });

            session.SelectNextInputSource();
            Assert.AreEqual(0.9f, session.ActiveSource.Sample().Yaw, 0.001f);

            session.SelectNextInputSource();
            Assert.AreEqual(0.5f, session.ActiveSource.Sample().Yaw, 0.001f);
        }

        [Test]
        public void SelectNextInputSource_NoSources_DoesNothing() {
            using var session = CreateSession();

            Assert.DoesNotThrow(() => session.SelectNextInputSource());
            Assert.IsNull(session.ActiveSource);
        }

        [Test]
        public void Tick_CompletedAttempt_IsApplied() {
            using var session = CreateSession();
            var provider = new FakeAuthorizationProvider();
            session.Gate.Request(provider);
            provider.Complete(AuthResult.Success());

            session.Tick(0.02f);

            Assert.IsTrue(session.Gate.IsCleared);
        }

        [Test]
        public void Tick_ArmedWithClimbInput_Flies() {
            using var session = CreateSession();
            var provider = new FakeAuthorizationProvider();
            session.Gate.Request(provider);
            provider.Complete(AuthResult.Success());
            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) });
            session.Tick(0.02f);
            session.Controller.Arm();

            session.Tick(0.02f);

            Assert.AreEqual(FlightState.Flying, session.Controller.Telemetry.FlightState);
        }

        [Test]
        public void Dispose_PendingAttempt_IsCancelled() {
            var session = CreateSession();
            var provider = new FakeAuthorizationProvider();
            session.Gate.Request(provider);

            session.Dispose();

            Assert.IsTrue(provider.WasCancelled);
        }

        [Test]
        public void Dispose_ClearsTheActiveSource() {
            var session = CreateSession();
            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.5f) });

            session.Dispose();

            Assert.IsNull(session.ActiveSource);
        }
    }
}
