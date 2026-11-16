namespace Drone.Contracts {
    
    public interface IFlightInputSource {
        
        string DisplayName { get; }
        FlightInput Sample();
    }
}