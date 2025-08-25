using Blazored.LocalStorage;
using HtERP.Components;
using HtERP.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;


namespace HtERP
{
    public class Program
    {
        internal static string? ConnectionString;//���ݿ����ӷ��ִ�
        internal static string? DbTypeSettings;//���ݿ�����

        //�ۿ�������ص�ȫ�ֱ���
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
                hubOptions.EnableDetailedErrors = true; //���Ϊ true�������ķ����������쳣ʱ���Ὣ��ϸ�쳣��Ϣ���ص��ͻ��ˡ�
                //hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(10); //����������ڴ˼����δ������Ϣ�����Զ����� ping ��Ϣ�Ա������Ӵ��ڿ���״̬��
                hubOptions.ClientTimeoutInterval=TimeSpan.FromMinutes(100);//����ڴ˼��(100����)ʱ����δ�յ���Ϣ��������������״̬��������������Ϊ�ͻ����ѶϿ����ӡ�
            });

            //--------------------------�Զ��ۿ��̨����----------------------------
            // ע���Զ����̨����ʹ�õ���ģʽ�Ա�������Ӧ���й���״̬
            builder.Services.AddSingleton<SettlementService>();
            // ���Զ������ע��ΪIHostedService
            builder.Services.AddHostedService(provider => provider.GetRequiredService<SettlementService>());


            //---------------------------�����֤��ط���------------------------------
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
            //ע�ἶ�������֤״̬����
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthentication().AddCookie(options =>
            {
                options.LoginPath = "/Account/Login"; // ��ĵ�¼ҳ��·��  
            });

            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("UPMaster", policy => policy.RequireClaim("UPMaster"));
            });

            builder.Services.AddBlazoredLocalStorage(); //�����ڿͻ���������н��б��ش洢�Ŀ�Դ��
          
            ConnectionString = builder.Configuration.GetConnectionString("HtdbCon"); //��ȡappsettings.json�����ݿ����ӷ��ִ�
            DbTypeSettings = builder.Configuration.GetConnectionString("DbType"); //��ȡappsettings.json�����ݿ�����

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection(); // ��https�Ļ��Զ�ת��https

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}