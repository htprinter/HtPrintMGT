using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace HtERP;

public class AuthState(ClaimsPrincipal user) : AuthenticationState(user)
{
    public bool IsLike { get; set; }
    public int Coining { get; set; }
    public string? Phone { get; set; }
    public string? UserName { get; set; }
    public string? BuMen { get; set; }   //部门
    public bool? IsAdmin { get; set; }   //管理员
    public bool? IsAdminPro { get; set; }  //超级管理员
    public bool? IsDepart { get; set; }   //已离职
}
