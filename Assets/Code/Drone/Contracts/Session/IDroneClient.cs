namespace Drone.Contracts {

    public interface IDroneClient {
        void FrameTick(float deltaTime);
        void Detach();
    }
}
