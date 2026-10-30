using System;
using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;

namespace Drone.Tests {
    
    sealed class FakeAuthorizationProvider : IAuthorizationProvider {
        
        private readonly TaskCompletionSource<AuthResult> _completion = new ();
        private readonly bool _ignoreCancellation;

        public FakeAuthorizationProvider(bool ignoreCancellation = false) {
            _ignoreCancellation = ignoreCancellation;
        }

        public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
            if (!_ignoreCancellation) {
                cancellationToken.Register(() => _completion.TrySetCanceled(cancellationToken));
            }
            return _completion.Task;
        }

        public void Complete(AuthResult result) => _completion.TrySetResult(result);
        public void Fail(Exception exception) => _completion.TrySetException(exception);
        public void CancelOnItsOwn() => _completion.TrySetCanceled();
        public bool WasCancelled => _completion.Task.IsCanceled;
    }
}
