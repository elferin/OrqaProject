using System;
using System.Collections.Generic;
using Drone.Contracts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drone.App {
    
    internal static class AuthorizationProvidersHelper {

        public static IReadOnlyList<IAuthorizationProvider> CreateProviders(
            AuthorizationProviderFactory[] authorizationProviderFactories,
            Object context) {
            
            var authorizationProviders = new List<IAuthorizationProvider>();
            foreach (var authorizationProviderFactory in authorizationProviderFactories) {
                if (authorizationProviderFactory == null) {
                    Debug.LogError("Empty authorization provider factory slot, digit keys will shift", context);
                    continue;
                }

                try {
                    authorizationProviders.Add(authorizationProviderFactory.Create());
                }
                catch (Exception exception) {
                    Debug.LogException(exception, context);
                }
            }

            if (authorizationProviders.Count == 0) {
                Debug.LogError("No authorization providers configured, the drone can't be authorized", context);
            }

            return authorizationProviders;
        }
    }
}