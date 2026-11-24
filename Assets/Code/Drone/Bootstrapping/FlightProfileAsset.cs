using Drone.Core.Flight;
using UnityEngine;

namespace Drone.App {

    [CreateAssetMenu(menuName = "Drone/Flight Profile")]
    public sealed class FlightProfileAsset : ScriptableObject {

        [SerializeField] private FlightProfile profile = FlightProfile.Default;
        
        public static FlightProfile ValidateOrDefault(FlightProfileAsset asset, Object context) {

            if (asset == null) {
                Debug.LogError("No flight profile assigned, using the default", context);
                return FlightProfile.Default;
            }

            if (asset.profile.IsValid) {
                return asset.profile;
            }

            Debug.LogError("Invalid flight profile, using the default", context);
            return FlightProfile.Default;
        }

        private void OnValidate() {
            if (!profile.IsValid) {
                Debug.LogWarning("Flight profile values must be positive and finite", this);
            }
        }
    }
}
