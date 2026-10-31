namespace Drone.Core.Flight {
    
    public interface IFlightInputSource {
        
        FlightInput Sample();
    }
}