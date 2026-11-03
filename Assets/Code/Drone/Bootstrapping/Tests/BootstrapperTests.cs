using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;
using Drone.Core.Flight;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Drone.App.Tests {

    public class BootstrapperTests {

        private const string EMPTY_SLOT_ERROR = "Empty authorization provider factory slot, digit keys will shift";
        private const string NO_PROVIDERS_ERROR = "No authorization providers configured, the drone can't be authorized";
        private const string INVALID_PROFILE_ERROR = "Invalid flight profile, using the default";
        private const string NO_PROFILE_ERROR = "No flight profile assigned, using the default";
        private const int FIXED_TICKS_TO_LIFT_OFF = 3;

        private sealed class ControlledProvider : IAuthorizationProvider {
            private readonly TaskCompletionSource<AuthResult> _completion = new TaskCompletionSource<AuthResult>();

            public CancellationToken LastToken { get; private set; }

            public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
                LastToken = cancellationToken;
                return _completion.Task;
            }

            public void Complete(AuthResult result) => _completion.TrySetResult(result);
        }

        private sealed class InstantProvider : IAuthorizationProvider {
            private readonly AuthResult _result;

            public InstantProvider(AuthResult result) {
                _result = result;
            }

            public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) => Task.FromResult(_result);
        }

        private readonly List<Object> _created = new List<Object>();
        private GameObject _root;

        [UnityTearDown]
        public IEnumerator TearDown() {
            foreach (var created in _created) {
                Object.Destroy(created);
            }
            _created.Clear();
            _root = null;
            yield return null;
        }

        private FakeProviderFactory FactoryFor(IAuthorizationProvider provider) {
            var factory = ScriptableObject.CreateInstance<FakeProviderFactory>();
            factory.Provider = provider;
            _created.Add(factory);
            return factory;
        }

        private FakeFrontend AddFrontend() {
            var child = new GameObject("Frontend");
            child.transform.SetParent(_root.transform, false);
            return child.AddComponent<FakeFrontend>();
        }

        private void CreateRoot() {
            _root = new GameObject("Boot");
            _root.SetActive(false);
            _created.Add(_root);
        }

        private FlightProfileAsset CreateProfile() {
            var profile = ScriptableObject.CreateInstance<FlightProfileAsset>();
            _created.Add(profile);
            return profile;
        }

        private void Start(bool autoStart, params AuthorizationProviderFactory[] factories) {
            Start(autoStart, CreateProfile(), factories);
        }

        private void Start(bool autoStart, FlightProfileAsset profile, params AuthorizationProviderFactory[] factories) {
            var bootstrapper = _root.AddComponent<Bootstrapper>();
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("autoStart").boolValue = autoStart;
            serialized.FindProperty("flightProfile").objectReferenceValue = profile;
            var list = serialized.FindProperty("authorizationProviderFactories");
            list.arraySize = factories.Length;
            for (var i = 0; i < factories.Length; i++) {
                list.GetArrayElementAtIndex(i).objectReferenceValue = factories[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _root.SetActive(true);
        }

        private FakeFrontend StartWithFrontend(bool autoStart, params AuthorizationProviderFactory[] factories) {
            CreateRoot();
            var frontend = AddFrontend();
            Start(autoStart, factories);
            return frontend;
        }

        private static IEnumerator WaitFixedTicks(int count) {
            for (var i = 0; i < count; i++) {
                yield return new WaitForFixedUpdate();
            }
        }

        [Test]
        public void Awake_WithFrontends_AttachesEachOnceToTheSameSession() {
            CreateRoot();
            var first = AddFrontend();
            var second = AddFrontend();

            Start(false, FactoryFor(new InstantProvider(AuthResult.Success())));

            Assert.AreEqual(1, first.AttachCount);
            Assert.AreEqual(1, second.AttachCount);
            Assert.IsNotNull(first.Session);
            Assert.AreSame(first.Session, second.Session);
        }

        [UnityTest]
        public IEnumerator Display_IsAttachedToTheSameSessionTickedAndDetached() {
            CreateRoot();
            var frontend = AddFrontend();
            var displayObject = new GameObject("Display");
            displayObject.transform.SetParent(_root.transform, false);
            var display = displayObject.AddComponent<FakeDisplay>();
            Start(false, FactoryFor(new InstantProvider(AuthResult.Success())));

            yield return null;
            yield return null;

            Assert.AreSame(frontend.Session, display.Readout);
            Assert.Greater(display.FrameTicks, 0);

            Object.Destroy(_root);
            yield return null;

            Assert.AreEqual(1, display.DetachCount);
        }

        [Test]
        public void Awake_WithAutopilot_MakesItTheActiveSource() {
            var frontend = StartWithFrontend(false, FactoryFor(new InstantProvider(AuthResult.Success())));

            Assert.IsNotNull(frontend.Session.ActiveSource);
        }

        [UnityTest]
        public IEnumerator Running_TicksFrontends() {
            var frontend = StartWithFrontend(false, FactoryFor(new InstantProvider(AuthResult.Success())));

            yield return WaitFixedTicks(2);
            yield return null;
            yield return null;

            Assert.Greater(frontend.FixedTicks, 0);
            Assert.Greater(frontend.FrameTicks, 0);
        }

        [UnityTest]
        public IEnumerator Running_DestroyedFrontend_IsSkippedAndOthersKeepTicking() {
            CreateRoot();
            var destroyed = AddFrontend();
            var survivor = AddFrontend();
            Start(false, FactoryFor(new InstantProvider(AuthResult.Success())));

            Object.Destroy(destroyed.gameObject);
            yield return null;
            var destroyedFrameTicks = destroyed.FrameTicks;
            var destroyedFixedTicks = destroyed.FixedTicks;
            var survivorFixedTicks = survivor.FixedTicks;
            var survivorFrameTicks = survivor.FrameTicks;

            yield return WaitFixedTicks(2);
            yield return null;
            yield return null;

            Assert.AreEqual(destroyedFrameTicks, destroyed.FrameTicks);
            Assert.AreEqual(destroyedFixedTicks, destroyed.FixedTicks);
            Assert.Greater(survivor.FixedTicks, survivorFixedTicks);
            Assert.Greater(survivor.FrameTicks, survivorFrameTicks);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OnDestroy_DetachesFrontendsBeforeDisposingTheSession() {
            var frontend = StartWithFrontend(false, FactoryFor(new InstantProvider(AuthResult.Success())));
            var session = frontend.Session;

            Object.Destroy(_root);
            yield return null;

            Assert.AreEqual(1, frontend.DetachCount);
            Assert.IsTrue(frontend.SessionWasLiveAtDetach);
            Assert.IsNull(session.ActiveSource);
        }

        [UnityTest]
        public IEnumerator OnDestroy_PendingAttempt_IsCancelled() {
            var provider = new ControlledProvider();
            StartWithFrontend(true, FactoryFor(provider));
            Assert.IsFalse(provider.LastToken.IsCancellationRequested);

            Object.Destroy(_root);
            yield return null;

            Assert.IsTrue(provider.LastToken.IsCancellationRequested);
        }

        [UnityTest]
        public IEnumerator OnDestroy_ThrowingDetach_StillCancelsThePendingAttempt() {
            var provider = new ControlledProvider();
            var frontend = StartWithFrontend(true, FactoryFor(provider));
            frontend.ThrowOnDetach = true;
            LogAssert.Expect(LogType.Exception, new Regex("Detach failed"));

            Object.Destroy(_root);
            yield return null;

            Assert.IsTrue(provider.LastToken.IsCancellationRequested);
        }

        [UnityTest]
        public IEnumerator AutoStart_Off_DoesNotAuthorizeOrFly() {
            var frontend = StartWithFrontend(false, FactoryFor(new InstantProvider(AuthResult.Success())));

            yield return WaitFixedTicks(FIXED_TICKS_TO_LIFT_OFF);

            Assert.AreEqual(AuthState.Unauthorized, frontend.Session.AuthState);
            Assert.AreEqual(FlightState.Grounded, frontend.Session.Telemetry.FlightState);
        }

        [UnityTest]
        public IEnumerator AutoStart_InstantProvider_AuthorizesAndLiftsOff() {
            var frontend = StartWithFrontend(true, FactoryFor(new InstantProvider(AuthResult.Success())));

            yield return WaitFixedTicks(FIXED_TICKS_TO_LIFT_OFF);

            Assert.AreEqual(AuthState.Authorized, frontend.Session.AuthState);
            Assert.AreEqual(FlightState.Flying, frontend.Session.Telemetry.FlightState);
        }

        [UnityTest]
        public IEnumerator AutoStart_SlowProvider_ArmsOnlyOnceCleared() {
            var provider = new ControlledProvider();
            var frontend = StartWithFrontend(true, FactoryFor(provider));

            yield return WaitFixedTicks(FIXED_TICKS_TO_LIFT_OFF);
            Assert.AreEqual(AuthState.Authorizing, frontend.Session.AuthState);
            Assert.AreEqual(FlightState.Grounded, frontend.Session.Telemetry.FlightState);

            provider.Complete(AuthResult.Success());
            yield return WaitFixedTicks(FIXED_TICKS_TO_LIFT_OFF);

            Assert.AreEqual(FlightState.Flying, frontend.Session.Telemetry.FlightState);
        }

        [UnityTest]
        public IEnumerator AutoStart_DeniedProvider_StaysGrounded() {
            var frontend = StartWithFrontend(true, FactoryFor(new InstantProvider(AuthResult.Denied("No licence"))));

            yield return WaitFixedTicks(FIXED_TICKS_TO_LIFT_OFF);

            Assert.AreEqual(AuthState.Denied, frontend.Session.AuthState);
            Assert.AreEqual(FlightState.Grounded, frontend.Session.Telemetry.FlightState);
        }

        [Test]
        public void Awake_EmptyFactorySlot_LogsAnErrorAndKeepsTheOthers() {
            LogAssert.Expect(LogType.Error, EMPTY_SLOT_ERROR);

            var frontend = StartWithFrontend(false, null, FactoryFor(new InstantProvider(AuthResult.Success())));

            Assert.AreEqual(1, frontend.Session.Providers.Count);
        }

        [Test]
        public void Awake_InvalidFlightProfile_LogsAnErrorAndStillBuildsTheSession() {
            LogAssert.Expect(LogType.Error, INVALID_PROFILE_ERROR);
            var profile = CreateProfile();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("profile").FindPropertyRelative("LandingSpeed").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateRoot();
            var frontend = AddFrontend();

            Start(false, profile, FactoryFor(new InstantProvider(AuthResult.Success())));

            Assert.IsNotNull(frontend.Session);
        }

        [Test]
        public void Awake_NoFlightProfile_LogsAnErrorAndStillBuildsTheSession() {
            LogAssert.Expect(LogType.Error, NO_PROFILE_ERROR);
            CreateRoot();
            var frontend = AddFrontend();

            Start(false, (FlightProfileAsset)null, FactoryFor(new InstantProvider(AuthResult.Success())));

            Assert.IsNotNull(frontend.Session);
        }

        [Test]
        public void Awake_NoFactories_LogsAnError() {
            LogAssert.Expect(LogType.Error, NO_PROVIDERS_ERROR);

            var frontend = StartWithFrontend(false);

            Assert.AreEqual(0, frontend.Session.Providers.Count);
        }

        [Test]
        public void Awake_ThrowingFactory_LogsAndKeepsTheOthers() {
            var broken = FactoryFor(null);
            broken.ThrowOnCreate = true;
            LogAssert.Expect(LogType.Exception, new Regex("Create failed"));

            var frontend = StartWithFrontend(false, broken, FactoryFor(new InstantProvider(AuthResult.Success())));

            Assert.AreEqual(1, frontend.Session.Providers.Count);
        }

        [UnityTest]
        public IEnumerator Awake_ThrowingAttach_LogsAndNeverTicksThatFrontend() {
            CreateRoot();
            var broken = AddFrontend();
            var working = AddFrontend();
            broken.ThrowOnAttach = true;
            LogAssert.Expect(LogType.Exception, new Regex("Attach failed"));

            Start(false, FactoryFor(new InstantProvider(AuthResult.Success())));
            yield return WaitFixedTicks(2);
            yield return null;

            Assert.AreEqual(0, broken.FixedTicks);
            Assert.AreEqual(0, broken.FrameTicks);
            Assert.Greater(working.FixedTicks, 0);
            Assert.Greater(working.FrameTicks, 0);
        }

        [UnityTest]
        public IEnumerator Running_ThrowingFrameTick_IsDroppedAndDetachedOnce() {
            CreateRoot();
            var broken = AddFrontend();
            var survivor = AddFrontend();
            broken.ThrowOnFrameTick = true;
            LogAssert.Expect(LogType.Exception, new Regex("FrameTick failed"));

            Start(false, FactoryFor(new InstantProvider(AuthResult.Success())));
            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(1, broken.FrameTicks);
            Assert.AreEqual(1, broken.DetachCount);
            Assert.Greater(survivor.FrameTicks, 1);

            Object.Destroy(_root);
            yield return null;

            Assert.AreEqual(1, broken.DetachCount);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Running_ThrowingFixedTick_IsDroppedAndOthersKeepTicking() {
            CreateRoot();
            var broken = AddFrontend();
            var survivor = AddFrontend();
            broken.ThrowOnFixedTick = true;
            LogAssert.Expect(LogType.Exception, new Regex("FixedTick failed"));

            Start(false, FactoryFor(new InstantProvider(AuthResult.Success())));
            yield return WaitFixedTicks(3);

            Assert.AreEqual(1, broken.FixedTicks);
            Assert.AreEqual(1, broken.DetachCount);
            Assert.Greater(survivor.FixedTicks, 1);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OnDestroy_ThrowingDetach_StillDetachesTheOthers() {
            CreateRoot();
            var broken = AddFrontend();
            var other = AddFrontend();
            broken.ThrowOnDetach = true;
            Start(false, FactoryFor(new InstantProvider(AuthResult.Success())));
            LogAssert.Expect(LogType.Exception, new Regex("Detach failed"));

            Object.Destroy(_root);
            yield return null;

            Assert.AreEqual(1, other.DetachCount);
        }
    }
}
