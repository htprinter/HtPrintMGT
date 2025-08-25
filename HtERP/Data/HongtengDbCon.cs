using SqlSugar;


namespace HtERP.Data
{
    public class HongtengDbCon
    {
        public static DbType SetDBType(string? dbType) //设置数据库类型
        {
            if (Enum.TryParse(dbType, true, out DbType result))
                return result;
            return DbType.SqlServer;
        }
        //多库情况下使用说明：
        //如果是固定多库可以传 new SqlSugarScope(List<ConnectionConfig>,db=>{}) 文档：多租户
        //如果是不固定多库 可以看文档Saas分库
        //用单例模式
        public static SqlSugarScope Db = new(new ConnectionConfig()
        {
            ConnectionString = Program.ConnectionString,//连接符字串存在appsettings.json里
            DbType = SetDBType(Program.DbTypeSettings),//数据库类型

            IsAutoCloseConnection = true, //不设成true要手动close
            MoreSettings = new ConnMoreSettings
            {
                // IsCorrectErrorSqlParameterName = true, // 参数名有空格特殊符号使用兼容模式，对性能会有影响，能局部修改配置就局部，尽量不要全局使用
            },
            ConfigureExternalServices =
                {
                    SqlFuncServices = [
                        new()
                        {
                            //建一个NullIf的SQL函数
                            UniqueMethodName = "NullIf",
                            MethodValue = (expInfo, dbType, expContext) =>
                            {
                                if(dbType==DbType.SqlServer)
                                    return string.Format("NULLIF({0}, {1})", expInfo.Args[0].MemberName, expInfo.Args[1].MemberName);
                                else
                                    throw new Exception("未实现");
                            }
                        }
                    ]
                }
        },
            db =>
            {
                //(A)全局生效配置点，一般AOP和程序启动的配置扔这里面 ，所有上下文生效
                //调试SQL事件，可以删掉
                db.Aop.OnLogExecuting = (sql, pars) =>
                {

                    //获取原生SQL推荐 5.1.4.63  性能OK
                    Console.WriteLine(UtilMethods.GetNativeSql(sql, pars));

                    //获取无参数化SQL 对性能有影响，特别大的SQL参数多的，调试使用
                    //Console.WriteLine(UtilMethods.GetSqlString(DbType.SqlServer,sql,pars))
                };

                //多个配置就写下面
                //db.Ado.IsDisableMasterSlaveSeparation=true;

                //注意多租户 有几个设置几个
                //db.GetConnection(i).Aop
            });

        public static T? NullIf<T>(T a, T b) where T : struct
        {
            throw null!;
        }

    }


}
