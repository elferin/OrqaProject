using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;

namespace Drone.Providers {

    public sealed class GuestAuthorizationProvider : IAuthorizationProvider {

        public Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
            return Task.FromResult(AuthResult.Success());
        }
    }
}
