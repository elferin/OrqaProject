using Drone.Core.Authorization;
using Drone.Providers;
using UnityEngine;

namespace Drone.App {
    
    [CreateAssetMenu(menuName = "Drone/Guest Provider")]
    public class GuestAuthorizationProviderFactory : AuthorizationProviderFactory {
        
        public override IAuthorizationProvider Create() {
            return new GuestAuthorizationProvider();
        }
    }
}