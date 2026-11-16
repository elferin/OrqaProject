using System.Threading;
using System.Threading.Tasks;

namespace Drone.Contracts {
    
    public interface IAuthorizationProvider {

        Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken);
    }
}
