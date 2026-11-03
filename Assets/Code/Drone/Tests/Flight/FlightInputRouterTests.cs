using System;
using System.Numerics;
using Drone.Core.Flight;
using NUnit.Framework;

namespace Drone.Tests {
    
    public class FlightInputRouterTests {

        private sealed class ThrowingInputSource : IFlightInputSource {
            public FlightInput Sample() => throw new InvalidOperationException("Sensor lost");
        }

        private static FlightController FlyingController(FlightInputRouter router) {
            router.SetSource(new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) });
            var controller = new FlightController(new FakeClearance { IsCleared = true }, router);
            controller.Arm();
            controller.Tick(0.5f);
            return controller;
        }

        [Test]
        public void Sample_SourceThrows_ReturnsNeutralAndReportsTheFault() {
            var router = new FlightInputRouter();
            router.SetSource(new ThrowingInputSource());

            var input = router.Sample();

            Assert.AreEqual(Vector3.Zero, input.MoveDirection);
            Assert.AreEqual(0f, input.Yaw);
            Assert.IsTrue(router.SourceFaulted);
        }

        [TestCase(float.NaN, 0f, 0f, 0f)]
        [TestCase(0f, float.NaN, 0f, 0f)]
        [TestCase(0f, 0f, float.NaN, 0f)]
        [TestCase(0f, 0f, 0f, float.NaN)]
        [TestCase(float.PositiveInfinity, 0f, 0f, 0f)]
        [TestCase(0f, float.NegativeInfinity, 0f, 0f)]
        [TestCase(0f, 0f, 0f, float.PositiveInfinity)]
        public void Sample_NonFiniteInput_ReturnsNeutralAndReportsTheFault(float x, float y, float z, float yaw) {
            var router = new FlightInputRouter();
            router.SetSource(new FakeInputSource { Input = new FlightInput(new Vector3(x, y, z), yaw) });

            var input = router.Sample();

            Assert.AreEqual(Vector3.Zero, input.MoveDirection);
            Assert.AreEqual(0f, input.Yaw);
            Assert.IsTrue(router.SourceFaulted);
        }

        [Test]
        public void Sample_SourceRecovers_ClearsTheFault() {
            var router = new FlightInputRouter();
            var source = new FakeInputSource { Input = new FlightInput(new Vector3(float.NaN, 0f, 0f), 0f) };
            router.SetSource(source);
            router.Sample();

            source.Input = new FlightInput(Vector3.UnitY, 0f);

            Assert.AreEqual(Vector3.UnitY, router.Sample().MoveDirection);
            Assert.IsFalse(router.SourceFaulted);
        }

        [Test]
        public void SetSource_AfterAFault_ClearsTheFault() {
            var router = new FlightInputRouter();
            router.SetSource(new ThrowingInputSource());
            router.Sample();

            router.SetSource(new FakeInputSource());

            Assert.IsFalse(router.SourceFaulted);
        }

        [Test]
        public void Controller_ArmedWithNaNClimb_DoesNotLiftOff() {
            var router = new FlightInputRouter();
            router.SetSource(new FakeInputSource { Input = new FlightInput(new Vector3(0f, float.NaN, 0f), 0f) });
            var controller = new FlightController(new FakeClearance { IsCleared = true }, router);
            controller.Arm();

            controller.Tick(0.02f);

            Assert.AreEqual(FlightState.Armed, controller.Telemetry.FlightState);
        }

        [Test]
        public void Controller_FlyingWithThrowingSource_KeepsTickingAndStillLands() {
            var router = new FlightInputRouter();
            var controller = FlyingController(router);
            router.SetSource(new ThrowingInputSource());

            Assert.DoesNotThrow(() => controller.Tick(0.1f));

            controller.Disarm();
            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
        }

        [Test]
        public void Controller_FlyingWithNaNInput_StaysFiniteAndStillLands() {
            var router = new FlightInputRouter();
            var controller = FlyingController(router);
            router.SetSource(new FakeInputSource { Input = new FlightInput(new Vector3(float.NaN, float.NaN, float.NaN), float.NaN) });

            controller.Tick(0.1f);

            Assert.IsTrue(float.IsFinite(controller.Telemetry.Altitude));
            Assert.IsTrue(float.IsFinite(controller.Telemetry.Heading));

            controller.Disarm();
            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
        }
        
        [Test]
        public void NewRouter_SamplesNeutralInput() {
            var router = new FlightInputRouter();

            var input = router.Sample();

            Assert.AreEqual(Vector3.Zero, input.MoveDirection);
            Assert.AreEqual(0f, input.Yaw);
        }

        [Test]
        public void Sample_AfterSetSource_ReturnsThatSourcesInput() {
            var router = new FlightInputRouter();
            router.SetSource(new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0.5f) });

            var input = router.Sample();

            Assert.AreEqual(Vector3.UnitY, input.MoveDirection);
            Assert.AreEqual(0.5f, input.Yaw);
        }

        [Test]
        public void SetSource_WhenSwapped_UsesOnlyNewSource() {
            var router = new FlightInputRouter();
            router.SetSource(new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) });

            router.SetSource(new FakeInputSource { Input = new FlightInput(Vector3.UnitX, 0f) });

            Assert.AreEqual(Vector3.UnitX, router.Sample().MoveDirection);
        }

        [Test]
        public void SetSource_WithNull_ReturnsNeutralInput() {
            var router = new FlightInputRouter();
            router.SetSource(new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) });

            router.SetSource(null);

            Assert.AreEqual(Vector3.Zero, router.Sample().MoveDirection);
        }

        [Test]
        public void Controller_WhenSourceSwappedMidFlight_FollowsNewSource() {
            var router = new FlightInputRouter();
            router.SetSource(new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) });
            var controller = new FlightController(new FakeClearance { IsCleared = true }, router);
            controller.Arm();
            controller.Tick(0.5f);

            router.SetSource(new FakeInputSource { Input = new FlightInput(-Vector3.UnitY, 0f) });
            var altitudeBefore = controller.Telemetry.Altitude;
            for (var i = 0; i < 10; i++) {
                controller.Tick(0.1f);
            }

            Assert.Less(controller.Telemetry.Altitude, altitudeBefore);
            Assert.AreEqual(FlightState.Flying, controller.Telemetry.FlightState);
        }
    }
}