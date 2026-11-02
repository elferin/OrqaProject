namespace Drone.Core.Session {
    
    public interface IDroneFrontend {
        void Attach(DroneSession session);
        void FixedTick();
        void FrameTick(float deltaTime);
        void Detach();
    }
}