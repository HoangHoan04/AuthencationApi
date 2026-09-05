namespace AuthApi.Application.Common;

public static class PermissionCodes
{
    public const string HomeView = "HOME:VIEW";
    public const string UserView = "USER:VIEW";
    public const string UserCreate = "USER:CREATE";
    public const string UserUpdate = "USER:UPDATE";
    public const string UserDelete = "USER:DELETE";
    public const string SystemAdmin = "SYSTEM:ADMIN";
    public const string RoleView = "AUTH:ROLE:VIEW";
    public const string RoleManage = "AUTH:ROLE:MANAGE";
    public const string AppRotate = "AUTH:APP:ROTATE";
    public const string AuditView = "AUTH:AUDIT:VIEW";
    public const string MfaManage = "AUTH:MFA:MANAGE";
    public const string GatewayView = "GATEWAY:VIEW";
    public const string AiView = "AI:VIEW";
    public const string HubView = "HUB:VIEW";
    public const string HrmView = "HRM:VIEW";

    public static readonly (string Code, string Name, string Module, string Resource, string Action)[] Catalog =
    {
        (HomeView, "Xem trang chủ", "AUTH", "HOME", "VIEW"),
        (UserView, "Xem người dùng", "AUTH", "USER", "VIEW"),
        (UserCreate, "Tạo người dùng", "AUTH", "USER", "CREATE"),
        (UserUpdate, "Sửa người dùng", "AUTH", "USER", "UPDATE"),
        (UserDelete, "Xóa người dùng", "AUTH", "USER", "DELETE"),
        (SystemAdmin, "Quản trị hệ thống", "AUTH", "SYSTEM", "ADMIN"),
        (RoleView, "Xem vai trò", "AUTH", "ROLE", "VIEW"),
        (RoleManage, "Quản lý vai trò & quyền", "AUTH", "ROLE", "MANAGE"),
        (AppRotate, "Rotate client secret", "AUTH", "APP", "ROTATE"),
        (AuditView, "Xem audit log", "AUTH", "AUDIT", "VIEW"),
        (MfaManage, "Quản lý MFA", "AUTH", "MFA", "MANAGE"),
        (GatewayView, "Xem API Gateway", "GATEWAY", "GATEWAY", "VIEW"),
        (AiView, "Xem AI Gateway", "AI", "AI", "VIEW"),
        (HubView, "Xem Integration Hub", "HUB", "HUB", "VIEW"),
        (HrmView, "Xem HRM (placeholder)", "HRM", "HRM", "VIEW"),
    };

    public static readonly string[] SuperAdminPermissions = Catalog.Select(x => x.Code).ToArray();

    public static readonly string[] AdminPermissions =
    {
        HomeView, UserView, UserCreate, UserUpdate, SystemAdmin, RoleView, AuditView, MfaManage, AppRotate
    };

    public static readonly string[] OperatorPermissions =
    {
        HomeView, UserView, AuditView
    };

    public static readonly string[] ViewerPermissions =
    {
        HomeView, UserView
    };

    public static readonly string[] UserPermissions = ViewerPermissions;
}

public static class RoleCodes
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";
    public const string User = "User";
}
