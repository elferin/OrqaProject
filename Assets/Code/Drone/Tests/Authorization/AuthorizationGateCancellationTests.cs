using System;
using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;
using NUnit.Framework;

namespace Drone.Tests {

    public class AuthorizationGateCancellationTests {

        private sealed class ThrowingCallbackProvider : IAuthorizationProvider {
            private readonly AuthResult? _result;

            public ThrowingCallbackProvider(AuthResult? result) {
                _result = result;
            }

            public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
                cancellationToken.Register(() => throw new InvalidOperationException("Callback failed"));
                return _result.HasValue
                    ? Task.FromResult(_result.Value)
                    : new TaskCompletionSource<AuthResult>().Task;
            }
        }

        [Test]
        public void Revoke_AuthorizedWithThrowingCallback_StillRevokes() {
            using var gate = new AuthorizationGate();
            gate.Request(new ThrowingCallbackProvider(AuthResult.Success()));
            Assert.IsTrue(gate.IsCleared);

            Assert.DoesNotThrow(() => gate.Revoke());

            Assert.AreEqual(AuthState.Unauthorized, gate.State);
            Assert.IsFalse(gate.IsCleared);
        }

        [Test]
        public void Request_PendingWithThrowingCallback_StartsTheNewAttempt() {
            using var gate = new AuthorizationGate();
            gate.Request(new ThrowingCallbackProvider(null));

            Assert.DoesNotThrow(() => gate.Request(new ThrowingCallbackProvider(AuthResult.Denied("No licence"))));

            Assert.AreEqual(AuthState.Denied, gate.State);
        }

        [Test]
        public void Dispose_PendingWithThrowingCallback_DoesNotThrow() {
            var gate = new AuthorizationGate();
            gate.Request(new ThrowingCallbackProvider(null));

            Assert.DoesNotThrow(() => gate.Dispose());
        }
    }
}
