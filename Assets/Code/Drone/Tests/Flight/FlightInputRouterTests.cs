using System.Numerics;
using Drone.Core.Flight;
using NUnit.Framework;

namespace Drone.Tests {
    
    public class FlightInputRouterTests {
        
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