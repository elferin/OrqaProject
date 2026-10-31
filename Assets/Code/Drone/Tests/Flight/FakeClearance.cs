using Drone.Core.Authorization;

namespace Drone.Tests {
    
    sealed class FakeClearance : IFlightClearance {
        
        public bool IsCleared { get; set; }
    }
}