using DocumentFormat.OpenXml.Spreadsheet;
using HtERP.Data;
using System.Diagnostics.CodeAnalysis;

namespace HtERP.Services
{
    public class SettlementService : IHostedService, IDisposable
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SettlementService> _logger;
        private Timer _timer;
        private bool _isRunning;
        private int _executionCount = 0;

        // 服务状态属性，供UI绑定
        public bool IsRunning => _isRunning;
        public IWebHostEnvironment Env => _env;
        public string LastExecutionTime { get; private set; } = "从未执行";

        public SettlementService(ILogger<SettlementService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        // 实现IHostedService的StartAsync，但不实际启动服务
        // 仅将服务置于"可启动"状态
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("后台服务已初始化，等待手动启动");
            return Task.CompletedTask;
        }

        // 用户触发的启动方法
        public void StartService(int p)
        {
            var timeSerCon = HongtengDbCon.Db.Queryable<自动扣款设置>().First();
            if (p == 1)
            {
                if (!_isRunning)
                {
                    _executionCount = 0;
                    _timer = new Timer(DoWorkOne, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
                    _isRunning = true;
                    _logger.LogInformation("用户启动了后台服务-执行一次");
                }
            }
            else if (p == 2)
            {
                if (!_isRunning)
                {
                    double d = timeSerCon.ExTimeSec ?? 28800;
                    _executionCount = 0;
                    _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(d));
                    _isRunning = true;
                    _logger.LogInformation("用户启动了后台服务-间隔执行");
                }
            }
            else if (p == 3)
            {
                if (!_isRunning)
                {
                    double d = timeSerCon.ExTimeSec ?? 28800;
                    _executionCount = 0;
                    _timer = new Timer(DoWorkDS, null, TimeSpan.FromSeconds(d), Timeout.InfiniteTimeSpan);
                    _isRunning = true;
                    _logger.LogInformation("用户启动了后台服务-定时执行");
                }
            }
            else
            {
                _logger.LogInformation("无执行");
            }


        }

        // 用户触发的停止方法
        public void StopService()
        {
            if (_isRunning)
            {
                _timer?.Change(Timeout.Infinite, 0);
                _isRunning = false;
                _logger.LogInformation("后台服务被用户停止了！");
            }
        }

        //执行一次的工作
        private void DoWorkOne([AllowNull] object state)
        {
            var timeSerCon = HongtengDbCon.Db.Queryable<自动扣款设置>().First();
            //自动扣款工作
            AutoDebit.Payment(timeSerCon.SetDay, timeSerCon.SetHour, timeSerCon.SetMinute);

            //停止timer
            _timer?.Change(Timeout.Infinite, 0);

            Program.lastTime = DateTime.Now;
            timeSerCon.LastTime = Program.lastTime;
            HongtengDbCon.Db.Updateable(timeSerCon).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommand();

            _isRunning = false;

        }

        // 间隔执行的工作
        private void DoWork([AllowNull] object state)
        {
            var timeSerCon = HongtengDbCon.Db.Queryable<自动扣款设置>().First();
            //自动扣款工作
            AutoDebit.Payment(timeSerCon.SetDay, timeSerCon.SetHour, timeSerCon.SetMinute);

            // 更新定时服务配置
            DateTime now = DateTime.Now;
            Program.lastTime = now;
            Program.nextTime = now.AddSeconds(Program.timeSpan);
            timeSerCon.LastTime = Program.lastTime;
            timeSerCon.NextTime = Program.nextTime;
            HongtengDbCon.Db.Updateable(timeSerCon).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommand();

            var count = Interlocked.Increment(ref _executionCount);
            LastExecutionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            _logger.LogInformation(
                "后台服务执行中。计数: {Count}，时间: {Time}",
                count,
                LastExecutionTime);
        }

        // 定时执行的工作
        private void DoWorkDS([AllowNull] object state)
        {
            var timeSerCon = HongtengDbCon.Db.Queryable<自动扣款设置>().First();
            //自动扣款工作
            AutoDebit.Payment(timeSerCon.SetDay, timeSerCon.SetHour, timeSerCon.SetMinute);

            var count = Interlocked.Increment(ref _executionCount);
            LastExecutionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            _logger.LogInformation(
                "后台服务执行中。计数: {Count}，时间: {Time}",
                count,
                LastExecutionTime);


            DateTime now = DateTime.Now;
            var timeList = HongtengDbCon.Db.Queryable<自动扣款时间设置>().ToList().OrderBy(it => it.ExeTime);
            var time = timeList
                .Select(it => it.ExeTimeOnly.ToTimeSpan())
                .FirstOrDefault(it => (it - now.TimeOfDay).TotalSeconds is >= 1, Timeout.InfiniteTimeSpan);
            if (time != Timeout.InfiniteTimeSpan)
            {
                //算出时间差
                DateTime newDateWithMidnight = now.Date + time;
                timeSerCon.ExTimeSec = (newDateWithMidnight - now).TotalSeconds;

            }
            else if (timeList.Any())
            {
                //算出明天第一个时间与现在之间的时间差
                DateTime newDateWithMidnight = now.Date.AddDays(1) + (timeList.First()?.ExeTimeOnly.ToTimeSpan() ?? TimeSpan.Zero);
                timeSerCon.ExTimeSec = (newDateWithMidnight - now).TotalSeconds;

            }

            Program.timeSpan = timeSerCon.ExTimeSec ?? 28800;
            Program.lastTime = now;
            Program.nextTime = now.AddSeconds(Program.timeSpan);
            timeSerCon.LastTime = Program.lastTime;
            timeSerCon.NextTime = Program.nextTime;
            HongtengDbCon.Db.Updateable(timeSerCon).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommand();
            //重设下次运行时间
            _timer?.Change(TimeSpan.FromSeconds(Program.timeSpan), Timeout.InfiniteTimeSpan);

        }


        // 实现IHostedService的StopAsync
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("后台服务正在停止");
            StopService();
            return Task.CompletedTask;
        }

        // 资源释放
        public void Dispose()
        {
            _timer?.Dispose();
        }


    }
}
