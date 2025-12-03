using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebHoney.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[]? _roles;

    public AuthorizeAttribute(params string[]? roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetString("UserId");
        
        if (string.IsNullOrEmpty(userId))
        {
            // Chưa đăng nhập, redirect về trang login
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        // Kiểm tra quyền nếu có yêu cầu role
        if (_roles != null && _roles.Length > 0)
        {
            var userRole = context.HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole))
            {
                // Không có quyền, redirect về trang AccessDenied
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
            
            // Kiểm tra role (hỗ trợ cả "Admin" và "ADMIN", "Customer" và "CUSTOMER")
            var normalizedUserRole = userRole.ToUpper();
            var hasPermission = _roles.Any(role => 
                role.Equals(userRole, StringComparison.OrdinalIgnoreCase) ||
                role.ToUpper() == normalizedUserRole ||
                (normalizedUserRole == "ADMIN" && role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            );
            
            if (!hasPermission)
            {
                // Không có quyền, redirect về trang AccessDenied
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}

