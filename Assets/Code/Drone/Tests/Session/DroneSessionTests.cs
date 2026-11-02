using System.Numerics;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using NUnit.Framework;

namespace Drone.Tests {

    public class DroneSessionTests {

        private static DroneSession CreateSession() {
            return new DroneSession(new IAuthorizationProvider[0]);
        }

        [Test]
        public void AddInputSource_First_BecomesActive() {
            using var session = CreateSession();
            var source = new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.5f) };

            session.AddInputSource(source);

            Assert.AreEqual(0.5f, session.Router.Sample().Yaw, 0.001f);
        }

        [Test]
        public void AddInputSource_Second_KeepsTheFirstActive() {
            using var session = CreateSession();
            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.5f) });

            session.AddInputSource(new FakeInputSource { Input = new FlightInput(Vector3.Zero, 0.9f) });

            Assert.AreEqual(2, session.InputSources.Count);
            Assert.AreEqual(0.5f, session.Router.Sample().Yaw, 0.001f);
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

            Assert.AreEqual(0f, session.Router.Sample().Yaw, 0.001f);
        }
    }
}
