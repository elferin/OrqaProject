namespace Drone.Contracts {

    public interface IDroneFrontend : IDroneClient {
        void Attach(IDroneOperator droneOperator);
    }
}
