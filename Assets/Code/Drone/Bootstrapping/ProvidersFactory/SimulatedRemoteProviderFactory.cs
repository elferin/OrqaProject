using System;
using Drone.Core.Authorization;
using Drone.Providers;
using UnityEngine;

namespace Drone.App {
    
    [CreateAssetMenu(menuName = "Drone/Simulated Remote Provider")]
    public sealed class SimulatedRemoteProviderFactory : AuthorizationProviderFactory {

        [SerializeField] private float latencySeconds = 1.5f;
        [SerializeField] private float timeoutSeconds = 5f;
        [SerializeField] private bool approves = true;
        [SerializeField] private string denialReason = "No licence";

        public override IAuthorizationProvider Create() {
            var result = approves ? AuthResult.Success() : AuthResult.Denied(denialReason);
            return new SimulatedRemoteAuthorizationProvider(
                TimeSpan.FromSeconds(latencySeconds),
                TimeSpan.FromSeconds(timeoutSeconds),
                result);
        }
    }
}