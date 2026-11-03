namespace Drone.Core.Session {

    public interface IDroneDisplay : IDroneClient {
        void Attach(IDroneReadout readout);
    }
}
