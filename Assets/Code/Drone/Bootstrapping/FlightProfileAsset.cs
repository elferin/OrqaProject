using Drone.Core.Flight;
using UnityEngine;

namespace Drone.App {

    [CreateAssetMenu(menuName = "Drone/Flight Profile")]
    public sealed class FlightProfileAsset : ScriptableObject {

        [SerializeField] private FlightProfile profile = FlightProfile.Default;

        public FlightProfile Profile => profile;
    }
}
