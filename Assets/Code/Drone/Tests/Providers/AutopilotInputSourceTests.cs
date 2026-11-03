using Drone.Core.Flight;
using Drone.Providers;
using NUnit.Framework;

namespace Drone.Tests {

    public class AutopilotInputSourceTests {

        [Test]
        public void Sample_BelowTarget_ClimbsAtFullInput() {
            var autopilot = new AutopilotInputSource(() => 0f, 10f);

            Assert.AreEqual(1f, autopilot.Sample().MoveDirection.Y, 0.001f);
        }

        [Test]
        public void Sample_NearTarget_SlowsDown() {
            var autopilot = new AutopilotInputSource(() => 9f, 10f);

            Assert.AreEqual(0.5f, autopilot.Sample().MoveDirection.Y, 0.001f);
        }

        [Test]
        public void Sample_AboveTarget_Descends() {
            var autopilot = new AutopilotInputSource(() => 20f, 10f);

            Assert.AreEqual(-1f, autopilot.Sample().MoveDirection.Y, 0.001f);
        }

        [Test]
        public void Sample_AtTarget_ReturnsNoMovement() {
            var autopilot = new AutopilotInputSource(() => 10f, 10f);

            Assert.AreEqual(0f, autopilot.Sample().MoveDirection.Y, 0.001f);
        }

        [Test]
        public void Controller_FlyingOnAutopilot_ClimbsToTargetAndHolds() {
            var router = new FlightInputRouter();
            var controller = new FlightController(new FakeClearance { IsCleared = true }, router, FlightProfile.Default);
            router.SetSource(new AutopilotInputSource(() => controller.Telemetry.Altitude, 3f));
            controller.Arm();

            for (var i = 0; i < 1000; i++) {
                controller.Tick(0.02f);
            }

            Assert.AreEqual(FlightState.Flying, controller.Telemetry.FlightState);
            Assert.AreEqual(3f, controller.Telemetry.Altitude, 0.05f);
        }
    }
}
