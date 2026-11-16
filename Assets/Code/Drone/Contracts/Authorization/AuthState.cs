namespace Drone.Contracts {
    
    public enum AuthState {
        Unauthorized, 
        Authorizing, 
        Authorized, 
        Denied, 
        Failed
    }
}