using Drone.Core.Flight;

namespace Drone.Tests {
    
    sealed class FakeInputSource : IFlightInputSource {

        public string DisplayName => "Fake";

        public FlightInput Input { get; set; }
        public FlightInput Sample() => Input;
    }
}