using System;
using System.Numerics;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using Drone.Providers;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Drone.Tests {

    public class DroneSessionAllocationTests {

        private const float DELTA = 0.02f;
        private const int TICKS_TO_CLIMB = 100;

        private static DroneSession CreateSession() {
            var gate = new AuthorizationGate();
            var router = new FlightInputRouter();
            var controller = new FlightController(gate, router, FlightProfile.Default);
            return new DroneSession(gate, router, controller, Array.Empty<IAuthorizationProvider>());
        }

        private static DroneSession FlyingSession(IFlightInputSource source) {
            var session = CreateSession();
            session.AddInputSource(source);
            var provider = new FakeAuthorizationProvider();
            provider.Complete(AuthResult.Success());
            session.AuthorizeAndArm(provider);
            for (var i = 0; i < TICKS_TO_CLIMB; i++) {
                session.Tick(DELTA);
            }
            return session;
        }

        [Test]
        public void Tick_Flying_AllocatesNothing() {
            using var session = FlyingSession(new FakeInputSource { Input = new FlightInput(new Vector3(1f, 1f, 1f), 1f) });
            Assert.AreEqual(FlightState.Flying, session.Telemetry.FlightState);

            Assert.That(() => {
                session.Tick(DELTA);
                _ = session.Telemetry;
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Tick_FlyingOnAutopilot_AllocatesNothing() {
            using var session = CreateSession();
            session.AddInputSource(new AutopilotInputSource(() => session.Telemetry.Altitude, 3f));
            var provider = new FakeAuthorizationProvider();
            provider.Complete(AuthResult.Success());
            session.AuthorizeAndArm(provider);
            for (var i = 0; i < TICKS_TO_CLIMB; i++) {
                session.Tick(DELTA);
            }
            Assert.AreEqual(FlightState.Flying, session.Telemetry.FlightState);

            Assert.That(() => session.Tick(DELTA), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Tick_Landing_AllocatesNothing() {
            using var session = FlyingSession(new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) });
            session.Controller.Disarm();
            session.Tick(DELTA);
            Assert.AreEqual(FlightState.Landing, session.Telemetry.FlightState);

            Assert.That(() => session.Tick(DELTA), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Tick_RequestPending_AllocatesNothing() {
            using var session = CreateSession();
            session.AddInputSource(new FakeInputSource());
            session.AuthorizeAndArm(new FakeAuthorizationProvider());
            session.Tick(DELTA);
            Assert.AreEqual(AuthState.Authorizing, session.AuthState);

            Assert.That(() => session.Tick(DELTA), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Tick_NonFiniteInput_AllocatesNothing() {
            var source = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            using var session = FlyingSession(source);
            source.Input = new FlightInput(new Vector3(float.NaN, 0f, 0f), 0f);
            session.Tick(DELTA);
            Assert.IsTrue(session.InputFaulted);

            Assert.That(() => session.Tick(DELTA), Is.Not.AllocatingGCMemory());
        }
    }
}
