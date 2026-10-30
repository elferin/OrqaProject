namespace Drone.Core.Authorization {
    
    public enum AuthState {
        Unauthorized, 
        Authorizing, 
        Authorized, 
        Denied, 
        Failed
    }
}