using System;
using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;

namespace Drone.Tests {
    sealed class ThrowingAuthorizationProvider : IAuthorizationProvider {
        public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
            throw new InvalidOperationException("Provider broke");
        }
    }
}
