using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Drone.Frontend.Tests {

    public class KeyboardInputSourceTests : InputTestFixture {

        [Test]
        public void Sample_NoKeyboard_ReturnsNoInput() {
            var input = new KeyboardInputSource().Sample();

            Assert.AreEqual(0f, input.MoveDirection.Length(), 0.001f);
            Assert.AreEqual(0f, input.Yaw, 0.001f);
        }

        [Test]
        public void Sample_NoKeysHeld_ReturnsNoInput() {
            InputSystem.AddDevice<Keyboard>();

            var input = new KeyboardInputSource().Sample();

            Assert.AreEqual(0f, input.MoveDirection.Length(), 0.001f);
            Assert.AreEqual(0f, input.Yaw, 0.001f);
        }

        [Test]
        public void Sample_MovementKeysHeld_MapToTheirAxes() {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var source = new KeyboardInputSource();

            Press(keyboard.wKey);
            Assert.AreEqual(1f, source.Sample().MoveDirection.Z, 0.001f);
            Release(keyboard.wKey);

            Press(keyboard.sKey);
            Assert.AreEqual(-1f, source.Sample().MoveDirection.Z, 0.001f);
            Release(keyboard.sKey);

            Press(keyboard.dKey);
            Assert.AreEqual(1f, source.Sample().MoveDirection.X, 0.001f);
            Release(keyboard.dKey);

            Press(keyboard.aKey);
            Assert.AreEqual(-1f, source.Sample().MoveDirection.X, 0.001f);
            Release(keyboard.aKey);

            Press(keyboard.spaceKey);
            Assert.AreEqual(1f, source.Sample().MoveDirection.Y, 0.001f);
            Release(keyboard.spaceKey);

            Press(keyboard.leftShiftKey);
            Assert.AreEqual(-1f, source.Sample().MoveDirection.Y, 0.001f);
        }

        [Test]
        public void Sample_YawKeysHeld_MapToYaw() {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var source = new KeyboardInputSource();

            Press(keyboard.eKey);
            Assert.AreEqual(1f, source.Sample().Yaw, 0.001f);
            Release(keyboard.eKey);

            Press(keyboard.qKey);
            Assert.AreEqual(-1f, source.Sample().Yaw, 0.001f);
        }

        [Test]
        public void Sample_OppositeKeysHeld_CancelOut() {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            Press(keyboard.sKey);

            Assert.AreEqual(0f, new KeyboardInputSource().Sample().MoveDirection.Z, 0.001f);
        }
    }
}
