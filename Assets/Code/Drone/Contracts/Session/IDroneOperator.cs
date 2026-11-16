namespace Drone.Contracts {

    public interface IDroneOperator : IDroneReadout {

        int ProviderCount { get; }
        void RequestAuthorization(int providerIndex);
        void Revoke();
        void Arm();
        void Disarm();
        void AddInputSource(IFlightInputSource flightInputSource);
        void RemoveInputSource(IFlightInputSource flightInputSource);
        void SelectNextInputSource();
    }
}