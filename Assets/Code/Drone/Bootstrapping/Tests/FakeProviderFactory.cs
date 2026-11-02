using Drone.Core.Authorization;

namespace Drone.App.Tests {

    public sealed class FakeProviderFactory : AuthorizationProviderFactory {

        public IAuthorizationProvider Provider { get; set; }

        public override IAuthorizationProvider Create() => Provider;
    }
}
