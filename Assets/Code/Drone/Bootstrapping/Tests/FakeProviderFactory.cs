using Drone.Core.Authorization;

namespace Drone.App.Tests {

    public sealed class FakeProviderFactory : AuthorizationProviderFactory {

        public IAuthorizationProvider Provider { get; set; }
        public bool ThrowOnCreate { get; set; }

        public override IAuthorizationProvider Create() {
            if (ThrowOnCreate) {
                throw new System.InvalidOperationException("Create failed");
            }

            return Provider;
        }
    }
}
