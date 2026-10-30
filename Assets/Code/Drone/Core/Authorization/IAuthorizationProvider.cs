using System.Threading;
using System.Threading.Tasks;

namespace Drone.Core.Authorization {
    
    public interface IAuthorizationProvider {

        Task<AuthResult> AuthorizeAsync(CancellationToken cancellationToken);
    }
}
