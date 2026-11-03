using System.Numerics;
using Drone.Core.Flight;
using NUnit.Framework;

namespace Drone.Tests {
    
    public class FlightControllerTests {
        
        private static FlightController CreateFlightController(FakeClearance clearance) {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(clearance, input, FlightProfile.Default);
            controller.Arm();
            controller.Tick(0.5f);
            return controller;
        }
        
        [TestCase(0f, 5f, 2f, 2f)]
        [TestCase(10f, 0f, 2f, 2f)]
        [TestCase(10f, 5f, 0f, 2f)]
        [TestCase(10f, 5f, 2f, 0f)]
        [TestCase(10f, 5f, -2f, 2f)]
        [TestCase(10f, float.NaN, 2f, 2f)]
        public void Constructor_InvalidProfile_Throws(float acceleration, float maxSpeed, float landingSpeed, float yawSpeed) {
            var profile = new FlightProfile {
                Acceleration = acceleration,
                MaxSpeed = maxSpeed,
                LandingSpeed = landingSpeed,
                YawSpeed = yawSpeed
            };

            Assert.Throws<System.ArgumentException>(() => new FlightController(new FakeClearance(), new FakeInputSource(), profile));
        }

        [Test]
        public void Tick_CustomProfile_UsesItsMaxSpeed() {
            var profile = FlightProfile.Default;
            profile.MaxSpeed = 1f;
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, profile);
            controller.Arm();

            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            Assert.AreEqual(1f, controller.Telemetry.Speed, 0.001f);
        }

        [Test]
        public void Tick_CustomProfile_UsesItsLandingSpeed() {
            var profile = FlightProfile.Default;
            profile.LandingSpeed = 0.5f;
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, profile);
            controller.Arm();
            controller.Tick(1f);

            controller.Disarm();
            controller.Tick(0.1f);

