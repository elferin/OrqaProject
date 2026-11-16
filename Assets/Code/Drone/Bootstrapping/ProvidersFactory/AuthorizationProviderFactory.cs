using Drone.Contracts;
using UnityEngine;

namespace Drone.App {
    
    public abstract class AuthorizationProviderFactory : ScriptableObject {
        public abstract IAuthorizationProvider Create();
    }
}