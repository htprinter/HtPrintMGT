using Blazored.LocalStorage;
using HtERP.Components;
using HtERP.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;


namespace HtERP
{
    public class Program
    {
        internal static string? ConnectionString;//数据库连接符字串
        internal static string? DbTypeSettings;//数据库类型

        //扣款设置相关的全局变量
        public static bool isRun = false;
        public static double timeSpan = 30;
        public static DateTime? lastTime = null;
        public static DateTime? nextTime = null;
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true; //如果为 true，则当中心方法中引发异常时，会将详细异常消息返回到客户端。
                //hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(10); //如果服务器在此间隔内未发送消息，将自动发送 ping 消息以保持连接处于开启状态。
                hubOptions.ClientTimeoutInterval=TimeSpan.FromMinutes(100);//如果在此间隔(100分钟)时间内未收到消息（包括保持连接状态），服务器将认为客户端已断开连接。
            });

            //--------------------------自动扣款后台服务----------------------------
            // 注册自定义后台服务，使用单例模式以便在整个应用中共享状态
            builder.Services.AddSingleton<SettlementService>();
            // 将自定义服务注册为IHostedService
            builder.Services.AddHostedService(provider => provider.GetRequiredService<SettlementService>());


            //---------------------------身份认证相关服务------------------------------
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
            //注册级联身份验证状态服务：
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthentication().AddCookie(options =>
            {
                options.LoginPath = "/Account/Login"; // 你的登录页面路由  
            });

            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("UPMaster", policy => policy.RequireClaim("UPMaster"));
            });

            builder.Services.AddBlazoredLocalStorage(); //用于在客户端浏览器中进行本地存储的开源库
          
            ConnectionString = builder.Configuration.GetConnectionString("HtdbCon"); //获取appsettings.json里数据库连接符字串
            DbTypeSettings = builder.Configuration.GetConnectionString("DbType"); //获取appsettings.json里数据库类型

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection(); // 有https的话自动转到https

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}