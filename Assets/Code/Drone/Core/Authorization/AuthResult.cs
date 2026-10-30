namespace Drone.Core.Authorization {
    
    public readonly struct AuthResult {
        
        public readonly bool IsSuccess;
        public readonly string Reason;
        
        private AuthResult(bool isSuccess, string reason) {
            IsSuccess = isSuccess;
            Reason = reason;
        }

        public static AuthResult Success() => new AuthResult(true, null);
        public static AuthResult Denied(string reason) => new AuthResult(false, reason);
    }
}