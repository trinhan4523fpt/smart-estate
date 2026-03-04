using System.Security.Claims;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.Shared.Time;

namespace SmartEstate.Api.Security;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public HttpCurrentUser(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid? UserId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.User?.Identity?.IsAuthenticated != true) return null;

            // bạn có thể dùng ClaimTypes.NameIdentifier hoặc "sub"
            var id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? ctx.User.FindFirstValue("sub");

            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }
}
