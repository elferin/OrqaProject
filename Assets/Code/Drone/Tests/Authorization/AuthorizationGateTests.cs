using System;
using Drone.Core.Authorization;
using NUnit.Framework;

namespace Drone.Tests {
    
    public class AuthorizationGateTests {

        private FakeAuthorizationProvider _authProvider;

        [SetUp]
        public void Setup() {
            _authProvider = new FakeAuthorizationProvider();
        }
        
        [Test]
        public void NewGate_IsUnauthorizedAndNotCleared() {
            using var gate = new AuthorizationGate();

            Assert.AreEqual(AuthState.Unauthorized, gate.State);
            Assert.IsFalse(gate.IsCleared);
        }
        
        [Test]
        public void Request_WhenIdle_MovesToAuthorizing() {
            using var gate = new AuthorizationGate();
            
            gate.Request(_authProvider);
            
            Assert.AreEqual(AuthState.Authorizing, gate.State);
            Assert.IsFalse(gate.IsCleared);
        }
        
        [Test]
        public void Pump_AfterProviderSucceeds_BecomesAuthorized() {
            using var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            _authProvider.Complete(AuthResult.Success());
            gate.Pump();

            Assert.AreEqual(AuthState.Authorized, gate.State);
            Assert.IsTrue(gate.IsCleared);
        }
        
        [Test]
        public void Pump_AfterProviderDenies_BecomesDeniedWithReason() {
            using var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            _authProvider.Complete(AuthResult.Denied("No licence"));
            gate.Pump();

            Assert.AreEqual(AuthState.Denied, gate.State);
            Assert.IsFalse(gate.IsCleared);
            Assert.AreEqual("No licence", gate.LastFailureReason);
        }
        
        [Test]
        public void Pump_WhenDenied_ReasonIsSetBeforeStateChanged() {
            using var gate = new AuthorizationGate();
            string reasonWhenNotified = null;

            gate.Request(_authProvider);
            gate.StateChanged += () => reasonWhenNotified = gate.LastFailureReason;
            _authProvider.Complete(AuthResult.Denied("No licence"));
            gate.Pump();

            Assert.AreEqual("No licence", reasonWhenNotified);
        }

        [Test]
        public void Request_AfterDenied_ClearsFailureReason() {
            using var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            _authProvider.Complete(AuthResult.Denied("No licence"));
            gate.Pump();
            gate.Request(new FakeAuthorizationProvider());

            Assert.IsNull(gate.LastFailureReason);
        }
        
        [Test]
        public void Pump_AfterProviderThrows_BecomesFailedWithReason() {
            using var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            _authProvider.Fail(new InvalidOperationException("Server unreachable"));
            gate.Pump();

            Assert.AreEqual(AuthState.Failed, gate.State);
            Assert.IsFalse(gate.IsCleared);
            Assert.AreEqual("Server unreachable", gate.LastFailureReason);
        }

        [Test]
        public void Pump_AfterProviderCancelsItself_BecomesFailed() {
            using var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            _authProvider.CancelOnItsOwn();
            gate.Pump();

            Assert.AreEqual(AuthState.Failed, gate.State);
            Assert.IsFalse(gate.IsCleared);
            Assert.IsNotNull(gate.LastFailureReason);
        }
        
        [Test]
        public void Revoke_DuringAttempt_IgnoresLateResult() {
            using var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            gate.Revoke();
            _authProvider.Complete(AuthResult.Success());
            gate.Pump();

            Assert.AreEqual(AuthState.Unauthorized, gate.State);
            Assert.IsFalse(gate.IsCleared);
            Assert.IsNull(gate.LastFailureReason);
        }

        [Test]
        public void Revoke_WhenAuthorized_RemovesClearance() {
            using var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            _authProvider.Complete(AuthResult.Success());
            gate.Pump();
            gate.Revoke();

            Assert.AreEqual(AuthState.Unauthorized, gate.State);
            Assert.IsFalse(gate.IsCleared);
            Assert.IsNull(gate.LastFailureReason);
        }
        
        [Test]
        public void Request_WhileAttemptRunning_IgnoresFirstProvidersLateResult() {
            using var gate = new AuthorizationGate();
            var first = new FakeAuthorizationProvider(ignoreCancellation: true);

            gate.Request(first);
            gate.Request(_authProvider);
            first.Complete(AuthResult.Success());
            gate.Pump();

            Assert.AreEqual(AuthState.Authorizing, gate.State);
            Assert.IsFalse(gate.IsCleared);

            _authProvider.Complete(AuthResult.Denied("No licence"));
            gate.Pump();

            Assert.AreEqual(AuthState.Denied, gate.State);
        }
        
        [Test]
        public void Dispose_DuringAttempt_IgnoresLateResult() {
            var gate = new AuthorizationGate();
            var provider = new FakeAuthorizationProvider(ignoreCancellation: true);
            var eventRaised = false;

            gate.Request(provider);
            gate.StateChanged += () => eventRaised = true;
            gate.Dispose();
            provider.Complete(AuthResult.Success());
            gate.Pump();

            Assert.IsFalse(gate.IsCleared);
            Assert.IsFalse(eventRaised);
        }

        [Test]
        public void Dispose_DuringAttempt_CancelsProvider() {
            var gate = new AuthorizationGate();

            gate.Request(_authProvider);
            gate.Dispose();

            Assert.IsTrue(_authProvider.WasCancelled);
        }
        
        [Test]
        public void Request_WithInstantProvider_IsAuthorizedImmediately() {
            using var gate = new AuthorizationGate();
            _authProvider.Complete(AuthResult.Success());

            gate.Request(_authProvider);

            Assert.AreEqual(AuthState.Authorized, gate.State);
            Assert.IsTrue(gate.IsCleared);
        }
        
        [Test]
        public void Request_WhenProviderThrowsDirectly_BecomesFailedWithReason() {
            using var gate = new AuthorizationGate();

            gate.Request(new ThrowingAuthorizationProvider());

            Assert.AreEqual(AuthState.Failed, gate.State);
            Assert.AreEqual("Provider broke", gate.LastFailureReason);
        }
    }
}