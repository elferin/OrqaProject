using Drone.Core.Flight;

namespace Drone.Tests {
    
    sealed class FakeInputSource : IFlightInputSource {
        
        public FlightInput Input { get; set; }
        public FlightInput Sample() => Input;
    }
}