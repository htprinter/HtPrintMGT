using SqlSugar;
using System.Reflection;

namespace HtERP.Data
{
    // 代码优先，实现创建可空类型，加“？”就是可空的类型
    public class HtTestdb
    {
        public static readonly SqlSugarClient db = new SqlSugarClient(new ConnectionConfig()
        {
            DbType = HongtengDbCon.SetDBType(Program.DbTypeSettings),//数据库类型存在appsettings.json里
            ConnectionString = Program.ConnectionString, //连接符字串存在appsettings.json里
            IsAutoCloseConnection = true,
            ConfigureExternalServices = new ConfigureExternalServices
            {
                //注意:  这儿AOP设置不能少
                EntityService = (c, p) =>
                {
                    /***高版C#写法***/
                    //支持string?和string  
                    if (p.IsPrimarykey == false && new NullabilityInfoContext()
                     .Create(c).WriteState is NullabilityState.Nullable)
                    {
                        p.IsNullable = true;
                    }
                   
                }
            },
            MoreSettings = new ConnMoreSettings
            {
                // IsCorrectErrorSqlParameterName = true, // 参数名有空格特殊符号使用兼容模式，对性能会有影响，能局部修改配置就局部，尽量不要全局使用
            }
        });
    }


    //实体与数据库结构一样
    public class 员工
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 员工ID { get; set; }
        [SugarColumn( ColumnDataType = "nvarchar(100)")]
        public string? 姓名 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 手机 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 住宅电话 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? QQ号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 电子邮件 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 地址 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 部门 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 头衔 { get; set; }
        public DateTime? 雇佣日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 身份证号 { get; set; }
        public string? 密码 { get; set; }
        public bool? 是否为管理员 { get; set; }
        public bool? 是否已离职 { get; set; }
        public bool? 超级管理员 { get; set; }
        public decimal? 账户余额 { get; set; }

    }

    public class 收款账单
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 收款单编号 { get; set; }
        public DateTime? 日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户ID { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 收款部门 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 付款类型 { get; set; }
        public decimal? 实收金额 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 账单状态 { get; set; }
        public bool? 发票已开 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 操作员 { get; set; }
        public bool? 有效 { get; set; }= true; 
        public decimal? 应收金额 { get; set; }
        public DateTime? 打印时间 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }

    }

    public class 工作表_数码印刷
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public int? 工单号 { get; set; }
        public DateTime? 输出日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 文件或工作名 { get; set; }
        public int? 打印长 { get; set; }
        public int? 打印宽 { get; set; }
        public float? 张数 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 纸张类型 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出设备 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(200)")]
        public string? 要求及文件位置 { get; set; }
        public decimal? 设计制作费 { get; set; }
        public decimal? 印刷费 { get; set; }
        public decimal? 其他费用 { get; set; }
        public decimal? 应收 { get; set; }
        public bool? 结清 { get; set; } = false;
        public decimal? 实收 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员1 { get; set; }
        public float? 工作量1 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员2 { get; set; }
        public float? 工作量2 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员3 { get; set; }
        public float? 工作量3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员4 { get; set; }
        public float? 工作量4 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public bool? 临时选定 { get; set; }
        public bool? 已开送货单 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 送货地点 { get; set; }
        public int? 送货单号 { get; set; }
        public int? 收款单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 发出人 { get; set; }
        public DateTime? 发出时间 { get; set; }
        public int? 发出单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 接收人 { get; set; }
        public bool? 需要发送 { get; set; }
        public bool? 已经发送 { get; set; }
        public DateTime? 接收时间 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 规格 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算状态 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 拼版员 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 操作员 { get; set; }
        public decimal? 起步价 { get; set; }
        public decimal? 阶梯价一 { get; set; }
        public decimal? 阶梯价二 { get; set; }
        public decimal? 阶梯价三 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 收费类别 { get; set; }
        public bool? 后加工 { get; set; }
        public bool? 急 { get; set; }
        public int? 结算号码 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方式 { get; set; }
        public bool? 双面 { get; set; }
        public decimal? 实收设计费 { get; set; }
        public decimal? 实收印制费 { get; set; }
        public bool? 已优惠 { get; set; }
        public bool IsDelete { get; set; }= false; //逻辑删除标记
        public long? 时间戳 { get; set; }
    }

    public class 工作表_CTP输出
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public int? 工单号 { get; set; }
        public DateTime? 输出日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 文件或工作名 { get; set; }
        public float? 长 { get; set; }
        public float? 宽 { get; set; }
        public float? 总色数 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? CTP板材型号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? CTP设备 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(200)")]
        public string? 输出要求 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 印刷机咬口要求 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 咬口方向 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 加网线数 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 网点类型 { get; set; }
        public decimal? 设计制作费 { get; set; }
        public decimal? 版费 { get; set; }
        public decimal? 其他费用 { get; set; }
        public decimal? 应收 { get; set; }
        public bool? 结清 { get; set; } = false;
        public decimal? 实收 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员1 { get; set; }
        public float? 工作量1 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员2 { get; set; }
        public float? 工作量2 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员3 { get; set; }
        public float? 工作量3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员4 { get; set; }
        public float? 工作量4 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public bool? 临时选定 { get; set; }
        public bool? 已开送货单 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 送货地点 { get; set; }
        public int? 发货单号 { get; set; }
        public int? 收款单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 发出人 { get; set; }
        public DateTime? 发出时间 { get; set; }
        public int? 发出单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 接收人 { get; set; }
        public bool? 需要发送 { get; set; }
        public bool? 已输出 { get; set; }
        public DateTime? 接收时间 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出员 { get; set; }
        public bool? 急 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算状态 { get; set; }
        public int? 结算号码 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方式 { get; set; }
        public decimal? 实收设计费 { get; set; }
        public decimal? 实收CTP费 { get; set; }
        public bool? 已优惠 { get; set; }
        public bool IsDelete { get; set; }= false;
        public long? 时间戳 { get; set; }
    }

    public class 工作表_菲林输出
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public int? 工单号 { get; set; }
        public DateTime? 输出日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 文件或工作名 { get; set; }
        public float? 长 { get; set; }
        public float? 宽 { get; set; }
        public float? 总色数 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出设备 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出要求 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 加网线数 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 网点类型 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出方向 { get; set; }
        public decimal? 设计制作费 { get; set; }
        public decimal? 菲林费 { get; set; }
        public decimal? 其他费用 { get; set; }
        public decimal? 应收 { get; set; }
        public bool? 结清 { get; set; }=false;
        public decimal? 实收 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员1 { get; set; }
        public float? 工作量1 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员2 { get; set; }
        public float? 工作量2 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员3 { get; set; }
        public float? 工作量3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员4 { get; set; }
        public float? 工作量4 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public bool? 临时选定 { get; set; }
        public bool? 已开送货单 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 送货地点 { get; set; }
        public int? 送货单号 { get; set; }
        public int? 收款单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 发出人 { get; set; }
        public DateTime? 发出时间 { get; set; }
        public int? 发出单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 接收人 { get; set; }
        public bool? 需要发送 { get; set; }
        [SugarColumn(ColumnName = "已经发送")]
        public bool? 已完成 { get; set; }
        public DateTime? 接收时间 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出员 { get; set; }
        public bool? 急 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算状态 { get; set; }
        public int? 结算号码 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方式 { get; set; }
        public decimal? 实收设计费 { get; set; }
        public decimal? 实收菲林费 { get; set; }
        public bool? 已优惠 { get; set; }
        public bool? 包装完成 { get; set; }
        public bool IsDelete { get; set; }= false;
        public long? 时间戳 { get; set; }
    }

    public class 工作表_设计制作
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public int? 工单号 { get; set; }
        public DateTime? 日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 文件或工作名 { get; set; }
        public float? 长 { get; set; }
        public float? 宽 { get; set; }
        public float? 总色数 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出要求 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 文件位置 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 发送至 { get; set; }
        public decimal? 设计制作费 { get; set; }
        public decimal? 施工费 { get; set; }
        public decimal? 其他费用 { get; set; }
        public decimal? 应收 { get; set; }
        public bool? 结清 { get; set; } = false;
        public decimal? 实收 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员1 { get; set; }
        public float? 工作量1 { get; set; }
        public string? 制作员2 { get; set; }
        public float? 工作量2 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员3 { get; set; }
        public float? 工作量3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员4 { get; set; }
        public float? 工作量4 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public bool? 临时选定 { get; set; }
        public bool? 已开送货单 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 送货地点 { get; set; }
        public int? 发货单号 { get; set; }
        public int? 收款单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 发出人 { get; set; }
        public DateTime? 发出时间 { get; set; }
        public int? 发出单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 接收人 { get; set; }
        public bool? 需要发送 { get; set; }
        public bool? 已完成 { get; set; }
        public DateTime? 接收时间 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出员 { get; set; }
        public bool? 急 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算状态 { get; set; }
        public int? 结算号码 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方式 { get; set; }
        public decimal? 实收设计费 { get; set; }
        public decimal? 实收施工费 { get; set; }
        public bool? 已优惠 { get; set; }
        public bool IsDelete { get; set; }= false;
        public long? 时间戳 { get; set; }

    }


    public class 工作表_后道加工
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public int? 工单号 { get; set; }
        public DateTime? 日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 文件或工作名 { get; set; }
        public float? 长 { get; set; }
        public float? 宽 { get; set; }
        public float? 数量 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 要求 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(20)")]
        public string? 打印类型 { get; set; }
        public int? 印刷单号 { get; set; }
        public bool? 淋膜 { get; set; }
        public bool? 覆膜 { get; set; }
        public bool? 切割 { get; set; }
        public bool? 装订 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 其他加工 { get; set; }
        public decimal? 覆膜单价 { get; set; }
        public decimal? 切割单价 { get; set; }
        public decimal? 装订单价 { get; set; }
        public decimal? 其他单价 { get; set; }
        public decimal? 设计制作费 { get; set; }
        public decimal? 加工费 { get; set; }
        public decimal? 其他费用 { get; set; }
        public decimal? 应收 { get; set; }
        public bool? 结清 { get; set; } = false;
        public decimal? 实收 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员1 { get; set; }
        public float? 工作量1 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员2 { get; set; }
        public float? 工作量2 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员3 { get; set; }
        public float? 工作量3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 制作员4 { get; set; }
        public float? 工作量4 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public bool? 临时选定 { get; set; }
        public bool? 已开送货单 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 送货地点 { get; set; }
        public int? 送货单号 { get; set; }
        public int? 收款单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 发出人 { get; set; }
        public DateTime? 发出时间 { get; set; }
        public int? 发出单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 接收人 { get; set; }
        public bool? 需要发送 { get; set; }
        public bool? 已经发送 { get; set; }
        public DateTime? 接收时间 { get; set; }

        public bool? 急 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算状态 { get; set; }
        public int? 结算号码 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方式 { get; set; }
        public decimal? 实收设计费 { get; set; }
        public decimal? 实收加工费 { get; set; }
        public bool? 已优惠 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 输出设备 { get; set; }
        public bool IsDelete { get; set; }= false;
        public long? 时间戳 { get; set; }

    }

    public class 工作表_彩喷写真
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public int? 工单号 { get; set; }
        public DateTime? 输出日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 客户 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 文件或工作名 { get; set; }
        public int? 打印长 { get; set; }
        public int? 打印宽 { get; set; }
        public float? 张数 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 打印类型 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 输出设备 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(200)")]
        public string? 要求及文件位置 { get; set; }
        public decimal? 设计制作费 { get; set; }
        public decimal? 价格 { get; set; }
        public decimal? 其他费用 { get; set; }
        public decimal? 应收 { get; set; }
        public bool? 结清 { get; set; } = false;
        public decimal? 实收 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 制作员1 { get; set; }
        public float? 工作量1 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 制作员2 { get; set; }
        public float? 工作量2 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 制作员3 { get; set; }
        public float? 工作量3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 制作员4 { get; set; }
        public float? 工作量4 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public bool? 临时选定 { get; set; }
        public bool? 已开送货单 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 送货地点 { get; set; }
        public int? 送货单号 { get; set; }
        public int? 收款单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 发出人 { get; set; }
        public DateTime? 发出时间 { get; set; }
        public int? 发出单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 结算方 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 接收人 { get; set; }
        public bool? 需要发送 { get; set; }
        public bool? 已经发送 { get; set; }
        public DateTime? 接收时间 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 规格 { get; set; }
        public decimal? 单价 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 结算状态 { get; set; }
        public bool? 后加工 { get; set; }
        public bool? 急 { get; set; }
        public int? 结算号码 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 结算方式 { get; set; }
        public decimal? 实收设计费 { get; set; }
        public decimal? 实收打印费 { get; set; }
        public bool? 已优惠 { get; set; }
        public bool IsDelete { get; set; }= false;
        public long? 时间戳 { get; set; }
    }

    public class dbModel   //通用生产单数据模型
    {
        public string? 分类 { get; set; }
        public int ID { get; set; }
        public int? 工单号 { get; set; }
        public DateTime? 日期 { get; set; }
        public string? 客户 { get; set; }
        public string? 品名 { get; set; }
        public float? 长 { get; set; }
        public float? 宽 { get; set; }
        public float? 数量 { get; set; }
        public string? 规格 { get; set; }
        public string? 要求说明 { get; set; }
        public decimal? 主费用 { get; set; }
        public decimal? 附加设计费 { get; set; }
        public decimal? 其他费用 { get; set; }
        public decimal? 价格 { get; set; }
        public bool? 结清 { get; set; }
        public decimal? 实收 { get; set; }
        public string? 制作员1 { get; set; }
        public float? 工作量1 { get; set; }
        public string? 制作员2 { get; set; }
        public float? 工作量2 { get; set; }
        public string? 制作员3 { get; set; }
        public float? 工作量3 { get; set; }
        public string? 制作员4 { get; set; }
        public float? 工作量4 { get; set; }
        public string? 备注 { get; set; }
        public bool? 临时选定 { get; set; }
        public bool? 已开送货单 { get; set; }
        public string? 送货地点 { get; set; }
        public int? 发货单号 { get; set; }
        public int? 收款单号 { get; set; }
        public string? 发出人 { get; set; }
        public DateTime? 发出时间 { get; set; }
        public int? 发出单号 { get; set; }
        public string? 结算方 { get; set; }
        public string? 接收人 { get; set; }
        public bool? 需要发送 { get; set; }
        public bool? 完成 { get; set; }
        public DateTime? 接收时间 { get; set; }
        public string? 输出员 { get; set; }
        public bool? 急 { get; set; }
        public string? 结算状态 { get; set; }
        public int? 结算号码 { get; set; }
        public string? 结算方式 { get; set; }
        public decimal? 实收设计费 { get; set; }
        public decimal? 实收CTP费 { get; set; }
        public bool? 已优惠 { get; set; }
        public bool IsDelete { get; set; }=false; //默认插入0
        public int? 客户编号 { get; set; }
        public bool? VIP客户 { get; set; }= false; //默认插入0
        public long? 时间戳 { get; set; }

    }
    public class 纸张类型
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }

        [SugarColumn(ColumnName = "纸张类型", ColumnDataType = "nvarchar(100)")]
        public string? 类型 { get; set; }
        public decimal? 价格 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 说明 { get; set; }
        public int? 库存 { get; set; }
    }

    public class 数码印刷费率表
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string 收费类别 { get; set; }= string.Empty;
        public decimal? 起步价 { get; set; }
        public decimal? 费率一 { get; set; }
        public decimal? 费率二{ get; set; }
        public decimal? 费率三 { get; set; }
        public decimal? 费率四 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 说明 { get; set; }
        public int? 一档张数 { get; set; }
        public int? 二档张数 { get; set; }
        public int? 三档张数 { get; set; }
        public int? 四档张数 { get; set; }
    }

    public class CTP板材型号
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? CTP板材型号ID { get; set; }
        public decimal? 价格 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 说明 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public int? 每箱数量 { get; set; }
        public int? 预警数量 { get; set; }
        public float? 长 { get; set; }
        public float? 宽 { get; set; }
        public float? 厚 { get; set; }
    }

    public class CTP加网线数
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public int? 加网线数 { get; set; }
    }
    public class CTP网点类型
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 网点类型 { get; set; }
    }
    public class CTP印刷机咬口要求
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 印刷机咬口要求 { get; set; }
    }

    public class 客户表
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 自动编号 { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户ID { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 公司名称 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 联系人 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 手机 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? username { get; set; }
        public string? password { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 地址 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 客户类型 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 备注 { get; set; }
        public bool? VIP客户 { get; set; }
        public float? CTP折扣率 { get; set; }
        public float? 菲林折扣率 { get; set; }
        public float? 数码折扣率 { get; set; }
        public float? 后加工折扣率 { get; set; }
        public float? 纯设计折扣率 { get; set; }
        public float? 其他折扣率 { get; set; }
        public decimal? 上期充值额 { get; set; }
        public decimal? 账户余额 { get; set; }
        public decimal? 未结金额 { get; set; }
        public bool IsDelete { get; set; }= false; //默认插入0

    }

    public class 公用设置
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }
        public decimal? 菲林起步价 { get; set; }
        public decimal? 菲林每平米价格 { get; set; }
        public decimal? CTP每平米价格 { get; set; }
        public decimal? 数码打样每平米价格 { get; set; }
        public decimal? 切割每米价格 { get; set; }
        public decimal? 资源费每张价格 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(10)")]
        public string? 默认折扣方案 { get; set; }
        public decimal? 计算每色一 { get; set; }
        public decimal? 计算每色二 { get; set; }
        public decimal? 计算每色三 { get; set; }
        public decimal? 计算每色四 { get; set; }
        public decimal? 计算每色五 { get; set; }
        public decimal? 计算每色六 { get; set; }
        public decimal? 计算每色七 { get; set; }
        public decimal? 计算每平米一 { get; set; }
        public decimal? 计算每平米二 { get; set; }
        public decimal? 计算每平米三 { get; set; }
        public decimal? 计算每平米四 { get; set; }
        public decimal? 计算每平米五 { get; set; }
        public decimal? 计算每平米六 { get; set; }
        public decimal? 计算每平米七 { get; set; }

    }

    public class 支出部门
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(10)")]
        public string? 部门 { get; set; }

    }

    public class 支出类别
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }

        [SugarColumn(ColumnName = "支出类别", ColumnDataType = "nvarchar(20)")]
        public string? 类别 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(20)")]
        public string? 说明 { get; set; }

    }


    public class 支出总表
    {
        //数据是自增需要加上IsIdentity 
        //数据库是主键需要加上IsPrimaryKey 
        //注意：要完全和数据库一致2个属性
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public DateTime? 日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 支出部门 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 支出类别 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 支出项目说明 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 收款单位 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 经办人 { get; set; }
        public decimal? 支出金额 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 支付方式 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 备注 { get; set; }
        public DateTime? 打印时间 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 规格 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 单位 { get; set; }
        public float? 数量 { get; set; }
        public decimal? 单价 { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 票据凭证 { get; set; }
        public float? 税率 { get; set; }
        public bool IsDelete { get; set; }= false; //默认插入0
    }

    public class 充值账户
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 客户ID { get; set; }
        public DateTime? 充值时间 { get; set; }
        public int? 使用优先级 { get; set; }
        public DateTime? 到期时间 { get; set; }
        public bool 是否有效 { get; set; }= true; //默认插入1
        public decimal? 充值金额 { get; set; }
        public decimal? 剩余金额 { get; set; }
        public bool 是否为赠送 { get; set; }=false;
        public int? 折扣链接 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 网点 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(32)")]
        public string? 操作员 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(32)")]
        public string? 交易方式 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 说明 { get; set; }
        public int? 账单号 { get; set; }
        public int? 流水号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(20)")]
        public string? 有效期 { get; set; }
        public int? 客户编号 { get; set; }

    }

    public class 充值折扣表
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        public decimal? 充值额 { get; set; }
        public float? CTP折扣率 { get; set; }
        public float? 菲林折扣率 { get; set; }
        public float? 数码折扣率 { get; set; }
        public float? 后加工折扣率 { get; set; }
        public float? 纯设计折扣率 { get; set; }
        public float? 其他折扣率1 { get; set; }
        public float? 其他折扣率2 { get; set; }
        public float? 其他折扣率3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 备注 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(20)")]
        public string? 折扣方案 { get; set; }
    }

    public class 充值折扣方案
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 编号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(20)")]
        public string? 优惠方案名 { get; set; }
        public float? CTP折扣率 { get; set; }
        public float? 菲林折扣率 { get; set; }
        public float? 数码折扣率 { get; set; }
        public float? 后加工折扣率 { get; set; }
        public float? 纯设计折扣率 { get; set; }
        public float? 其他折扣率1 { get; set; }
        public float? 其他折扣率2 { get; set; }
        public float? 其他折扣率3 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 备注 { get; set; }
        public int? 使用有效期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(20)")]
        public string? 有效期描述 { get; set; }
    }

    public class 交易流水表
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 流水号 { get; set; }
        public DateTime? 日期 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 客户 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 交易类型 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(20)")]
        public string? 工单号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(100)")]
        public string? 用途名称 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(255)")]
        public string? 摘要 { get; set; }
        public decimal? 原价 { get; set; }
        public float? 折扣 { get; set; }
        public decimal? 发生金额 { get; set; }
        public decimal? 账户余额 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 网点 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(32)")]
        public string? 操作员 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 备注 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(32)")]
        public string? 收付款方式 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 收付款说明 { get; set; }
        public decimal? 赠送余额 { get; set; }
        public DateTime? 工单日期 { get; set; }
        public decimal? 赠送金额 { get; set; }
        public int? 扣款账号 { get; set; }
        public long? 时间戳 { get; set; }
    }


    public class 自动扣款设置
    {
        [SqlSugar.SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string Name { get; set; }= "自动扣款任务";
        [SugarColumn(ColumnDataType = "nvarchar(250)")]
        public string? DbCon { get; set; }
        public int SetDay { get; set; }
        public int SetHour { get; set; }
        public int SetMinute { get; set; }
        public int? ExecuteSet { get; set; }

        [SugarColumn(IsIgnore = true)]
        public int ExeHour
        {
            get => ExeTimeOnly.Hours;
            set => ExeTimeOnly = new(value, ExeTimeOnly.Minutes, ExeTimeOnly.Seconds);
        }
        [SugarColumn(IsIgnore = true)]
        public int ExeMinute
        {
            get => ExeTimeOnly.Minutes;
            set => ExeTimeOnly = new(ExeTimeOnly.Hours, value, ExeTimeOnly.Seconds);
        }
        [SugarColumn(IsIgnore = true)]
        public int ExeSecond
        {
            get => ExeTimeOnly.Seconds;
            set => ExeTimeOnly = new(ExeTimeOnly.Hours, ExeTimeOnly.Minutes, value);
        }
        [SugarColumn(IsIgnore = true)]
        public TimeSpan ExeTimeOnly { get; set; }
        public long ExeTime
        {
            get => ExeTimeOnly.Ticks;
            set => ExeTimeOnly = new(value);
        }
        public DateTime? EveryDayTime { get; set; }
        public bool Effective { get; set; }    //有效的
        public bool IsRunning { get; set; }    //运行
        public double? ExTimeSec { get; set; }  //下次运行间隔时间秒
        public DateTime? NextTime { get; set; } //下次运行时间
        public DateTime? LastTime { get; set; } //上次运行时间

    }

    public class 自动扣款时间设置
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(IsIgnore = true)]
        public TimeOnly ExeTimeOnly { get; set; }
        public long ExeTime
        {
            get => ExeTimeOnly.Ticks;
            set => ExeTimeOnly = new(value);
        }
    }


    //客户账户 是 充值账户 与 充值折扣表 合并查询
    public class 客户账户
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 客户ID { get; set; }
        public DateTime? 充值时间 { get; set; }
        public int? 使用优先级 { get; set; }
        public DateTime? 到期时间 { get; set; }
        public bool 是否有效 { get; set; }
        public decimal? 充值金额 { get; set; }
        public decimal? 剩余金额 { get; set; }
        public bool 是否为赠送 { get; set; }
        public int 折扣链接 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 网点 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(32)")]
        public string? 操作员 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(32)")]
        public string? 交易方式 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(50)")]
        public string? 说明 { get; set; }
        public int? 账单号 { get; set; }
        public int? 流水号 { get; set; }
        [SugarColumn(ColumnDataType = "nvarchar(10)")]
        public string? 有效期 { get; set; }
        public int? 客户编号 { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(16)")]
        public string 优惠方案名 { get; set; }= "默认";
        public float? CTP折扣率 { get; set; }
        public float? 菲林折扣率 { get; set; }
        public float? 数码折扣率 { get; set; }
        public float? 后加工折扣率 { get; set; }
        public float? 纯设计折扣率 { get; set; }
        public float? 其他折扣率1 { get; set; }
        public float? 其他折扣率2 { get; set; }
        public float? 其他折扣率3 { get; set; }
        public float? 折扣率 { get; set; }

    }



}
