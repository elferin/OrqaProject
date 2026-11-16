using System.Collections.Generic;

namespace Drone.Contracts {
    
    public interface IDroneOperator : IDroneReadout {
        
        IReadOnlyList<IAuthorizationProvider> Providers { get; }
        void RequestAuthorization(int providerIndex);
        void Revoke();
        void Arm();
        void Disarm();
        public void AddInputSource(IFlightInputSource flightInputSource);
        public void RemoveInputSource(IFlightInputSource flightInputSource);
        public void SelectNextInputSource();
    }
}