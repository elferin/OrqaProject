using System;
using System.Threading;
using System.Threading.Tasks;

namespace Drone.Core.Authorization {
    public sealed class AuthorizationGate : IFlightClearance, IDisposable {
        
        private const string AUTH_CANCELLED = "Authorization cancelled by provider";
        public AuthState State { get; private set; }
        public string LastFailureReason { get; private set; }
        public bool IsCleared => State == AuthState.Authorized;
        public event Action StateChanged;
        
        private CancellationTokenSource _cts;
        private Task<AuthResult> _attempt;
        
        public void Request(IAuthorizationProvider provider) {
            CancelCurrentAttempt();
            
            LastFailureReason = null;
            SetState(AuthState.Authorizing);

            _cts = new CancellationTokenSource();
            try {
                _attempt = provider.AuthorizeAsync(_cts.Token);
            }
            catch (Exception e) {
                _attempt = Task.FromException<AuthResult>(e);
            }
            
            Pump();
        }
        
        public void Revoke() {
            CancelCurrentAttempt();
            LastFailureReason = null;
            SetState(AuthState.Unauthorized);
        }

        public void Pump() {
            if (_attempt == null || !_attempt.IsCompleted) return;

            var attempt = _attempt;
            _attempt = null;
            
            if (attempt.Status != TaskStatus.RanToCompletion) {
                LastFailureReason = attempt.Exception?.GetBaseException().Message ?? AUTH_CANCELLED;
                SetState(AuthState.Failed);
                return;
            }

            var result = attempt.Result;
            
            if (result.IsSuccess) {
                SetState(AuthState.Authorized);
                return;
            }
            
            LastFailureReason = result.Reason;
            SetState(AuthState.Denied);
        }

        public void Dispose() {
            CancelCurrentAttempt();
        }
        
        private void CancelCurrentAttempt() {
            _attempt = null;
            
            if (_cts == null) {
                return;
            }

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        
        private void SetState(AuthState state) {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke();
        }
    }

}