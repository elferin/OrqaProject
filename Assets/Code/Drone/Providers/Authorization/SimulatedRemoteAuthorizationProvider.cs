using System;
using System.Threading;
using System.Threading.Tasks;
using Drone.Core.Authorization;

namespace Drone.Providers {

    public sealed class SimulatedRemoteAuthorizationProvider : IAuthorizationProvider {

        private readonly TimeSpan _latency;
        private readonly TimeSpan _timeout;
        private readonly AuthResult _result;

        public SimulatedRemoteAuthorizationProvider(TimeSpan latency, TimeSpan timeout, AuthResult result) {
            _latency = latency;
            _timeout = timeout;
            _result = result;
        }

        public async Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken) {
            if (_latency > _timeout) {
                await Task.Delay(_timeout, cancellationToken).ConfigureAwait(false);
                throw new TimeoutException("Remote authorization timed out");
            }

            await Task.Delay(_latency, cancellationToken).ConfigureAwait(false);
            return _result;
        }
    }
}
