using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using Drone.Core.Session;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Drone.Frontend.Tests {

    public class OperatorControlsTests : InputTestFixture {

        private sealed class InstantProvider : IAuthorizationProvider {
            public int Calls { get; private set; }

            public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
                Calls++;
                return Task.FromResult(AuthResult.Success());
            }
        }

        private sealed class HangingProvider : IAuthorizationProvider {
            public int Calls { get; private set; }
            public CancellationToken LastToken { get; private set; }

            public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
                Calls++;
                LastToken = cancellationToken;
                return new TaskCompletionSource<AuthResult>().Task;
            }
        }

        private sealed class MarkedSource : IFlightInputSource {
            private readonly float _yaw;

            public MarkedSource(float yaw) {
                _yaw = yaw;
            }

            public FlightInput Sample() => new FlightInput(default, _yaw);
        }

        private DroneSession _session;
        private Keyboard _keyboard;

        [SetUp]
        public override void Setup() {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public override void TearDown() {
            _session.Dispose();
            base.TearDown();
        }

        private OperatorControls CreateControls(params IAuthorizationProvider[] providers) {
            _session = new DroneSession(providers);
            _session.AddInputSource(new MarkedSource(0f));
            return new OperatorControls(_session);
        }

        private void PressOnce(ButtonControl key, OperatorControls controls) {
            Press(key);
            controls.Tick();
            Release(key);
        }

        [Test]
        public void Tick_NoKeyPressed_RequestsNothing() {
            var provider = new InstantProvider();
            var controls = CreateControls(provider);

            controls.Tick();

            Assert.AreEqual(0, provider.Calls);
        }

        [Test]
        public void Tick_NoKeyboard_DoesNotThrow() {
            var controls = CreateControls(new InstantProvider());
            InputSystem.RemoveDevice(_keyboard);

            Assert.DoesNotThrow(() => controls.Tick());
        }

        [Test]
        public void Tick_DigitKey_RequestsTheMatchingProvider() {
            var first = new InstantProvider();
            var second = new InstantProvider();
            var controls = CreateControls(first, second);

            PressOnce(_keyboard.digit2Key, controls);

            Assert.AreEqual(0, first.Calls);
            Assert.AreEqual(1, second.Calls);
            Assert.AreEqual(AuthState.Authorized, _session.Gate.State);
        }

        [Test]
        public void Tick_DigitKeyWithNoProvider_DoesNothing() {
            var provider = new InstantProvider();
            var controls = CreateControls(provider);

            PressOnce(_keyboard.digit2Key, controls);

            Assert.AreEqual(0, provider.Calls);
            Assert.AreEqual(AuthState.Unauthorized, _session.Gate.State);
        }

        [Test]
        public void Tick_RevokeKeyDuringAuthorizing_CancelsTheAttempt() {
            var provider = new HangingProvider();
            var controls = CreateControls(provider);
            PressOnce(_keyboard.digit1Key, controls);
            Assert.AreEqual(AuthState.Authorizing, _session.Gate.State);

            PressOnce(_keyboard.rKey, controls);

            Assert.AreEqual(AuthState.Unauthorized, _session.Gate.State);
            Assert.IsTrue(provider.LastToken.IsCancellationRequested);
        }

        [Test]
        public void Tick_DigitKeyDuringAuthorizing_RetriesWithANewAttempt() {
            var hanging = new HangingProvider();
            var instant = new InstantProvider();
            var controls = CreateControls(hanging, instant);
            PressOnce(_keyboard.digit1Key, controls);

            PressOnce(_keyboard.digit2Key, controls);

            Assert.IsTrue(hanging.LastToken.IsCancellationRequested);
            Assert.AreEqual(AuthState.Authorized, _session.Gate.State);
        }

        [Test]
        public void Tick_ArmKeyWithoutClearance_StaysGrounded() {
            var controls = CreateControls(new InstantProvider());

            PressOnce(_keyboard.fKey, controls);

            Assert.AreEqual(FlightState.Grounded, _session.Controller.Telemetry.FlightState);
        }

        [Test]
        public void Tick_ArmKeyWithClearance_Arms() {
            var controls = CreateControls(new InstantProvider());
            PressOnce(_keyboard.digit1Key, controls);

            PressOnce(_keyboard.fKey, controls);

            Assert.AreEqual(FlightState.Armed, _session.Controller.Telemetry.FlightState);
        }

        [Test]
        public void Tick_DisarmKeyWhileArmed_ReturnsToGrounded() {
            var controls = CreateControls(new InstantProvider());
            PressOnce(_keyboard.digit1Key, controls);
            PressOnce(_keyboard.fKey, controls);

            PressOnce(_keyboard.gKey, controls);

            Assert.AreEqual(FlightState.Grounded, _session.Controller.Telemetry.FlightState);
        }

        [Test]
        public void Tick_SwapKey_CyclesSourcesAndWraps() {
            _session = new DroneSession(new IAuthorizationProvider[0]);
            _session.AddInputSource(new MarkedSource(0.5f));
            _session.AddInputSource(new MarkedSource(0.9f));
            var controls = new OperatorControls(_session);

            PressOnce(_keyboard.tabKey, controls);
            Assert.AreEqual(0.9f, _session.Router.Sample().Yaw, 0.001f);

            PressOnce(_keyboard.tabKey, controls);
            Assert.AreEqual(0.5f, _session.Router.Sample().Yaw, 0.001f);
        }

        [Test]
        public void Tick_SwapKeyWithSourceAddedLater_IncludesIt() {
            _session = new DroneSession(new IAuthorizationProvider[0]);
            _session.AddInputSource(new MarkedSource(0.5f));
            var controls = new OperatorControls(_session);
            _session.AddInputSource(new MarkedSource(0.9f));

            PressOnce(_keyboard.tabKey, controls);

            Assert.AreEqual(0.9f, _session.Router.Sample().Yaw, 0.001f);
        }
    }
}
