namespace Drone.Contracts {

    public interface IDroneDisplay : IDroneClient {
        void Attach(IDroneReadout readout);
    }
}
