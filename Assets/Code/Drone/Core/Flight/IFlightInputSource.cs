namespace Drone.Core.Flight {
    
    public interface IFlightInputSource {
        
        string DisplayName { get; }
        FlightInput Sample();
    }
}