            Assert.AreEqual(0.5f, controller.Telemetry.Speed, 0.001f);
        }

        [Test]
        public void StateChanged_ThroughAFullFlight_FiresOnEachTransition() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            var states = new System.Collections.Generic.List<FlightState>();
            controller.StateChanged += () => states.Add(controller.Telemetry.FlightState);

            controller.Arm();
            controller.Tick(0.1f);
            controller.Tick(0.1f);
            controller.Disarm();
            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            CollectionAssert.AreEqual(new[] { FlightState.Armed, FlightState.Flying, FlightState.Landing, FlightState.Grounded }, states);
        }

        [Test]
        public void StateChanged_ClearanceLostInFlight_ReportsLanding() {
            var clearance = new FakeClearance { IsCleared = true };
            var controller = CreateFlightController(clearance);
            var states = new System.Collections.Generic.List<FlightState>();
            controller.StateChanged += () => states.Add(controller.Telemetry.FlightState);

            clearance.IsCleared = false;
            controller.Tick(0.01f);

            CollectionAssert.AreEqual(new[] { FlightState.Landing }, states);
        }

        [Test]
        public void NewController_IsGrounded() {
            var controller = new FlightController(new FakeClearance(), new FakeInputSource(), FlightProfile.Default);

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
        }

        [Test]
        public void Arm_WithoutClearance_StaysGrounded() {
            var controller = new FlightController(new FakeClearance { IsCleared = false }, new FakeInputSource(), FlightProfile.Default);

            controller.Arm();

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
        }

        [Test]
        public void Arm_WithClearance_BecomesArmed() {
            var controller = new FlightController(new FakeClearance { IsCleared = true }, new FakeInputSource(), FlightProfile.Default);

            controller.Arm();

            Assert.AreEqual(FlightState.Armed, controller.Telemetry.FlightState);
        }
        
        [Test]
        public void Tick_WhenGrounded_IgnoresInput() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);

            controller.Tick(0.02f);

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
            Assert.AreEqual(0f, controller.Telemetry.Altitude);
        }

        [Test]
        public void Tick_WhenArmedWithUpwardInput_TakesOff() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            controller.Tick(0.02f);

            Assert.AreEqual(FlightState.Flying, controller.Telemetry.FlightState);
            Assert.Greater(controller.Telemetry.Altitude, 0f);
            Assert.Greater(controller.Telemetry.Speed, 0f);
        }

        [Test]
        public void Tick_WhenArmedWithNoInput_StaysArmed() {
            var controller = new FlightController(new FakeClearance { IsCleared = true }, new FakeInputSource(), FlightProfile.Default);
            controller.Arm();

            controller.Tick(0.02f);

            Assert.AreEqual(FlightState.Armed, controller.Telemetry.FlightState);
            Assert.AreEqual(0f, controller.Telemetry.Altitude);
        }
        
        [Test]
        public void Tick_WhileFlying_AccumulatesAltitude() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            controller.Tick(0.1f);
            var altitudeAfterOneTick = controller.Telemetry.Altitude;
            controller.Tick(0.1f);

            Assert.Greater(controller.Telemetry.Altitude, altitudeAfterOneTick);
        }
        
        [Test]
        public void Tick_WhenClearanceLostInFlight_StartsLanding() {
            var clearance = new FakeClearance { IsCleared = true };
            var controller = CreateFlightController(clearance);

            clearance.IsCleared = false;
            controller.Tick(0.02f);

            Assert.AreEqual(FlightState.Landing, controller.Telemetry.FlightState);
        }

        [Test]
        public void Tick_WhileLanding_DescendsDespiteUpwardInput() {
            var clearance = new FakeClearance { IsCleared = true };
            var controller = CreateFlightController(clearance);
            var altitudeBefore = controller.Telemetry.Altitude;

            clearance.IsCleared = false;
            controller.Tick(0.1f);

            Assert.Less(controller.Telemetry.Altitude, altitudeBefore);
        }

        [Test]
        public void Tick_WhenLandingReachesGround_BecomesGroundedAndStopped() {
            var clearance = new FakeClearance { IsCleared = true };
            var controller = CreateFlightController(clearance);

            clearance.IsCleared = false;
            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
            Assert.AreEqual(0f, controller.Telemetry.Altitude);
            Assert.AreEqual(0f, controller.Telemetry.Speed);
        }

        [Test]
        public void Tick_WhenClearanceReturnsDuringLanding_KeepsLanding() {
            var clearance = new FakeClearance { IsCleared = true };
            var controller = CreateFlightController(clearance);

            clearance.IsCleared = false;
            controller.Tick(0.02f);
            var altitudeBefore = controller.Telemetry.Altitude;
            clearance.IsCleared = true;
            controller.Tick(0.1f);

            Assert.AreEqual(FlightState.Landing, controller.Telemetry.FlightState);
            Assert.Less(controller.Telemetry.Altitude, altitudeBefore);
        }

        [Test]
        public void Tick_WhenClearanceLostWhileArmed_BecomesGrounded() {
            var clearance = new FakeClearance { IsCleared = true };
            var controller = new FlightController(clearance, new FakeInputSource(), FlightProfile.Default);
            controller.Arm();

            clearance.IsCleared = false;
            controller.Tick(0.02f);

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
        }
        
        [Test]
        public void Tick_WhenFlyingDownIntoGround_StopsAtZero() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();
            controller.Tick(0.5f);

            input.Input = new FlightInput(-Vector3.UnitY, 0f);
            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            Assert.AreEqual(0f, controller.Telemetry.Altitude);
            Assert.AreEqual(0f, controller.Telemetry.Speed, 0.001f);
        }

        [Test]
        public void Tick_WithDiagonalInput_NeverExceedsMaxSpeed() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.One, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            Assert.LessOrEqual(controller.Telemetry.Speed, 5.001f);
        }

        [Test]
        public void Tick_WhenTakingOff_AcceleratesGradually() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            controller.Tick(0.02f);

            Assert.AreEqual(0.2f, controller.Telemetry.Speed, 0.001f);
        }
        
        [Test]
        public void Tick_WithFullInput_ReachesMaxSpeed() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            for (var i = 0; i < 100; i++) {
                controller.Tick(0.1f);
            }

            Assert.AreEqual(5f, controller.Telemetry.Speed, 0.001f);
        }
        
        [Test]
        public void Disarm_WhenArmed_BecomesGrounded() {
            var controller = new FlightController(new FakeClearance { IsCleared = true }, new FakeInputSource(), FlightProfile.Default);
            controller.Arm();

            controller.Disarm();

            Assert.AreEqual(FlightState.Grounded, controller.Telemetry.FlightState);
        }

        [Test]
        public void Disarm_WhileFlying_StartsLanding() {
            var controller = CreateFlightController(new FakeClearance { IsCleared = true });

            controller.Disarm();

            Assert.AreEqual(FlightState.Landing, controller.Telemetry.FlightState);
        }

        [Test]
        public void Tick_WithYawInput_TurnsDrone() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 1f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            controller.Tick(0.5f);

            Assert.AreEqual(1f, controller.Telemetry.Heading, 0.001f);
        }

        [Test]
        public void Tick_WithOversizedYawInput_LimitsTurnRate() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 10f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            controller.Tick(0.5f);

            Assert.AreEqual(1f, controller.Telemetry.Heading, 0.001f);
        }

        [Test]
        public void Tick_WhileFlying_ReportsPosition() {
            var input = new FakeInputSource { Input = new FlightInput(Vector3.UnitY, 0f) };
            var controller = new FlightController(new FakeClearance { IsCleared = true }, input, FlightProfile.Default);
            controller.Arm();

            controller.Tick(0.5f);

            Assert.AreEqual(controller.Telemetry.Altitude, controller.Telemetry.Position.Y, 0.001f);
        }
    }
}