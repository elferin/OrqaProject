namespace Drone.Core.Authorization {
    
    public interface IFlightClearance {
        bool IsCleared { get; }
    }
}