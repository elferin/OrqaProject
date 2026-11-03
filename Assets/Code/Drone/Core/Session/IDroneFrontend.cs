namespace Drone.Core.Session {

    public interface IDroneFrontend : IDroneClient {
        void Attach(DroneSession session);
        void FixedTick();
    }
}
