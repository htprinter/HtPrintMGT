using System.Drawing;

namespace HtERP.Data
{
    public class CreateData
    {
        public string CreateDatabase()
        {
            //建库：如果不存在创建数据库存在不会重复创建
            HtTestdb.db.DbMaintenance.CreateDatabase();
            string aaa = "数据库已经创建,请初始化数据";
            return aaa;
        }
        public string InitializeData()
        {
            string aaa = "初始化未完成";
            if (HtTestdb.db.Ado.IsValidConnection())
            {
                // HtTestdb.db.CurrentConnectionConfig.MoreSettings.IsCorrectErrorSqlParameterName = true; //局部生效兼容模式
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("员工", false))
                {
                    //创建员工表
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(员工));
                    //插入超级管理员
                    HtTestdb.db.Insertable(new 员工() { 姓名 = "root", 手机 = "123456789", 密码 = "75e266f182b4fa3625d4a4f4f779af54", 住宅电话 = "123456", QQ号 = "12345678", 电子邮件 = "12@56.78", 地址 = "中国", 部门 = "总务", 头衔 = " ", 雇佣日期 = DateTime.Now, 身份证号 = " ", 是否为管理员 = true, 是否已离职 = false, 超级管理员 = true, 账户余额 = 88 }).ExecuteCommand();
                    //插入普通管理员
                    HtTestdb.db.Insertable(new 员工() { 姓名 = "admin", 手机 = "222222", 密码 = "75e266f182b4fa3625d4a4f4f779af54", 住宅电话 = "123456", QQ号 = "12345678", 电子邮件 = "12@56.78", 地址 = "中国", 部门 = "总务", 头衔 = " ", 雇佣日期 = DateTime.Now, 身份证号 = " ", 是否为管理员 = true, 是否已离职 = false, 超级管理员 = false, 账户余额 = 88 }).ExecuteCommand();
                    //插入普通员工
                    HtTestdb.db.Insertable(new 员工() { 姓名 = "user", 手机 = "333333", 密码 = "75e266f182b4fa3625d4a4f4f779af54", 住宅电话 = "123456", QQ号 = "12345678", 电子邮件 = "12@56.78", 地址 = "中国", 部门 = "设计制作", 头衔 = " ", 雇佣日期 = DateTime.Now, 身份证号 = " ", 是否为管理员 = false, 是否已离职 = false, 超级管理员 = false, 账户余额 = 66 }).ExecuteCommand();
                }
                //建表
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("收款账单")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(收款账单));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("工作表_数码印刷")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(工作表_数码印刷));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("工作表_CTP输出")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(工作表_CTP输出));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("工作表_菲林输出")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(工作表_菲林输出));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("工作表_设计制作")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(工作表_设计制作));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("工作表_后道加工")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(工作表_后道加工));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("工作表_彩喷写真")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(工作表_彩喷写真));

                if (!HtTestdb.db.DbMaintenance.IsAnyTable("纸张类型")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(纸张类型));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("客户表")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(客户表));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("CTP板材型号")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(CTP板材型号));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("CTP加网线数")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(CTP加网线数));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("CTP网点类型")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(CTP网点类型));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("CTP印刷机咬口要求")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(CTP印刷机咬口要求));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("数码印刷费率表")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(数码印刷费率表));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("公用设置"))
                {
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(公用设置));
                    HongtengDbCon.Db.Insertable(new 公用设置() { 菲林起步价 = 5, 菲林每平米价格=150, 资源费每张价格=2, 数码打样每平米价格=100, CTP每平米价格=50, 切割每米价格=(decimal)0.5, 默认折扣方案= "1" }).ExecuteReturnEntity();

                }
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("支出部门"))
                {
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(支出部门));
                    HongtengDbCon.Db.Insertable(new 支出部门() { 部门 = "总务"}).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出部门() { 部门 = "设计制作" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出部门() { 部门 = "菲林CTP" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出部门() { 部门 = "印刷打印" }).ExecuteReturnEntity();
                }
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("支出类别")) 
                {
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(支出类别));
                    HongtengDbCon.Db.Insertable(new 支出类别() { 类别 = "房租水电" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出类别() { 类别 = "工资奖金" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出类别() { 类别 = "日常办公" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出类别() { 类别 = "机器设备" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出类别() { 类别 = "打印耗材" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出类别() { 类别 = "纸张" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 支出类别() { 类别 = "菲林板材" }).ExecuteReturnEntity();
                }
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("支出总表")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(支出总表));

                if (!HtTestdb.db.DbMaintenance.IsAnyTable("充值账户")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(充值账户));
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("充值折扣表"))
                {
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(充值折扣表));
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 0,      CTP折扣率 = 1,     菲林折扣率 = 1,     数码折扣率 = 1,     后加工折扣率 = 1,     纯设计折扣率 = 1,     其他折扣率1 = 1,     其他折扣率2 = 1,     其他折扣率3 = 1, 折扣方案 = "1",    备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 100,    CTP折扣率 = 0.98f, 菲林折扣率 = 0.98f, 数码折扣率 = 0.98f, 后加工折扣率 = 0.98f, 纯设计折扣率 = 0.98f, 其他折扣率1 = 0.98f, 其他折扣率2 = 1,     其他折扣率3 = 1, 折扣方案 = "0.98", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 500,    CTP折扣率 = 0.95f, 菲林折扣率 = 0.95f, 数码折扣率 = 0.95f, 后加工折扣率 = 0.95f, 纯设计折扣率 = 0.95f, 其他折扣率1 = 0.95f, 其他折扣率2 = 0.98f, 其他折扣率3 = 1, 折扣方案 = "0.95", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 1000,   CTP折扣率 = 0.9f,  菲林折扣率 = 0.9f,  数码折扣率 = 0.9f,  后加工折扣率 = 0.9f,  纯设计折扣率 = 0.9f,  其他折扣率1 = 0.9f,  其他折扣率2 = 0.95f, 其他折扣率3 = 1, 折扣方案 = "0.9",  备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 2000,   CTP折扣率 = 0.88f, 菲林折扣率 = 0.88f, 数码折扣率 = 0.88f, 后加工折扣率 = 0.88f, 纯设计折扣率 = 0.88f, 其他折扣率1 = 0.88f, 其他折扣率2 = 0.9f,  其他折扣率3 = 1, 折扣方案 = "0.88", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 5000,   CTP折扣率 = 0.85f, 菲林折扣率 = 0.85f, 数码折扣率 = 0.85f, 后加工折扣率 = 0.85f, 纯设计折扣率 = 0.85f, 其他折扣率1 = 0.85f, 其他折扣率2 = 0.88f, 其他折扣率3 = 1, 折扣方案 = "0.85", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 10000,  CTP折扣率 = 0.8f,  菲林折扣率 = 0.8f,  数码折扣率 = 0.8f,  后加工折扣率 = 0.8f,  纯设计折扣率 = 0.8f,  其他折扣率1 = 0.8f,  其他折扣率2 = 0.85f, 其他折扣率3 = 1, 折扣方案 = "0.8",  备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 20000,  CTP折扣率 = 0.78f, 菲林折扣率 = 0.78f, 数码折扣率 = 0.78f, 后加工折扣率 = 0.78f, 纯设计折扣率 = 0.78f, 其他折扣率1 = 0.78f, 其他折扣率2 = 0.8f,  其他折扣率3 = 1, 折扣方案 = "0.78", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 50000,  CTP折扣率 = 0.75f, 菲林折扣率 = 0.75f, 数码折扣率 = 0.75f, 后加工折扣率 = 0.75f, 纯设计折扣率 = 0.75f, 其他折扣率1 = 0.75f, 其他折扣率2 = 0.78f, 其他折扣率3 = 1, 折扣方案 = "0.75", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 100000, CTP折扣率 = 0.7f,  菲林折扣率 = 0.7f,  数码折扣率 = 0.7f,  后加工折扣率 = 0.7f,  纯设计折扣率 = 0.7f,  其他折扣率1 = 0.7f,  其他折扣率2 = 0.75f, 其他折扣率3 = 1, 折扣方案 = "0.7",  备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 200000, CTP折扣率 = 0.68f, 菲林折扣率 = 0.68f, 数码折扣率 = 0.68f, 后加工折扣率 = 0.68f, 纯设计折扣率 = 0.68f, 其他折扣率1 = 0.68f, 其他折扣率2 = 0.7f,  其他折扣率3 = 1, 折扣方案 = "0.68", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣表() { 充值额 = 500000, CTP折扣率 = 0.65f, 菲林折扣率 = 0.65f, 数码折扣率 = 0.65f, 后加工折扣率 = 0.65f, 纯设计折扣率 = 0.65f, 其他折扣率1 = 0.65f, 其他折扣率2 = 0.68f, 其他折扣率3 = 1, 折扣方案 = "0.65", 备注 = "系统预设，请勿删除或改名" }).ExecuteReturnEntity();
                }

                if (!HtTestdb.db.DbMaintenance.IsAnyTable("充值折扣方案"))
                {
                    //建表
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(充值折扣方案));
                    //预设折扣方案
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_无折扣", CTP折扣率 = 1,    菲林折扣率 = 1,    数码折扣率 = 1,     后加工折扣率 = 1,     纯设计折扣率 = 1,     其他折扣率1 = 1,     其他折扣率2 = 1,     其他折扣率3 = 1, 使用有效期 = 36600, 备注 = "系统预设，请勿删除或改名，无折扣", 有效期描述 = "长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_100",   CTP折扣率 = 0.95f, 菲林折扣率 = 0.95f,数码折扣率 = 0.9f,  后加工折扣率 = 0.9f,  纯设计折扣率 = 0.98f, 其他折扣率1 = 1,     其他折扣率2 = 0.85f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值100以上", 有效期描述="长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_500",   CTP折扣率 = 0.9f,  菲林折扣率 = 0.9f, 数码折扣率 = 0.85f, 后加工折扣率 = 0.88f, 纯设计折扣率 = 0.95f, 其他折扣率1 = 0.98f, 其他折扣率2 = 0.80f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值500以上", 有效期描述="长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_1千",   CTP折扣率 = 0.85f, 菲林折扣率 = 0.85f,数码折扣率 = 0.80f, 后加工折扣率 = 0.85f, 纯设计折扣率 = 0.90f, 其他折扣率1 = 0.95f, 其他折扣率2 = 0.75f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值1千以上", 有效期描述="长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_2千",   CTP折扣率 = 0.8f,  菲林折扣率 = 0.8f, 数码折扣率 = 0.75f, 后加工折扣率 = 0.82f, 纯设计折扣率 = 0.88f, 其他折扣率1 = 0.90f, 其他折扣率2 = 0.70f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值2千以上", 有效期描述="长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_5千",   CTP折扣率 = 0.75f, 菲林折扣率 = 0.75f,数码折扣率 = 0.70f, 后加工折扣率 = 0.80f, 纯设计折扣率 = 0.85f, 其他折扣率1 = 0.88f, 其他折扣率2 = 0.65f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值5千以上", 有效期描述="长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_1万",   CTP折扣率 = 0.7f,  菲林折扣率 = 0.7f, 数码折扣率 = 0.65f, 后加工折扣率 = 0.78f, 纯设计折扣率 = 0.80f, 其他折扣率1 = 0.85f, 其他折扣率2 = 0.60f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值1万以上", 有效期描述="长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_2万",   CTP折扣率 = 0.65f, 菲林折扣率 = 0.68f,数码折扣率 = 0.60f, 后加工折扣率 = 0.75f, 纯设计折扣率 = 0.78f, 其他折扣率1 = 0.80f, 其他折扣率2 = 0.55f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值2万以上", 有效期描述="长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_5万",   CTP折扣率 = 0.60f, 菲林折扣率 = 0.65f,数码折扣率 = 0.55f, 后加工折扣率 = 0.72f, 纯设计折扣率 = 0.75f, 其他折扣率1 = 0.78f, 其他折扣率2 = 0.50f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值5万以上", 有效期描述 = "长期" }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 充值折扣方案() { 优惠方案名 = "预设_10万",  CTP折扣率 = 0.58f, 菲林折扣率 = 0.60f,数码折扣率 = 0.5f,  后加工折扣率 = 0.7f,  纯设计折扣率 = 0.7f,  其他折扣率1 = 0.75f, 其他折扣率2 = 0.48f, 其他折扣率3 = 1, 使用有效期 = 3660,  备注 = "系统预设，请勿删除或改名，充值10万以上", 有效期描述 = "长期" }).ExecuteReturnEntity();
                }

                if (!HtTestdb.db.DbMaintenance.IsAnyTable("交易流水表")) HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(交易流水表));

                //自动扣款设置建表
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("自动扣款设置"))
                {
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(自动扣款设置));
                    HongtengDbCon.Db.Insertable(new 自动扣款设置() { Name = "默认设置", SetDay = 2, SetHour = 0, SetMinute = 0, ExecuteSet = 3, ExeTimeOnly = new(8, 0, 0), EveryDayTime = new DateTime(2025, 1, 1, 02, 08, 08), Effective = true, IsRunning = false, ExTimeSec = 3600 }).ExecuteReturnEntity();

                }
                if (!HtTestdb.db.DbMaintenance.IsAnyTable("自动扣款时间设置"))
                {
                    HtTestdb.db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(自动扣款时间设置));
                    HongtengDbCon.Db.Insertable(new 自动扣款时间设置() { ExeTimeOnly = new(01, 08, 08) }).ExecuteReturnEntity();
                    HongtengDbCon.Db.Insertable(new 自动扣款时间设置() { ExeTimeOnly = new(12, 08, 08) }).ExecuteReturnEntity();
                }
                





                aaa = "数据初始化完成";
            }
            else
            {
                aaa = "请先创建数据库，再初始化数据";
            }
            return aaa;
        }
    }
}
