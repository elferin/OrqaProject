namespace Drone.Core.Flight {
    
    public sealed class FlightInputRouter : IFlightInputSource {
        
        private IFlightInputSource _inputSource;
        
        public FlightInput Sample() {
            
            if (_inputSource is null) {
                return default;
            }
            
            return _inputSource.Sample();
        }

        public void SetSource(IFlightInputSource source) {
            
            _inputSource = source;
        }
    }
}