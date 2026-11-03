namespace Drone.Core.Session {

    public interface IDroneClient {
        void FrameTick(float deltaTime);
        void Detach();
    }
}
