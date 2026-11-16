using Drone.Contracts;

namespace Drone.Tests {
    
    sealed class FakeInputSource : IFlightInputSource {

        public string DisplayName { get; set; } = "Fake";

        public FlightInput Input { get; set; }
        public FlightInput Sample() => Input;
    }
}