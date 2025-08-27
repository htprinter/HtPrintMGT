using HtERP.Data;
using SqlSugar;

namespace HtERP.Services
{
    /// <summary>
    /// 旧版自动扣款服务，扣款时没有写入时间戳
    /// </summary>  
    public class OldAutoDebit
    {

        /// <summary>    
        /// 自动扣款，参数为结算多少时间以前的工单：int天、int时、int分。
        /// </summary>
        public static void Payment(int day, int hour, int minut)
        {
            //列名有空格等符号的兼容模式
            //HongtengDbCon.Db.CurrentConnectionConfig.MoreSettings.IsCorrectErrorSqlParameterName = true;

            //读取结算多久时间前的单子
            DateTime beforTime = DateTime.Now.AddDays(-day).AddHours(-hour).AddMinutes(-minut);
            DateTime beforTime2 = beforTime.AddDays(-day - 7);

            //初始化实收金额
            decimal ss = 0;    //实收额
            decimal sk = 0;    //应收额
            decimal zk = 1;    //扣款率

            //------------------------写入收款单-取得单号------------------------
            var ddd = HongtengDbCon.Db.Insertable(new 收款账单() { 日期 = DateTime.Now, 操作员 = "自动扣款", 客户ID = "VIP客户集中扣款", 收款部门 = "系统后台", 付款类型 = "账户扣款", 有效 = false, 账单状态 = "已扣款", 备注 = "此账单是集中扣款时自动生成，不记录金额。" }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

            //-------------------------数码印刷--------------------------
            //连表查询出VIP客户未结清的条目
            var 数码VIP = HongtengDbCon.Db.Queryable<工作表_数码印刷>()
                         .LeftJoin<客户表>((it, cus) => it.客户 == cus.客户ID)//多个条件用&&
                         .Where((it, cus) => it.结清 == false && cus.VIP客户 == true)
                         .Where((it, cus) => it.输出日期 < beforTime)
                         .Select((it, cus) => new dbModel { 分类 = "印刷", ID = it.编号, 日期 = it.输出日期, 客户 = it.客户, 品名 = it.文件或工作名, 长 = it.打印长, 宽 = it.打印宽, 数量 = it.张数, 规格 = it.纸张类型, 价格 = it.应收, 要求说明 = it.要求及文件位置, 制作员1 = it.制作员1, 送货地点 = it.送货地点, 备注 = it.备注, 完成 = it.已经发送, 附加设计费 = it.设计制作费, 主费用 = it.印刷费, 其他费用 = it.其他费用, 结清 = it.结清, 实收 = it.实收, 实收设计费 = it.实收设计费, 实收CTP费 = it.实收印制费, 客户编号 = cus.自动编号 })
                         //.OrderBy(it => it.ID)
                         .ToList();

            foreach (var view in 数码VIP)
            {

                if (view.日期 > beforTime2 && (view.完成 == false || view.完成 == null))
                {
                    continue; // 日期大于设定值，不进行扣款，跳到下一次循环迭代
                }

                //连表查询出客户充值账户以及对应折扣率
                var 客户账户 = HongtengDbCon.Db.Queryable<充值账户>()
                             .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                             .Where((it, cus) => it.客户编号 == view.客户编号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                             .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.数码折扣率 })
                             .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                             .ToList();
                double 折算总余额 = 0;
                double 总余额 = 0;

                if (!客户账户.Any())
                {
                    continue; // 客户没有可供账户，跳到下一次循环迭代
                }

                foreach (var item in 客户账户)
                {
                    折算总余额 = (double)(折算总余额 + (double)(item.剩余金额 ?? 0) / (item.折扣率 ?? 1));
                    总余额 = 总余额 + (double)(item.剩余金额 ?? 0);
                }

                //检查余额是否足够
                if ((view.价格 ?? 0) > (decimal)总余额 && view.已优惠 == true || (view.价格 ?? 0) > (decimal)折算总余额)
                {
                    continue; // 余额不够，不进行扣款，跳到下一次循环迭代
                }

                //初始化实收金额
                ss = 0;                  //实收额
                sk = view.价格 ?? 0;     //应收额
                zk = 1;                  //扣款率

                //循环扣款
                foreach (var item in 客户账户)
                {
                    if (view.已优惠 == true)
                    {
                        zk = 1;
                    }
                    else
                    {
                        zk = (decimal)(item.折扣率 ?? 1);
                    }

                    if (sk * zk <= item.剩余金额)
                    {
                        item.剩余金额 = item.剩余金额 - sk * zk;
                        总余额 = (double)(总余额 - (double)(sk * zk));
                        //计算实收金额
                        ss = ss + sk * zk;

                        //生成摘要
                        string zy = view.数量.ToString() + "印面" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "快印扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        //计算各项实收比例
                        decimal feeRatio = 0; //设计劳务费比例
                        decimal costRatio = 1; //产品费比例
                        decimal designFee = view.附加设计费 ?? 0;
                        decimal productFee = view.主费用 ?? 0;
                        decimal otherFee = view.其他费用 ?? 0;
                        if (designFee + productFee + otherFee != 0)
                        {
                            feeRatio = (view.附加设计费 ?? 0) / (designFee + productFee + otherFee);
                            costRatio = (view.主费用 ?? 0) / (designFee + productFee + otherFee);
                        }
                        view.结清 = true;
                        view.实收 = ss;
                        view.收款单号 = ddd;
                        view.结算号码 = jyid;
                        view.实收设计费 = ss * feeRatio;
                        view.实收CTP费 = ss * costRatio;
                        view.结算状态 = "结算并扣款";

                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新更新工作表收款
                        HongtengDbCon.Db.Updateable<工作表_数码印刷>()
                                .SetColumns(it => new 工作表_数码印刷() { 结清 = view.结清, 实收 = view.实收, 收款单号 = view.收款单号, 结算号码 = view.结算号码, 实收设计费 = view.实收设计费, 实收印制费 = view.实收CTP费, 结算状态 = view.结算状态 })
                                .Where(it => it.编号 == view.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();
                        break; // 退出扣款循环
                    }
                    else
                    {
                        //计算此处扣款后剩余金额
                        sk = (decimal)(sk - (item.剩余金额 ?? 0) / zk);

                        总余额 = 总余额 - (double)(item.剩余金额 ?? 0);

                        //计算实收金额
                        ss = (decimal)(ss + (item.剩余金额 ?? 0));

                        //生成摘要
                        string zy = view.数量.ToString() + "印面" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "快印扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        item.剩余金额 = 0;
                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();

                    }

                }

            }

            //-------------------------工作表_后道加工--------------------------
            //连表查询出VIP客户未结清的条目
            var 后道VIP = HongtengDbCon.Db.Queryable<工作表_后道加工>()
                         .LeftJoin<客户表>((it, cus) => it.客户 == cus.客户ID)//多个条件用&&
                         .Where((it, cus) => it.结清 == false && cus.VIP客户 == true)
                         .Where((it, cus) => it.日期 < beforTime)
                         .Select((it, cus) => new dbModel { 分类 = "后道", ID = it.编号, 日期 = it.日期, 客户 = it.客户, 品名 = it.文件或工作名, 长 = it.长, 宽 = it.宽, 数量 = it.数量, 规格 = it.输出设备, 价格 = it.应收, 要求说明 = it.要求, 制作员1 = it.制作员1, 送货地点 = it.送货地点, 备注 = it.备注, 完成 = it.已经发送, 附加设计费 = it.设计制作费, 主费用 = it.加工费, 其他费用 = it.其他费用, 结清 = it.结清, 实收 = it.实收, 实收设计费 = it.实收设计费, 实收CTP费 = it.实收加工费, 客户编号 = cus.自动编号 })
                         //.OrderBy(it => it.ID)
                         .ToList();

            foreach (var view in 后道VIP)
            {

                if (view.日期 > beforTime2 && (view.完成 == false || view.完成 == null))
                {
                    continue; // 日期大于设定值，不进行扣款，跳到下一次循环迭代
                }

                //连表查询出客户充值账户以及对应折扣率
                var 客户账户 = HongtengDbCon.Db.Queryable<充值账户>()
                             .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                             .Where((it, cus) => it.客户编号 == view.客户编号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                             .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.后加工折扣率 })
                             .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                             .ToList();
                double 折算总余额 = 0;
                double 总余额 = 0;

                if (!客户账户.Any())
                {
                    continue; // 客户没有可供账户，跳到下一次循环迭代
                }
                foreach (var item in 客户账户)
                {
                    折算总余额 = (double)(折算总余额 + (double)(item.剩余金额 ?? 0) / (item.折扣率 ?? 1));
                    总余额 = 总余额 + (double)(item.剩余金额 ?? 0);
                }

                //检查余额是否足够
                if ((double)(view.价格 ?? 0) > 总余额 && view.已优惠 == true || (double)(view.价格 ?? 0) > 折算总余额)
                {
                    continue; // 余额不够，不进行扣款，跳到下一次循环迭代
                }

                //初始化实收金额
                ss = 0;                  //实收额
                sk = view.价格 ?? 0;     //应收额
                zk = 1;                  //扣款率

                //循环扣款
                foreach (var item in 客户账户)
                {
                    if (view.已优惠 == true)
                    {
                        zk = 1;
                    }
                    else
                    {
                        zk = (decimal)(item.折扣率 ?? 1);
                    }

                    if (sk * zk <= item.剩余金额)
                    {
                        item.剩余金额 = item.剩余金额 - sk * zk;
                        总余额 = (double)(总余额 - (double)(sk * zk));
                        //计算实收金额
                        ss = ss + sk * zk;

                        //生成摘要
                        string zy = view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "后道扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        //计算各项实收比例
                        decimal feeRatio = 0; //设计劳务费比例
                        decimal costRatio = 1; //产品费比例
                        decimal designFee = view.附加设计费 ?? 0;
                        decimal productFee = view.主费用 ?? 0;
                        decimal otherFee = view.其他费用 ?? 0;
                        if (designFee + productFee + otherFee != 0)
                        {
                            feeRatio = (view.附加设计费 ?? 0) / (designFee + productFee + otherFee);
                            costRatio = (view.主费用 ?? 0) / (designFee + productFee + otherFee);
                        }
                        view.结清 = true;
                        view.实收 = ss;
                        view.收款单号 = ddd;
                        view.结算号码 = jyid;
                        view.实收设计费 = ss * feeRatio;
                        view.实收CTP费 = ss * costRatio;
                        view.结算状态 = "结算并扣款";

                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新更新工作表收款
                        HongtengDbCon.Db.Updateable<工作表_后道加工>()
                                .SetColumns(it => new 工作表_后道加工() { 结清 = view.结清, 实收 = view.实收, 收款单号 = view.收款单号, 结算号码 = view.结算号码, 实收设计费 = view.实收设计费, 实收加工费 = view.实收CTP费, 结算状态 = view.结算状态 })
                                .Where(it => it.编号 == view.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();
                        break; // 退出扣款循环
                    }
                    else
                    {
                        //计算此处扣款后剩余金额
                        sk = (decimal)(sk - (item.剩余金额 ?? 0) / zk);

                        总余额 = 总余额 - (double)(item.剩余金额 ?? 0);

                        //计算实收金额
                        ss = (decimal)(ss + (item.剩余金额 ?? 0));


                        //生成摘要
                        string zy = view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "后道扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        item.剩余金额 = 0;
                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();

                    }

                }

            }

            //-------------------------CTP输出--------------------------
            //连表查询出VIP客户未结清的条目
            var CTPVIP = HongtengDbCon.Db.Queryable<工作表_CTP输出>()
                         .LeftJoin<客户表>((it, cus) => it.客户 == cus.客户ID)//多个条件用&&
                         .Where((it, cus) => it.结清 == false && cus.VIP客户 == true)
                         .Where((it, cus) => it.输出日期 < beforTime)
                         .Select((it, cus) => new dbModel { 分类 = "CTP", ID = it.编号, 日期 = it.输出日期, 客户 = it.客户, 品名 = it.文件或工作名, 长 = it.长, 宽 = it.宽, 数量 = it.总色数, 规格 = it.CTP板材型号, 价格 = it.应收, 要求说明 = it.输出要求, 制作员1 = it.制作员1, 送货地点 = it.送货地点, 备注 = it.备注, 完成 = it.已输出, 附加设计费 = it.设计制作费, 主费用 = it.版费, 其他费用 = it.其他费用, 结清 = it.结清, 实收 = it.实收, 实收设计费 = it.实收设计费, 实收CTP费 = it.实收CTP费, 客户编号 = cus.自动编号 })
                         //.OrderBy(it => it.ID)
                         .ToList();

            foreach (var view in CTPVIP)
            {

                if (view.日期 > beforTime2 && (view.完成 == false || view.完成 == null))
                {
                    continue; // 日期大于设定值，不进行扣款，跳到下一次循环迭代
                }

                //连表查询出客户充值账户以及对应折扣率
                var 客户账户 = HongtengDbCon.Db.Queryable<充值账户>()
                             .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                             .Where((it, cus) => it.客户编号 == view.客户编号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                             .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.CTP折扣率 })
                             .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                             .ToList();
                double 折算总余额 = 0;
                double 总余额 = 0;

                if (!客户账户.Any())
                {
                    continue; // 客户没有可供账户，跳到下一次循环迭代
                }
                foreach (var item in 客户账户)
                {
                    折算总余额 = (double)(折算总余额 + (double)(item.剩余金额 ?? 0) / (item.折扣率 ?? 1));
                    总余额 = 总余额 + (double)(item.剩余金额 ?? 0);
                }

                //检查余额是否足够
                if ((double)(view.价格 ?? 0) > 总余额 && view.已优惠 == true || (double)(view.价格 ?? 0) > 折算总余额)
                {
                    continue; // 余额不够，不进行扣款，跳到下一次循环迭代
                }

                //初始化实收金额
                ss = 0;                  //实收额
                sk = view.价格 ?? 0;     //应收额
                zk = 1;                  //扣款率

                //循环扣款
                foreach (var item in 客户账户)
                {
                    if (view.已优惠 == true)
                    {
                        zk = 1;
                    }
                    else
                    {
                        zk = (decimal)(item.折扣率 ?? 1);
                    }

                    if (sk * zk <= item.剩余金额)
                    {
                        item.剩余金额 = item.剩余金额 - sk * zk;
                        总余额 = (double)(总余额 - (double)(sk * zk));
                        //计算实收金额
                        ss = ss + sk * zk;

                        //生成摘要
                        string zy = view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "CTP扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        //计算各项实收比例
                        decimal feeRatio = 0; //设计劳务费比例
                        decimal costRatio = 1; //产品费比例
                        decimal designFee = view.附加设计费 ?? 0;
                        decimal productFee = view.主费用 ?? 0;
                        decimal otherFee = view.其他费用 ?? 0;
                        if (designFee + productFee + otherFee != 0)
                        {
                            feeRatio = (view.附加设计费 ?? 0) / (designFee + productFee + otherFee);
                            costRatio = (view.主费用 ?? 0) / (designFee + productFee + otherFee);
                        }
                        view.结清 = true;
                        view.实收 = ss;
                        view.收款单号 = ddd;
                        view.结算号码 = jyid;
                        view.实收设计费 = ss * feeRatio;
                        view.实收CTP费 = ss * costRatio;
                        view.结算状态 = "结算并扣款";

                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新更新工作表收款
                        HongtengDbCon.Db.Updateable<工作表_CTP输出>()
                                .SetColumns(it => new 工作表_CTP输出() { 结清 = view.结清, 实收 = view.实收, 收款单号 = view.收款单号, 结算号码 = view.结算号码, 实收设计费 = view.实收设计费, 实收CTP费 = view.实收CTP费, 结算状态 = view.结算状态 })
                                .Where(it => it.编号 == view.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();
                        break; // 退出扣款循环
                    }
                    else
                    {
                        //计算此处扣款后剩余金额
                        sk = (decimal)(sk - (item.剩余金额 ?? 0) / zk);

                        总余额 = 总余额 - (double)(item.剩余金额 ?? 0);

                        //计算实收金额
                        ss = (decimal)(ss + (item.剩余金额 ?? 0));

                        //生成摘要
                        string zy = view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "CTP扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        item.剩余金额 = 0;
                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();

                    }

                }

            }

            //-------------------------菲林输出--------------------------
            //连表查询出VIP客户未结清的条目
            var 菲林VIP = HongtengDbCon.Db.Queryable<工作表_菲林输出>()
                         .LeftJoin<客户表>((it, cus) => it.客户 == cus.客户ID)//多个条件用&&
                         .Where((it, cus) => it.结清 == false && cus.VIP客户 == true)
                         .Where((it, cus) => it.输出日期 < beforTime)
                         .Select((it, cus) => new dbModel { 分类 = "菲林", ID = it.编号, 日期 = it.输出日期, 客户 = it.客户, 品名 = it.文件或工作名, 长 = it.长, 宽 = it.宽, 数量 = it.总色数, 规格 = it.输出设备, 价格 = it.应收, 要求说明 = it.输出要求, 制作员1 = it.制作员1, 送货地点 = it.送货地点, 备注 = it.备注, 完成 = it.已完成, 附加设计费 = it.设计制作费, 主费用 = it.菲林费, 其他费用 = it.其他费用, 结清 = it.结清, 实收 = it.实收, 实收设计费 = it.实收设计费, 实收CTP费 = it.实收菲林费, 客户编号 = cus.自动编号 })
                         //.OrderBy(it => it.ID)
                         .ToList();

            foreach (var view in 菲林VIP)
            {

                if (view.日期 > beforTime2 && (view.完成 == false || view.完成 == null))
                {
                    continue; // 日期大于设定值，不进行扣款，跳到下一次循环迭代
                }

                //连表查询出客户充值账户以及对应折扣率
                var 客户账户 = HongtengDbCon.Db.Queryable<充值账户>()
                             .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                             .Where((it, cus) => it.客户编号 == view.客户编号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                             .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.菲林折扣率 })
                             .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                             .ToList();
                double 折算总余额 = 0;
                double 总余额 = 0;

                if (!客户账户.Any())
                {
                    continue; // 客户没有可供账户，跳到下一次循环迭代
                }
                foreach (var item in 客户账户)
                {
                    折算总余额 = (double)(折算总余额 + (double)(item.剩余金额 ?? 0) / (item.折扣率 ?? 1));
                    总余额 = 总余额 + (double)(item.剩余金额 ?? 0);
                }

                //检查余额是否足够
                if ((double)(view.价格 ?? 0) > 总余额 && view.已优惠 == true || (double)(view.价格 ?? 0) > 折算总余额)
                {
                    continue; // 余额不够，不进行扣款，跳到下一次循环迭代
                }

                //初始化实收金额
                ss = 0;                  //实收额
                sk = view.价格 ?? 0;     //应收额
                zk = 1;                  //扣款率

                //循环扣款
                foreach (var item in 客户账户)
                {
                    if (view.已优惠 == true)
                    {
                        zk = 1;
                    }
                    else
                    {
                        zk = (decimal)(item.折扣率 ?? 1);
                    }

                    if (sk * zk <= item.剩余金额)
                    {
                        item.剩余金额 = item.剩余金额 - sk * zk;
                        总余额 = (double)(总余额 - (double)(sk * zk));
                        //计算实收金额
                        ss = ss + sk * zk;

                        //生成摘要
                        string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "菲林扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        //计算各项实收比例
                        decimal feeRatio = 0; //设计劳务费比例
                        decimal costRatio = 1; //产品费比例
                        decimal designFee = view.附加设计费 ?? 0;
                        decimal productFee = view.主费用 ?? 0;
                        decimal otherFee = view.其他费用 ?? 0;
                        if (designFee + productFee + otherFee != 0)
                        {
                            feeRatio = (view.附加设计费 ?? 0) / (designFee + productFee + otherFee);
                            costRatio = (view.主费用 ?? 0) / (designFee + productFee + otherFee);
                        }
                        view.结清 = true;
                        view.实收 = ss;
                        view.收款单号 = ddd;
                        view.结算号码 = jyid;
                        view.实收设计费 = ss * feeRatio;
                        view.实收CTP费 = ss * costRatio;
                        view.结算状态 = "结算并扣款";

                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新更新工作表收款
                        HongtengDbCon.Db.Updateable<工作表_菲林输出>()
                                .SetColumns(it => new 工作表_菲林输出() { 结清 = view.结清, 实收 = view.实收, 收款单号 = view.收款单号, 结算号码 = view.结算号码, 实收设计费 = view.实收设计费, 实收菲林费 = view.实收CTP费, 结算状态 = view.结算状态 })
                                .Where(it => it.编号 == view.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();
                        break; // 退出扣款循环
                    }
                    else
                    {
                        //计算此处扣款后剩余金额
                        sk = (decimal)(sk - (item.剩余金额 ?? 0) / zk);

                        总余额 = 总余额 - (double)(item.剩余金额 ?? 0);

                        //计算实收金额
                        ss = (decimal)(ss + (item.剩余金额 ?? 0));

                        //生成摘要
                        string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "菲林扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        item.剩余金额 = 0;
                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();

                    }

                }

            }

            //-------------------------彩喷写真--------------------------
            //连表查询出VIP客户未结清的条目
            var 彩喷VIP = HongtengDbCon.Db.Queryable<工作表_彩喷写真>()
                         .LeftJoin<客户表>((it, cus) => it.客户 == cus.客户ID)//多个条件用&&
                         .Where((it, cus) => it.结清 == false && cus.VIP客户 == true)
                         .Where((it, cus) => it.输出日期 < beforTime)
                         .Select((it, cus) => new dbModel { 分类 = "彩喷", ID = it.编号, 日期 = it.输出日期, 客户 = it.客户, 品名 = it.文件或工作名, 长 = it.打印长, 宽 = it.打印宽, 数量 = it.张数, 规格 = it.规格, 价格 = it.应收, 要求说明 = it.要求及文件位置, 制作员1 = it.制作员1, 送货地点 = it.送货地点, 备注 = it.备注, 完成 = it.已经发送, 附加设计费 = it.设计制作费, 主费用 = it.价格, 其他费用 = it.其他费用, 结清 = it.结清, 实收 = it.实收, 实收设计费 = it.实收设计费, 实收CTP费 = it.实收打印费, 客户编号 = cus.自动编号 })
                         //.OrderBy(it => it.ID)
                         .ToList();

            foreach (var view in 彩喷VIP)
            {

                if (view.日期 > beforTime2 && (view.完成 == false || view.完成 == null))
                {
                    continue; // 日期大于设定值，不进行扣款，跳到下一次循环迭代
                }

                //连表查询出客户充值账户以及对应折扣率
                var 客户账户 = HongtengDbCon.Db.Queryable<充值账户>()
                             .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                             .Where((it, cus) => it.客户编号 == view.客户编号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                             .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.数码折扣率 })
                             .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                             .ToList();
                double 折算总余额 = 0;
                double 总余额 = 0;

                if (!客户账户.Any())
                {
                    continue; // 客户没有可供账户，跳到下一次循环迭代
                }

                foreach (var item in 客户账户)
                {
                    折算总余额 = (double)(折算总余额 + (double)(item.剩余金额 ?? 0) / (item.折扣率 ?? 1));
                    总余额 = 总余额 + (double)(item.剩余金额 ?? 0);
                }

                //检查余额是否足够
                if ((double)(view.价格 ?? 0) > 总余额 && view.已优惠 == true || (double)(view.价格 ?? 0) > 折算总余额)
                {
                    continue; // 余额不够，不进行扣款，跳到下一次循环迭代
                }

                //初始化实收金额
                ss = 0;                  //实收额
                sk = view.价格 ?? 0;     //应收额
                zk = 1;                  //扣款率

                //循环扣款
                foreach (var item in 客户账户)
                {
                    if (view.已优惠 == true)
                    {
                        zk = 1;
                    }
                    else
                    {
                        zk = (decimal)(item.折扣率 ?? 1);
                    }

                    if (sk * zk <= item.剩余金额)
                    {
                        item.剩余金额 = item.剩余金额 - sk * zk;
                        总余额 = (double)(总余额 - (double)(sk * zk));
                        //计算实收金额
                        ss = ss + sk * zk;

                        //生成摘要
                        string zy = view.数量.ToString() + "印面" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "彩喷扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        //计算各项实收比例
                        decimal feeRatio = 0; //设计劳务费比例
                        decimal costRatio = 1; //产品费比例
                        decimal designFee = view.附加设计费 ?? 0;
                        decimal productFee = view.主费用 ?? 0;
                        decimal otherFee = view.其他费用 ?? 0;
                        if (designFee + productFee + otherFee != 0)
                        {
                            feeRatio = (view.附加设计费 ?? 0) / (designFee + productFee + otherFee);
                            costRatio = (view.主费用 ?? 0) / (designFee + productFee + otherFee);
                        }
                        view.结清 = true;
                        view.实收 = ss;
                        view.收款单号 = ddd;
                        view.结算号码 = jyid;
                        view.实收设计费 = ss * feeRatio;
                        view.实收CTP费 = ss * costRatio;
                        view.结算状态 = "结算并扣款";

                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新更新工作表收款
                        HongtengDbCon.Db.Updateable<工作表_彩喷写真>()
                                .SetColumns(it => new 工作表_彩喷写真() { 结清 = view.结清, 实收 = view.实收, 收款单号 = view.收款单号, 结算号码 = view.结算号码, 实收设计费 = view.实收设计费, 实收打印费 = view.实收CTP费, 结算状态 = view.结算状态 })
                                .Where(it => it.编号 == view.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();
                        break; // 退出扣款循环
                    }
                    else
                    {
                        //计算此处扣款后剩余金额
                        sk = (decimal)(sk - (item.剩余金额 ?? 0) / zk);

                        总余额 = 总余额 - (double)(item.剩余金额 ?? 0);

                        //计算实收金额
                        ss = (decimal)(ss + (item.剩余金额 ?? 0));

                        //生成摘要
                        string zy = view.数量.ToString() + "印面" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "彩喷扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        item.剩余金额 = 0;
                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();

                    }

                }

            }

            //-------------------------设计制作--------------------------
            //连表查询出VIP客户未结清的条目
            var 设计VIP = HongtengDbCon.Db.Queryable<工作表_设计制作>()
                         .LeftJoin<客户表>((it, cus) => it.客户 == cus.客户ID)//多个条件用&&
                         .Where((it, cus) => it.结清 == false && cus.VIP客户 == true)
                         .Where((it, cus) => it.日期 < beforTime)
                         .Select((it, cus) => new dbModel { 分类 = "设计", ID = it.编号, 日期 = it.日期, 客户 = it.客户, 品名 = it.文件或工作名, 长 = it.长, 宽 = it.宽, 数量 = it.总色数, 规格 = it.发送至, 价格 = it.应收, 要求说明 = it.输出要求, 制作员1 = it.制作员1, 送货地点 = it.送货地点, 备注 = it.备注, 完成 = it.已完成, 附加设计费 = it.设计制作费, 主费用 = it.施工费, 其他费用 = it.其他费用, 结清 = it.结清, 实收 = it.实收, 实收设计费 = it.实收设计费, 实收CTP费 = it.实收施工费, 客户编号 = cus.自动编号 })
                         //.OrderBy(it => it.ID)
                         .ToList();

            foreach (var view in 设计VIP)
            {

                if (view.日期 > beforTime2 && (view.完成 == false || view.完成 == null))
                {
                    continue; // 日期大于设定值，不进行扣款，跳到下一次循环迭代
                }

                //连表查询出客户充值账户以及对应折扣率
                var 客户账户 = HongtengDbCon.Db.Queryable<充值账户>()
                             .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                             .Where((it, cus) => it.客户编号 == view.客户编号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                             .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.纯设计折扣率 })
                             .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                             .ToList();
                double 折算总余额 = 0;
                double 总余额 = 0;

                if (!客户账户.Any())
                {
                    continue; // 客户没有可供账户，跳到下一次循环迭代
                }
                foreach (var item in 客户账户)
                {
                    折算总余额 = (double)(折算总余额 + (double)(item.剩余金额 ?? 0) / (item.折扣率 ?? 1));
                    总余额 = 总余额 + (double)(item.剩余金额 ?? 0);
                }

                //检查余额是否足够
                if ((double)(view.价格 ?? 0) > 总余额 && view.已优惠 == true || (double)(view.价格 ?? 0) > 折算总余额)
                {
                    continue; // 余额不够，不进行扣款，跳到下一次循环迭代
                }

                //初始化实收金额
                ss = 0;                  //实收额
                sk = view.价格 ?? 0;     //应收额
                zk = 1;                  //扣款率

                //循环扣款
                foreach (var item in 客户账户)
                {
                    if (view.已优惠 == true)
                    {
                        zk = 1;
                    }
                    else
                    {
                        zk = (decimal)(item.折扣率 ?? 1);
                    }

                    if (sk * zk <= item.剩余金额)
                    {
                        item.剩余金额 = item.剩余金额 - sk * zk;
                        总余额 = (double)(总余额 - (double)(sk * zk));
                        //计算实收金额
                        ss = ss + sk * zk;

                        //生成摘要
                        string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "设计扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        //计算各项实收比例
                        decimal feeRatio = 0; //设计劳务费比例
                        decimal costRatio = 1; //产品费比例
                        decimal designFee = view.附加设计费 ?? 0;
                        decimal productFee = view.主费用 ?? 0;
                        decimal otherFee = view.其他费用 ?? 0;
                        if (designFee + productFee + otherFee != 0)
                        {
                            feeRatio = (view.附加设计费 ?? 0) / (designFee + productFee + otherFee);
                            costRatio = (view.主费用 ?? 0) / (designFee + productFee + otherFee);
                        }
                        view.结清 = true;
                        view.实收 = ss;
                        view.收款单号 = ddd;
                        view.结算号码 = jyid;
                        view.实收设计费 = ss * feeRatio;
                        view.实收CTP费 = ss * costRatio;
                        view.结算状态 = "结算并扣款";

                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新更新工作表收款
                        HongtengDbCon.Db.Updateable<工作表_设计制作>()
                                .SetColumns(it => new 工作表_设计制作() { 结清 = view.结清, 实收 = view.实收, 收款单号 = view.收款单号, 结算号码 = view.结算号码, 实收设计费 = view.实收设计费, 实收施工费 = view.实收CTP费, 结算状态 = view.结算状态 })
                                .Where(it => it.编号 == view.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();
                        break; // 退出扣款循环
                    }
                    else
                    {
                        //计算此处扣款后剩余金额
                        sk = (decimal)(sk - (item.剩余金额 ?? 0) / zk);

                        总余额 = 总余额 - (double)(item.剩余金额 ?? 0);

                        //计算实收金额
                        ss = (decimal)(ss + (item.剩余金额 ?? 0));

                        //生成摘要
                        string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.数量.ToString() + "张" + view.规格 + "，" + view.要求说明;
                        //----------------------写入交易流水表-----------------------
                        var jyid = HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "设计扣款", 工单号 = view.ID.ToString(), 用途名称 = view.品名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = "系统后台", 操作员 = "自动扣款", 原价 = view.价格, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentity();

                        item.剩余金额 = 0;
                        //更新充值账户剩余金额
                        HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommand();
                        //更新客户表账户余额
                        HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommand();

                    }

                }

            }


        }

        //--------------------------------单条扣款--------------------------------
        /// <summary>    
        /// 单条扣款方法，参数：string分类、int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> DebitOne(string inf, int p, string bm, string yg)
        {
            if (inf == "CTP")
            {
                bool t = false;
                t = await CTP输出扣款(p, bm, yg);
                return t; // 如果扣款成功，返回true
            }
            else if (inf == "菲林")
            {
                bool t = false;
                t = await 菲林输出扣款(p, bm, yg);
                return t; // 如果扣款成功，返回true

            }
            else if (inf == "彩喷")
            {
                bool t = false;
                t = await 彩喷写真扣款(p, bm, yg);
                return t; // 如果扣款成功，返回true

            }
            else if (inf == "印刷")
            {
                bool t = false;
                t = await 数码印刷扣款(p, bm, yg);
                return t; // 如果扣款成功，返回true

            }
            else if (inf == "后道")
            {
                bool t = false;
                t = await 后道制作扣款(p, bm, yg);
                return t; // 如果扣款成功，返回true

            }
            else if (inf == "设计")
            {
                bool t = false;
                t = await 设计制作扣款(p, bm, yg);
                return t; // 如果扣款成功，返回true

            }
            else
            {
                return false; // 如果inf不匹配任何已知类型，返回false
            }
        }

        /// <summary>    
        /// CTP扣款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> CTP输出扣款(int p, string bm, string yg)
        {
            var view = await HongtengDbCon.Db.Queryable<工作表_CTP输出>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }
            var 客户号 = kh.自动编号;

            //连表查询出客户充值账户以及对应折扣率
            var 客户账户 = await HongtengDbCon.Db.Queryable<充值账户>()
                          .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                          .Where((it, cus) => it.客户编号 == 客户号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                          .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.CTP折扣率 })
                          .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                          .ToListAsync(); ;
            double 折算总余额 = 0;
            double 总余额 = 0;

            if (!客户账户.Any())
            {
                return false; // 客户没有可供账户，直接返回
            }
            foreach (var khzh in 客户账户)
            {
                折算总余额 = (double)(折算总余额 + (double)(khzh.剩余金额 ?? 0) / (khzh.折扣率 ?? 1));
                总余额 = 总余额 + (double)(khzh.剩余金额 ?? 0);
            }

            //检查余额是否足够
            if (view.应收 == null)
            {
                view.应收 = 0;
            }
            if ((double)view.应收 > 总余额 && view.已优惠 == true || (double)view.应收 > 折算总余额)
            {
                return false; // 余额不够，不进行扣款，直接返回
            }

            //初始化实收金额
            decimal ss = 0;                  //实收额
            decimal sk = view.应收 ?? 0;     //应收额
            decimal zk = 1;                  //扣款率

            //循环扣款
            foreach (var item in 客户账户)
            {
                if (view.已优惠 == true)
                {
                    zk = 1;
                }
                else
                {
                    zk = (decimal)(item.折扣率 ?? 1);
                }

                if (sk * zk <= item.剩余金额)
                {
                    item.剩余金额 = item.剩余金额 - sk * zk;
                    总余额 = (double)(总余额 - (double)(sk * zk));
                    //计算实收金额
                    ss = ss + sk * zk;

                    //生成摘要
                    string zy = view.总色数.ToString() + "张" + view.CTP板材型号 + "，" + view.输出要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "CTP扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    //计算各项实收比例
                    decimal feeRatio = 0; //设计劳务费比例
                    decimal costRatio = 1; //产品费比例
                    decimal designFee = view.设计制作费 ?? 0;
                    decimal productFee = view.版费 ?? 0;
                    decimal otherFee = view.其他费用 ?? 0;
                    if (designFee + productFee + otherFee != 0)
                    {
                        feeRatio = (view.设计制作费 ?? 0) / (designFee + productFee + otherFee);
                        costRatio = (view.版费 ?? 0) / (designFee + productFee + otherFee);
                    }
                    view.结清 = true;
                    view.实收 = ss;
                    view.收款单号 = 0;  //单条扣款没有收款单号
                    view.结算号码 = jyid;
                    view.实收设计费 = ss * feeRatio;
                    view.实收CTP费 = ss * costRatio;
                    view.结算状态 = "结算并扣款";

                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新更新工作表收款
                    await HongtengDbCon.Db.Updateable(view).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();

                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();
                    break; // 退出扣款循环
                }
                else
                {
                    //计算此处扣款后剩余金额
                    decimal 剩余金额 = item.剩余金额 ?? 0;
                    sk = (decimal)(sk - 剩余金额 / zk);

                    总余额 = 总余额 - (double)剩余金额;

                    //计算实收金额
                    ss = (decimal)(ss + 剩余金额);

                    //生成摘要
                    string zy = view.总色数.ToString() + "张" + view.CTP板材型号 + "，" + view.输出要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "CTP扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    item.剩余金额 = 0;
                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();

                }

            }

            return true; // 扣款成功，返回true
        }

        /// <summary>    
        /// 菲林扣款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 菲林输出扣款(int p, string bm, string yg)
        {
            var view = await HongtengDbCon.Db.Queryable<工作表_菲林输出>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }
            var 客户号 = kh.自动编号;

            //连表查询出客户充值账户以及对应折扣率
            var 客户账户 = await HongtengDbCon.Db.Queryable<充值账户>()
                          .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                          .Where((it, cus) => it.客户编号 == 客户号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                          .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.菲林折扣率 })
                          .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                          .ToListAsync(); ;
            double 折算总余额 = 0;
            double 总余额 = 0;

            if (!客户账户.Any())
            {
                return false; // 客户没有可供账户，直接返回
            }
            foreach (var khzh in 客户账户)
            {
                折算总余额 = (double)(折算总余额 + (double)(khzh.剩余金额 ?? 0) / (khzh.折扣率 ?? 1));
                总余额 = 总余额 + (double)(khzh.剩余金额 ?? 0);
            }

            //检查余额是否足够
            if (view.应收 == null)
            {
                view.应收 = 0;
            }
            if ((double)view.应收 > 总余额 && view.已优惠 == true || (double)view.应收 > 折算总余额)
            {
                return false; // 余额不够，不进行扣款，直接返回
            }

            //初始化实收金额
            decimal ss = 0;                  //实收额
            decimal sk = view.应收 ?? 0;     //应收额
            decimal zk = 1;                  //扣款率

            //循环扣款
            foreach (var item in 客户账户)
            {
                if (view.已优惠 == true)
                {
                    zk = 1;
                }
                else
                {
                    zk = (decimal)(item.折扣率 ?? 1);
                }

                if (sk * zk <= item.剩余金额)
                {
                    item.剩余金额 = item.剩余金额 - sk * zk;
                    总余额 = (double)(总余额 - (double)(sk * zk));
                    //计算实收金额
                    ss = ss + sk * zk;

                    //生成摘要
                    string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.总色数.ToString() + "张" + view.输出设备 + "，" + view.输出要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "菲林扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    //计算各项实收比例
                    decimal feeRatio = 0; //设计劳务费比例
                    decimal costRatio = 1; //产品费比例
                    decimal designFee = view.设计制作费 ?? 0;
                    decimal productFee = view.菲林费 ?? 0;
                    decimal otherFee = view.其他费用 ?? 0;
                    if (designFee + productFee + otherFee != 0)
                    {
                        feeRatio = (view.设计制作费 ?? 0) / (designFee + productFee + otherFee);
                        costRatio = (view.菲林费 ?? 0) / (designFee + productFee + otherFee);
                    }
                    view.结清 = true;
                    view.实收 = ss;
                    view.收款单号 = 0;  //单条扣款没有收款单号
                    view.结算号码 = jyid;
                    view.实收设计费 = ss * feeRatio;
                    view.实收菲林费 = ss * costRatio;
                    view.结算状态 = "结算并扣款";

                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新更新工作表收款
                    await HongtengDbCon.Db.Updateable(view).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();

                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();
                    break; // 退出扣款循环
                }
                else
                {
                    //计算此处扣款后剩余金额
                    decimal 剩余金额 = item.剩余金额 ?? 0;
                    sk = (decimal)(sk - 剩余金额 / zk);

                    总余额 = 总余额 - (double)剩余金额;

                    //计算实收金额
                    ss = (decimal)(ss + 剩余金额);

                    //生成摘要
                    string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.总色数.ToString() + "张" + view.输出设备 + "，" + view.输出要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "菲林扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    item.剩余金额 = 0;
                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();

                }

            }


            return true; // 扣款成功，返回true
        }

        /// <summary>    
        /// 彩喷扣款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 彩喷写真扣款(int p, string bm, string yg)
        {
            var view = await HongtengDbCon.Db.Queryable<工作表_彩喷写真>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }
            var 客户号 = kh.自动编号;

            //连表查询出客户充值账户以及对应折扣率
            var 客户账户 = await HongtengDbCon.Db.Queryable<充值账户>()
                          .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                          .Where((it, cus) => it.客户编号 == 客户号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                          .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.数码折扣率 })
                          .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                          .ToListAsync(); ;
            double 折算总余额 = 0;
            double 总余额 = 0;

            if (!客户账户.Any())
            {
                return false; // 客户没有可供账户，直接返回
            }
            foreach (var khzh in 客户账户)
            {
                折算总余额 = (double)(折算总余额 + (double)(khzh.剩余金额 ?? 0) / (khzh.折扣率 ?? 1));
                总余额 = 总余额 + (double)(khzh.剩余金额 ?? 0);
            }

            //检查余额是否足够
            if (view.应收 == null)
            {
                view.应收 = 0;
            }
            if ((double)view.应收 > 总余额 && view.已优惠 == true || (double)view.应收 > 折算总余额)
            {
                return false; // 余额不够，不进行扣款，直接返回
            }

            //初始化实收金额
            decimal ss = 0;                  //实收额
            decimal sk = view.应收 ?? 0;     //应收额
            decimal zk = 1;                  //扣款率

            //循环扣款
            foreach (var item in 客户账户)
            {
                if (view.已优惠 == true)
                {
                    zk = 1;
                }
                else
                {
                    zk = (decimal)(item.折扣率 ?? 1);
                }

                if (sk * zk <= item.剩余金额)
                {
                    item.剩余金额 = item.剩余金额 - sk * zk;
                    总余额 = (double)(总余额 - (double)(sk * zk));
                    //计算实收金额
                    ss = ss + sk * zk;

                    //生成摘要
                    string zy = view.张数.ToString() + "印面" + view.规格 + "，" + view.要求及文件位置;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "彩喷扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    //计算各项实收比例
                    decimal feeRatio = 0; //设计劳务费比例
                    decimal costRatio = 1; //产品费比例
                    decimal designFee = view.设计制作费 ?? 0;
                    decimal productFee = view.价格 ?? 0;
                    decimal otherFee = view.其他费用 ?? 0;
                    if (designFee + productFee + otherFee != 0)
                    {
                        feeRatio = (view.设计制作费 ?? 0) / (designFee + productFee + otherFee);
                        costRatio = (view.价格 ?? 0) / (designFee + productFee + otherFee);
                    }
                    view.结清 = true;
                    view.实收 = ss;
                    view.收款单号 = 0;  //单条扣款没有收款单号
                    view.结算号码 = jyid;
                    view.实收设计费 = ss * feeRatio;
                    view.实收打印费 = ss * costRatio;
                    view.结算状态 = "结算并扣款";

                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新更新工作表收款
                    await HongtengDbCon.Db.Updateable(view).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();

                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();
                    break; // 退出扣款循环
                }
                else
                {
                    //计算此处扣款后剩余金额
                    decimal 剩余金额 = item.剩余金额 ?? 0;
                    sk = (decimal)(sk - 剩余金额 / zk);

                    总余额 = 总余额 - (double)剩余金额;

                    //计算实收金额
                    ss = (decimal)(ss + 剩余金额);

                    //生成摘要
                    string zy = view.张数.ToString() + "印面" + view.规格 + "，" + view.要求及文件位置;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "彩喷扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    item.剩余金额 = 0;
                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();

                }

            }


            return true; // 扣款成功，返回true
        }

        /// <summary>    
        /// 快印扣款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 数码印刷扣款(int p, string bm, string yg)
        {
            var view = await HongtengDbCon.Db.Queryable<工作表_数码印刷>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }
            var 客户号 = kh.自动编号;

            //连表查询出客户充值账户以及对应折扣率
            var 客户账户 = await HongtengDbCon.Db.Queryable<充值账户>()
                          .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                          .Where((it, cus) => it.客户编号 == 客户号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                          .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.数码折扣率 })
                          .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                          .ToListAsync(); ;
            double 折算总余额 = 0;
            double 总余额 = 0;

            if (!客户账户.Any())
            {
                return false; // 客户没有可供账户，直接返回
            }
            foreach (var khzh in 客户账户)
            {
                折算总余额 = (double)(折算总余额 + (double)(khzh.剩余金额 ?? 0) / (khzh.折扣率 ?? 1));
                总余额 = 总余额 + (double)(khzh.剩余金额 ?? 0);
            }

            //检查余额是否足够
            if (view.应收 == null)
            {
                view.应收 = 0;
            }
            if ((double)view.应收 > 总余额 && view.已优惠 == true || (double)view.应收 > 折算总余额)
            {
                return false; // 余额不够，不进行扣款，直接返回
            }

            //初始化实收金额
            decimal ss = 0;                  //实收额
            decimal sk = view.应收 ?? 0;     //应收额
            decimal zk = 1;                  //扣款率

            //循环扣款
            foreach (var item in 客户账户)
            {
                if (view.已优惠 == true)
                {
                    zk = 1;
                }
                else
                {
                    zk = (decimal)(item.折扣率 ?? 1);
                }

                if (sk * zk <= item.剩余金额)
                {
                    item.剩余金额 = item.剩余金额 - sk * zk;
                    总余额 = (double)(总余额 - (double)(sk * zk));
                    //计算实收金额
                    ss = ss + sk * zk;

                    //生成摘要
                    string zy = view.张数.ToString() + "印面" + view.规格 + "，" + view.要求及文件位置;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "快印扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    //计算各项实收比例
                    decimal feeRatio = 0; //设计劳务费比例
                    decimal costRatio = 1; //产品费比例
                    decimal designFee = view.设计制作费 ?? 0;
                    decimal productFee = view.印刷费 ?? 0;
                    decimal otherFee = view.其他费用 ?? 0;
                    if (designFee + productFee + otherFee != 0)
                    {
                        feeRatio = (view.设计制作费 ?? 0) / (designFee + productFee + otherFee);
                        costRatio = (view.印刷费 ?? 0) / (designFee + productFee + otherFee);
                    }
                    view.结清 = true;
                    view.实收 = ss;
                    view.收款单号 = 0;  //单条扣款没有收款单号
                    view.结算号码 = jyid;
                    view.实收设计费 = ss * feeRatio;
                    view.实收印制费 = ss * costRatio;
                    view.结算状态 = "结算并扣款";

                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新更新工作表收款
                    await HongtengDbCon.Db.Updateable(view).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();

                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();
                    break; // 退出扣款循环
                }
                else
                {
                    //计算此处扣款后剩余金额
                    decimal 剩余金额 = item.剩余金额 ?? 0;
                    sk = (decimal)(sk - 剩余金额 / zk);

                    总余额 = 总余额 - (double)剩余金额;

                    //计算实收金额
                    ss = (decimal)(ss + 剩余金额);

                    //生成摘要
                    string zy = view.张数.ToString() + "印面" + view.规格 + "，" + view.要求及文件位置;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "快印扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    item.剩余金额 = 0;
                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();

                }

            }


            return true; // 扣款成功，返回true
        }

        /// <summary>    
        /// 后道扣款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 后道制作扣款(int p, string bm, string yg)
        {
            var view = await HongtengDbCon.Db.Queryable<工作表_后道加工>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }
            var 客户号 = kh.自动编号;

            //连表查询出客户充值账户以及对应折扣率
            var 客户账户 = await HongtengDbCon.Db.Queryable<充值账户>()
                          .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                          .Where((it, cus) => it.客户编号 == 客户号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                          .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.后加工折扣率 })
                          .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                          .ToListAsync(); ;
            double 折算总余额 = 0;
            double 总余额 = 0;

            if (!客户账户.Any())
            {
                return false; // 客户没有可供账户，直接返回
            }
            foreach (var khzh in 客户账户)
            {
                折算总余额 = (double)(折算总余额 + (double)(khzh.剩余金额 ?? 0) / (khzh.折扣率 ?? 1));
                总余额 = 总余额 + (double)(khzh.剩余金额 ?? 0);
            }

            //检查余额是否足够
            if (view.应收 == null)
            {
                view.应收 = 0;
            }
            if ((double)view.应收 > 总余额 && view.已优惠 == true || (double)view.应收 > 折算总余额)
            {
                return false; // 余额不够，不进行扣款，直接返回
            }

            //初始化实收金额
            decimal ss = 0;                  //实收额
            decimal sk = view.应收 ?? 0;     //应收额
            decimal zk = 1;                  //扣款率

            //循环扣款
            foreach (var item in 客户账户)
            {
                if (view.已优惠 == true)
                {
                    zk = 1;
                }
                else
                {
                    zk = (decimal)(item.折扣率 ?? 1);
                }

                if (sk * zk <= item.剩余金额)
                {
                    item.剩余金额 = item.剩余金额 - sk * zk;
                    总余额 = (double)(总余额 - (double)(sk * zk));
                    //计算实收金额
                    ss = ss + sk * zk;

                    //生成摘要
                    string zy = view.数量.ToString() + "张" + view.输出设备 + "，" + view.要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "后道扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    //计算各项实收比例
                    decimal feeRatio = 0; //设计劳务费比例
                    decimal costRatio = 1; //产品费比例
                    decimal designFee = view.设计制作费 ?? 0;
                    decimal productFee = view.加工费 ?? 0;
                    decimal otherFee = view.其他费用 ?? 0;
                    if (designFee + productFee + otherFee != 0)
                    {
                        feeRatio = (view.设计制作费 ?? 0) / (designFee + productFee + otherFee);
                        costRatio = (view.加工费 ?? 0) / (designFee + productFee + otherFee);
                    }
                    view.结清 = true;
                    view.实收 = ss;
                    view.收款单号 = 0;  //单条扣款没有收款单号
                    view.结算号码 = jyid;
                    view.实收设计费 = ss * feeRatio;
                    view.实收加工费 = ss * costRatio;
                    view.结算状态 = "结算并扣款";

                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新更新工作表收款
                    await HongtengDbCon.Db.Updateable(view).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();

                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();
                    break; // 退出扣款循环
                }
                else
                {
                    //计算此处扣款后剩余金额
                    decimal 剩余金额 = item.剩余金额 ?? 0;
                    sk = (decimal)(sk - 剩余金额 / zk);

                    总余额 = 总余额 - (double)剩余金额;

                    //计算实收金额
                    ss = (decimal)(ss + 剩余金额);

                    //生成摘要
                    string zy = view.数量.ToString() + "张" + view.输出设备 + "，" + view.要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "后道扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    item.剩余金额 = 0;
                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();

                }

            }


            return true; // 扣款成功，返回true
        }

        /// <summary>    
        /// 设计扣款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 设计制作扣款(int p, string bm, string yg)
        {
            var view = await HongtengDbCon.Db.Queryable<工作表_设计制作>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }
            var 客户号 = kh.自动编号;

            //连表查询出客户充值账户以及对应折扣率
            var 客户账户 = await HongtengDbCon.Db.Queryable<充值账户>()
                          .LeftJoin<充值折扣表>((it, cus) => it.折扣链接 == cus.编号)//多个条件用&&
                          .Where((it, cus) => it.客户编号 == 客户号 && it.是否有效 == true && it.到期时间 >= DateTime.Today && it.剩余金额 > 0)
                          .Select((it, cus) => new 客户账户 { ID = it.ID, 客户ID = it.客户ID, 使用优先级 = it.使用优先级, 剩余金额 = it.剩余金额, 客户编号 = it.客户编号, 折扣率 = cus.纯设计折扣率 })
                          .OrderBy(it => it.使用优先级).OrderBy(it => it.ID)
                          .ToListAsync(); ;
            double 折算总余额 = 0;
            double 总余额 = 0;

            if (!客户账户.Any())
            {
                return false; // 客户没有可供账户，直接返回
            }
            foreach (var khzh in 客户账户)
            {
                折算总余额 = (double)(折算总余额 + (double)(khzh.剩余金额 ?? 0) / (khzh.折扣率 ?? 1));
                总余额 = 总余额 + (double)(khzh.剩余金额 ?? 0);
            }

            //检查余额是否足够
            if (view.应收 == null)
            {
                view.应收 = 0;
            }
            if ((double)view.应收 > 总余额 && view.已优惠 == true || (double)view.应收 > 折算总余额)
            {
                return false; // 余额不够，不进行扣款，直接返回
            }

            //初始化实收金额
            decimal ss = 0;                  //实收额
            decimal sk = view.应收 ?? 0;     //应收额
            decimal zk = 1;                  //扣款率

            //循环扣款
            foreach (var item in 客户账户)
            {
                if (view.已优惠 == true)
                {
                    zk = 1;
                }
                else
                {
                    zk = (decimal)(item.折扣率 ?? 1);
                }

                if (sk * zk <= item.剩余金额)
                {
                    item.剩余金额 = item.剩余金额 - sk * zk;
                    总余额 = (double)(总余额 - (double)(sk * zk));
                    //计算实收金额
                    ss = ss + sk * zk;

                    //生成摘要
                    string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.总色数.ToString() + "张" + view.发送至 + "，" + view.输出要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "设计扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -(sk * zk), 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    //计算各项实收比例
                    decimal feeRatio = 0; //设计劳务费比例
                    decimal costRatio = 1; //产品费比例
                    decimal designFee = view.设计制作费 ?? 0;
                    decimal productFee = view.施工费 ?? 0;
                    decimal otherFee = view.其他费用 ?? 0;
                    if (designFee + productFee + otherFee != 0)
                    {
                        feeRatio = (view.设计制作费 ?? 0) / (designFee + productFee + otherFee);
                        costRatio = (view.施工费 ?? 0) / (designFee + productFee + otherFee);
                    }
                    view.结清 = true;
                    view.实收 = ss;
                    view.收款单号 = 0;  //单条扣款没有收款单号
                    view.结算号码 = jyid;
                    view.实收设计费 = ss * feeRatio;
                    view.实收施工费 = ss * costRatio;
                    view.结算状态 = "结算并扣款";

                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新更新工作表收款
                    await HongtengDbCon.Db.Updateable(view).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();

                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();
                    break; // 退出扣款循环
                }
                else
                {
                    //计算此处扣款后剩余金额
                    decimal 剩余金额 = item.剩余金额 ?? 0;
                    sk = (decimal)(sk - 剩余金额 / zk);

                    总余额 = 总余额 - (double)剩余金额;

                    //计算实收金额
                    ss = (decimal)(ss + 剩余金额);

                    //生成摘要
                    string zy = view.长.ToString() + "x" + view.宽.ToString() + "x" + view.总色数.ToString() + "张" + view.发送至 + "，" + view.输出要求;
                    //----------------------写入交易流水表-----------------------
                    var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "设计扣款", 工单号 = view.编号.ToString(), 用途名称 = view.文件或工作名, 摘要 = zy, 发生金额 = -item.剩余金额, 账户余额 = (decimal)总余额, 扣款账号 = item.ID, 收付款方式 = "账户扣款", 网点 = bm, 操作员 = yg, 原价 = view.应收, 折扣 = (float)zk, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

                    item.剩余金额 = 0;
                    //更新充值账户剩余金额
                    await HongtengDbCon.Db.Updateable<充值账户>()
                                .SetColumns(it => new 充值账户() { 剩余金额 = item.剩余金额 })
                                .Where(it => it.ID == item.ID)
                                .ExecuteCommandAsync();
                    //更新客户表账户余额
                    await HongtengDbCon.Db.Updateable<客户表>()
                               .SetColumns(it => new 客户表() { 账户余额 = (decimal?)总余额 })
                               .Where(it => it.自动编号 == item.客户编号)
                               .ExecuteCommandAsync();

                }

            }


            return true; // 扣款成功，返回true
        }



        //-------------------------------单条退款-------------------------------
        /// <summary>    
        /// 单条退款方法，参数：string分类、int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> RefundOne(string inf, int p, string bm, string yg)
        {
            if (inf == "CTP")
            {
                bool t = false;
                t = await CTP退款(p, bm, yg);
                return t;
            }
            else if (inf == "菲林")
            {
                bool t = false;
                t = await 菲林退款(p, bm, yg);
                return t;

            }
            else if (inf == "彩喷")
            {
                bool t = false;
                t = await 彩喷退款(p, bm, yg);
                return t;

            }
            else if (inf == "印刷")
            {
                bool t = false;
                t = await 印刷退款(p, bm, yg);
                return t;

            }
            else if (inf == "后道")
            {
                bool t = false;
                t = await 后道退款(p, bm, yg);
                return t;

            }
            else if (inf == "设计")
            {
                bool t = false;
                t = await 设计退款(p, bm, yg);
                return t;

            }
            else
            {
                return false;
            }
        }

        /// <summary>    
        /// CTP退款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> CTP退款(int p, string bm, string yg)
        {

            string 部门 = bm;
            string 操作员工 = yg;
            string 流水号 = "";

            var view = await HongtengDbCon.Db.Queryable<工作表_CTP输出>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }

            var 客户号 = kh.自动编号;

            var 交易流水 = await HongtengDbCon.Db.Queryable<交易流水表>().Where(it => it.交易类型 == "CTP扣款" && it.工单号 == view.编号.ToString()).ToListAsync();
            if (!交易流水.Any())
            {
                return false; // 如果没有找到对应的记录，直接返回
            }
            //获取原流水号
            foreach (var item in 交易流水)
            {
                流水号 = $"{流水号}, {item.流水号}";
            }
            //计算客户总余额
            var khye = HongtengDbCon.Db.Queryable<充值账户>()
                                   .GroupBy(it => it.客户编号)
                                   .Where(it => it.客户编号 == 客户号)
                                   .Select(it => new
                                   {
                                       客户编号 = it.客户编号,
                                       价格合计 = SqlFunc.AggregateSum(it.剩余金额)
                                   })
                                   .ToList().AsQueryable();
            decimal 客户总余额 = khye.FirstOrDefault()?.价格合计 ?? 0;

            //计算原折扣
            var ss = view.实收 ?? 0;
            var ys = view.应收 ?? 1;
            if (ys <= 0) ys = 1;  //避免除以0
            var 原折扣 = ss / ys;
            int 折扣号 = 1;

            if (原折扣 < 0.7m)
            {
                折扣号 = 8;
            }
            else if (原折扣 < 0.75m)
            {
                折扣号 = 7;
            }
            else if (原折扣 < 0.8m)
            {
                折扣号 = 6;
            }
            else if (原折扣 < 0.85m)
            {
                折扣号 = 5;
            }
            else if (原折扣 < 0.9m)
            {
                折扣号 = 4;
            }
            else if (原折扣 < 0.95m)
            {
                折扣号 = 3;
            }
            else if (原折扣 < 0.98m)
            {
                折扣号 = 2;
            }
            else
            {
                折扣号 = 1;
            }

            var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "CTP退款", 工单号 = view.编号.ToString(), 用途名称 = $"退款：{view.文件或工作名}", 摘要 = $"原流水号{流水号}", 发生金额 = ss, 账户余额 = (客户总余额 + ss), 扣款账号 = null, 收付款方式 = "退款", 网点 = 部门, 操作员 = 操作员工, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

            //加入充值账户
            await HongtengDbCon.Db.Insertable(new 充值账户() { 客户ID = view.客户, 充值时间 = DateTime.Now, 使用优先级 = 3, 到期时间 = DateTime.Now.AddDays(1830), 充值金额 = ss, 剩余金额 = ss, 是否为赠送 = true, 折扣链接 = 折扣号, 网点 = 部门, 操作员 = 操作员工, 交易方式 = "退款", 说明 = $"CTP退款 {view.编号}", 流水号 = jyid, 有效期 = "五年", 客户编号 = 客户号 }).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();

            //更新客户表账户余额
            await HongtengDbCon.Db.Updateable<客户表>()
                   .SetColumns(it => new 客户表() { 账户余额 = (客户总余额 + ss) })
                   .Where(it => it.自动编号 == 客户号)
                   .ExecuteCommandAsync();

            //更新更新工作表收款
            await HongtengDbCon.Db.Updateable<工作表_CTP输出>()
                    .SetColumns(it => new 工作表_CTP输出() { 结清 = false, 实收 = 0, 收款单号 = null, 实收设计费 = 0, 实收CTP费 = 0, 结算状态 = "已退款" })
                    .Where(it => it.编号 == view.编号)
                    .ExecuteCommandAsync();


            return true;
        }

        /// <summary>    
        /// 菲林退款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 菲林退款(int p, string bm, string yg)
        {

            string 部门 = bm;
            string 操作员工 = yg;
            string 流水号 = "";

            var view = await HongtengDbCon.Db.Queryable<工作表_菲林输出>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }

            var 客户号 = kh.自动编号;

            var 交易流水 = await HongtengDbCon.Db.Queryable<交易流水表>().Where(it => it.交易类型 == "菲林扣款" && it.工单号 == view.编号.ToString()).ToListAsync();
            if (!交易流水.Any())
            {
                return false; // 如果没有找到对应的记录，直接返回
            }
            //获取原流水号
            foreach (var item in 交易流水)
            {
                流水号 = $"{流水号}, {item.流水号}";
            }
            //计算客户总余额
            var khye = HongtengDbCon.Db.Queryable<充值账户>()
                                   .GroupBy(it => it.客户编号)
                                   .Where(it => it.客户编号 == 客户号)
                                   .Select(it => new
                                   {
                                       客户编号 = it.客户编号,
                                       价格合计 = SqlFunc.AggregateSum(it.剩余金额)
                                   })
                                   .ToList().AsQueryable();
            decimal 客户总余额 = khye.FirstOrDefault()?.价格合计 ?? 0;

            //计算原折扣
            var ss = view.实收 ?? 0;
            var ys = view.应收 ?? 1;
            if (ys <= 0) ys = 1;  //避免除以0
            var 原折扣 = ss / ys;
            int 折扣号 = 1;

            if (原折扣 < 0.7m)
            {
                折扣号 = 8;
            }
            else if (原折扣 < 0.75m)
            {
                折扣号 = 7;
            }
            else if (原折扣 < 0.8m)
            {
                折扣号 = 6;
            }
            else if (原折扣 < 0.85m)
            {
                折扣号 = 5;
            }
            else if (原折扣 < 0.9m)
            {
                折扣号 = 4;
            }
            else if (原折扣 < 0.95m)
            {
                折扣号 = 3;
            }
            else if (原折扣 < 0.98m)
            {
                折扣号 = 2;
            }
            else
            {
                折扣号 = 1;
            }

            var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "菲林退款", 工单号 = view.编号.ToString(), 用途名称 = $"退款：{view.文件或工作名}", 摘要 = $"原流水号{流水号}", 发生金额 = ss, 账户余额 = (客户总余额 + ss), 扣款账号 = null, 收付款方式 = "退款", 网点 = 部门, 操作员 = 操作员工, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

            //加入充值账户
            await HongtengDbCon.Db.Insertable(new 充值账户() { 客户ID = view.客户, 充值时间 = DateTime.Now, 使用优先级 = 3, 到期时间 = DateTime.Now.AddDays(1830), 充值金额 = ss, 剩余金额 = ss, 是否为赠送 = true, 折扣链接 = 折扣号, 网点 = 部门, 操作员 = 操作员工, 交易方式 = "退款", 说明 = $"菲林退款 {view.编号}", 流水号 = jyid, 有效期 = "五年", 客户编号 = 客户号 }).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();

            //更新客户表账户余额
            await HongtengDbCon.Db.Updateable<客户表>()
                   .SetColumns(it => new 客户表() { 账户余额 = (客户总余额 + ss) })
                   .Where(it => it.自动编号 == 客户号)
                   .ExecuteCommandAsync();

            //更新更新工作表收款
            await HongtengDbCon.Db.Updateable<工作表_菲林输出>()
                    .SetColumns(it => new 工作表_菲林输出() { 结清 = false, 实收 = 0, 收款单号 = null, 实收设计费 = 0, 实收菲林费 = 0, 结算状态 = "已退款" })
                    .Where(it => it.编号 == view.编号)
                    .ExecuteCommandAsync();


            return true;
        }

        /// <summary>    
        /// 彩喷退款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 彩喷退款(int p, string bm, string yg)
        {

            string 部门 = bm;
            string 操作员工 = yg;
            string 流水号 = "";

            var view = await HongtengDbCon.Db.Queryable<工作表_彩喷写真>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }

            var 客户号 = kh.自动编号;

            var 交易流水 = await HongtengDbCon.Db.Queryable<交易流水表>().Where(it => it.交易类型 == "彩喷扣款" && it.工单号 == view.编号.ToString()).ToListAsync();
            if (!交易流水.Any())
            {
                return false; // 如果没有找到对应的记录，直接返回
            }
            //获取原流水号
            foreach (var item in 交易流水)
            {
                流水号 = $"{流水号}, {item.流水号}";
            }
            //计算客户总余额
            var khye = HongtengDbCon.Db.Queryable<充值账户>()
                                   .GroupBy(it => it.客户编号)
                                   .Where(it => it.客户编号 == 客户号)
                                   .Select(it => new
                                   {
                                       客户编号 = it.客户编号,
                                       价格合计 = SqlFunc.AggregateSum(it.剩余金额)
                                   })
                                   .ToList().AsQueryable();
            decimal 客户总余额 = khye.FirstOrDefault()?.价格合计 ?? 0;

            //计算原折扣
            var ss = view.实收 ?? 0;
            var ys = view.应收 ?? 1;
            if (ys <= 0) ys = 1;  //避免除以0
            var 原折扣 = ss / ys;
            int 折扣号 = 1;

            if (原折扣 < 0.7m)
            {
                折扣号 = 8;
            }
            else if (原折扣 < 0.75m)
            {
                折扣号 = 7;
            }
            else if (原折扣 < 0.8m)
            {
                折扣号 = 6;
            }
            else if (原折扣 < 0.85m)
            {
                折扣号 = 5;
            }
            else if (原折扣 < 0.9m)
            {
                折扣号 = 4;
            }
            else if (原折扣 < 0.95m)
            {
                折扣号 = 3;
            }
            else if (原折扣 < 0.98m)
            {
                折扣号 = 2;
            }
            else
            {
                折扣号 = 1;
            }

            var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "彩喷退款", 工单号 = view.编号.ToString(), 用途名称 = $"退款：{view.文件或工作名}", 摘要 = $"原流水号{流水号}", 发生金额 = ss, 账户余额 = (客户总余额 + ss), 扣款账号 = null, 收付款方式 = "退款", 网点 = 部门, 操作员 = 操作员工, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

            //加入充值账户
            await HongtengDbCon.Db.Insertable(new 充值账户() { 客户ID = view.客户, 充值时间 = DateTime.Now, 使用优先级 = 3, 到期时间 = DateTime.Now.AddDays(1830), 充值金额 = ss, 剩余金额 = ss, 是否为赠送 = true, 折扣链接 = 折扣号, 网点 = 部门, 操作员 = 操作员工, 交易方式 = "退款", 说明 = $"彩喷退款 {view.编号}", 流水号 = jyid, 有效期 = "五年", 客户编号 = 客户号 }).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();

            //更新客户表账户余额
            await HongtengDbCon.Db.Updateable<客户表>()
                   .SetColumns(it => new 客户表() { 账户余额 = (客户总余额 + ss) })
                   .Where(it => it.自动编号 == 客户号)
                   .ExecuteCommandAsync();

            //更新更新工作表收款
            await HongtengDbCon.Db.Updateable<工作表_彩喷写真>()
                    .SetColumns(it => new 工作表_彩喷写真() { 结清 = false, 实收 = 0, 收款单号 = null, 实收设计费 = 0, 实收打印费 = 0, 结算状态 = "已退款" })
                    .Where(it => it.编号 == view.编号)
                    .ExecuteCommandAsync();


            return true;
        }

        /// <summary>    
        /// 印刷退款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 印刷退款(int p, string bm, string yg)
        {

            string 部门 = bm;
            string 操作员工 = yg;
            string 流水号 = "";

            var view = await HongtengDbCon.Db.Queryable<工作表_数码印刷>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }

            var 客户号 = kh.自动编号;

            var 交易流水 = await HongtengDbCon.Db.Queryable<交易流水表>().Where(it => it.交易类型 == "快印扣款" && it.工单号 == view.编号.ToString()).ToListAsync();
            if (!交易流水.Any())
            {
                return false; // 如果没有找到对应的记录，直接返回
            }
            //获取原流水号
            foreach (var item in 交易流水)
            {
                流水号 = $"{流水号}, {item.流水号}";
            }
            //计算客户总余额
            var khye = HongtengDbCon.Db.Queryable<充值账户>()
                                   .GroupBy(it => it.客户编号)
                                   .Where(it => it.客户编号 == 客户号)
                                   .Select(it => new
                                   {
                                       客户编号 = it.客户编号,
                                       价格合计 = SqlFunc.AggregateSum(it.剩余金额)
                                   })
                                   .ToList().AsQueryable();
            decimal 客户总余额 = khye.FirstOrDefault()?.价格合计 ?? 0;

            //计算原折扣
            var ss = view.实收 ?? 0;
            var ys = view.应收 ?? 1;
            if (ys <= 0) ys = 1;  //避免除以0
            var 原折扣 = ss / ys;
            int 折扣号 = 1;

            if (原折扣 < 0.7m)
            {
                折扣号 = 8;
            }
            else if (原折扣 < 0.75m)
            {
                折扣号 = 7;
            }
            else if (原折扣 < 0.8m)
            {
                折扣号 = 6;
            }
            else if (原折扣 < 0.85m)
            {
                折扣号 = 5;
            }
            else if (原折扣 < 0.9m)
            {
                折扣号 = 4;
            }
            else if (原折扣 < 0.95m)
            {
                折扣号 = 3;
            }
            else if (原折扣 < 0.98m)
            {
                折扣号 = 2;
            }
            else
            {
                折扣号 = 1;
            }

            var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "快印退款", 工单号 = view.编号.ToString(), 用途名称 = $"退款：{view.文件或工作名}", 摘要 = $"原流水号{流水号}", 发生金额 = ss, 账户余额 = (客户总余额 + ss), 扣款账号 = null, 收付款方式 = "退款", 网点 = 部门, 操作员 = 操作员工, 工单日期 = view.输出日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

            //加入充值账户
            await HongtengDbCon.Db.Insertable(new 充值账户() { 客户ID = view.客户, 充值时间 = DateTime.Now, 使用优先级 = 3, 到期时间 = DateTime.Now.AddDays(1830), 充值金额 = ss, 剩余金额 = ss, 是否为赠送 = true, 折扣链接 = 折扣号, 网点 = 部门, 操作员 = 操作员工, 交易方式 = "退款", 说明 = $"快印退款 {view.编号}", 流水号 = jyid, 有效期 = "五年", 客户编号 = 客户号 }).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();

            //更新客户表账户余额
            await HongtengDbCon.Db.Updateable<客户表>()
                   .SetColumns(it => new 客户表() { 账户余额 = (客户总余额 + ss) })
                   .Where(it => it.自动编号 == 客户号)
                   .ExecuteCommandAsync();

            //更新更新工作表收款
            await HongtengDbCon.Db.Updateable<工作表_数码印刷>()
                    .SetColumns(it => new 工作表_数码印刷() { 结清 = false, 实收 = 0, 收款单号 = null, 实收设计费 = 0, 实收印制费 = 0, 结算状态 = "已退款" })
                    .Where(it => it.编号 == view.编号)
                    .ExecuteCommandAsync();


            return true;
        }

        /// <summary>    
        /// 后道退款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 后道退款(int p, string bm, string yg)
        {

            string 部门 = bm;
            string 操作员工 = yg;
            string 流水号 = "";

            var view = await HongtengDbCon.Db.Queryable<工作表_后道加工>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }

            var 客户号 = kh.自动编号;

            var 交易流水 = await HongtengDbCon.Db.Queryable<交易流水表>().Where(it => it.交易类型 == "后道扣款" && it.工单号 == view.编号.ToString()).ToListAsync();
            if (!交易流水.Any())
            {
                return false; // 如果没有找到对应的记录，直接返回
            }
            //获取原流水号
            foreach (var item in 交易流水)
            {
                流水号 = $"{流水号}, {item.流水号}";
            }
            //计算客户总余额
            var khye = HongtengDbCon.Db.Queryable<充值账户>()
                                   .GroupBy(it => it.客户编号)
                                   .Where(it => it.客户编号 == 客户号)
                                   .Select(it => new
                                   {
                                       客户编号 = it.客户编号,
                                       价格合计 = SqlFunc.AggregateSum(it.剩余金额)
                                   })
                                   .ToList().AsQueryable();
            decimal 客户总余额 = khye.FirstOrDefault()?.价格合计 ?? 0;

            //计算原折扣
            var ss = view.实收 ?? 0;
            var ys = view.应收 ?? 1;
            if (ys <= 0) ys = 1;  //避免除以0
            var 原折扣 = ss / ys;
            int 折扣号 = 1;

            if (原折扣 < 0.7m)
            {
                折扣号 = 8;
            }
            else if (原折扣 < 0.75m)
            {
                折扣号 = 7;
            }
            else if (原折扣 < 0.8m)
            {
                折扣号 = 6;
            }
            else if (原折扣 < 0.85m)
            {
                折扣号 = 5;
            }
            else if (原折扣 < 0.9m)
            {
                折扣号 = 4;
            }
            else if (原折扣 < 0.95m)
            {
                折扣号 = 3;
            }
            else if (原折扣 < 0.98m)
            {
                折扣号 = 2;
            }
            else
            {
                折扣号 = 1;
            }

            var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "后道退款", 工单号 = view.编号.ToString(), 用途名称 = $"退款：{view.文件或工作名}", 摘要 = $"原流水号{流水号}", 发生金额 = ss, 账户余额 = (客户总余额 + ss), 扣款账号 = null, 收付款方式 = "退款", 网点 = 部门, 操作员 = 操作员工, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

            //加入充值账户
            await HongtengDbCon.Db.Insertable(new 充值账户() { 客户ID = view.客户, 充值时间 = DateTime.Now, 使用优先级 = 3, 到期时间 = DateTime.Now.AddDays(1830), 充值金额 = ss, 剩余金额 = ss, 是否为赠送 = true, 折扣链接 = 折扣号, 网点 = 部门, 操作员 = 操作员工, 交易方式 = "退款", 说明 = $"后道退款 {view.编号}", 流水号 = jyid, 有效期 = "五年", 客户编号 = 客户号 }).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();

            //更新客户表账户余额
            await HongtengDbCon.Db.Updateable<客户表>()
                   .SetColumns(it => new 客户表() { 账户余额 = (客户总余额 + ss) })
                   .Where(it => it.自动编号 == 客户号)
                   .ExecuteCommandAsync();

            //更新更新工作表收款
            await HongtengDbCon.Db.Updateable<工作表_后道加工>()
                    .SetColumns(it => new 工作表_后道加工() { 结清 = false, 实收 = 0, 收款单号 = null, 实收设计费 = 0, 实收加工费 = 0, 结算状态 = "已退款" })
                    .Where(it => it.编号 == view.编号)
                    .ExecuteCommandAsync();


            return true;
        }

        /// <summary>    
        /// 设计退款方法，参数：int编号、string部门、string操作员。
        /// </summary>
        public static async Task<bool> 设计退款(int p, string bm, string yg)
        {

            string 部门 = bm;
            string 操作员工 = yg;
            string 流水号 = "";

            var view = await HongtengDbCon.Db.Queryable<工作表_设计制作>().Where(it => it.编号 == p).FirstAsync();
            if (view == null)
            {
                return false; // 如果没有找到对应的记录，直接返回
            }

            var kh = await HongtengDbCon.Db.Queryable<客户表>().Where(it => it.客户ID == view.客户).FirstAsync();
            if (kh == null)
            {
                return false; // 如果没有找到对应的客户，直接返回
            }

            var 客户号 = kh.自动编号;

            var 交易流水 = await HongtengDbCon.Db.Queryable<交易流水表>().Where(it => it.交易类型 == "设计扣款" && it.工单号 == view.编号.ToString()).ToListAsync();
            if (!交易流水.Any())
            {
                return false; // 如果没有找到对应的记录，直接返回
            }
            //获取原流水号
            foreach (var item in 交易流水)
            {
                流水号 = $"{流水号}, {item.流水号}";
            }
            //计算客户总余额
            var khye = HongtengDbCon.Db.Queryable<充值账户>()
                                   .GroupBy(it => it.客户编号)
                                   .Where(it => it.客户编号 == 客户号)
                                   .Select(it => new
                                   {
                                       客户编号 = it.客户编号,
                                       价格合计 = SqlFunc.AggregateSum(it.剩余金额)
                                   })
                                   .ToList().AsQueryable();
            decimal 客户总余额 = khye.FirstOrDefault()?.价格合计 ?? 0;

            //计算原折扣
            var ss = view.实收 ?? 0;
            var ys = view.应收 ?? 1;
            if (ys <= 0) ys = 1;  //避免除以0
            var 原折扣 = ss / ys;
            int 折扣号 = 1;

            if (原折扣 < 0.7m)
            {
                折扣号 = 8;
            }
            else if (原折扣 < 0.75m)
            {
                折扣号 = 7;
            }
            else if (原折扣 < 0.8m)
            {
                折扣号 = 6;
            }
            else if (原折扣 < 0.85m)
            {
                折扣号 = 5;
            }
            else if (原折扣 < 0.9m)
            {
                折扣号 = 4;
            }
            else if (原折扣 < 0.95m)
            {
                折扣号 = 3;
            }
            else if (原折扣 < 0.98m)
            {
                折扣号 = 2;
            }
            else
            {
                折扣号 = 1;
            }

            var jyid = await HongtengDbCon.Db.Insertable(new 交易流水表() { 日期 = DateTime.Now, 客户 = view.客户, 交易类型 = "设计退款", 工单号 = view.编号.ToString(), 用途名称 = $"退款：{view.文件或工作名}", 摘要 = $"原流水号{流水号}", 发生金额 = ss, 账户余额 = (客户总余额 + ss), 扣款账号 = null, 收付款方式 = "退款", 网点 = 部门, 操作员 = 操作员工, 工单日期 = view.日期 }).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnIdentityAsync();

            //加入充值账户
            await HongtengDbCon.Db.Insertable(new 充值账户() { 客户ID = view.客户, 充值时间 = DateTime.Now, 使用优先级 = 3, 到期时间 = DateTime.Now.AddDays(1830), 充值金额 = ss, 剩余金额 = ss, 是否为赠送 = true, 折扣链接 = 折扣号, 网点 = 部门, 操作员 = 操作员工, 交易方式 = "退款", 说明 = $"设计退款 {view.编号}", 流水号 = jyid, 有效期 = "五年", 客户编号 = 客户号 }).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();

            //更新客户表账户余额
            await HongtengDbCon.Db.Updateable<客户表>()
                   .SetColumns(it => new 客户表() { 账户余额 = (客户总余额 + ss) })
                   .Where(it => it.自动编号 == 客户号)
                   .ExecuteCommandAsync();

            //更新更新工作表收款
            await HongtengDbCon.Db.Updateable<工作表_设计制作>()
                    .SetColumns(it => new 工作表_设计制作() { 结清 = false, 实收 = 0, 收款单号 = null, 实收设计费 = 0, 实收施工费 = 0, 结算状态 = "已退款" })
                    .Where(it => it.编号 == view.编号)
                    .ExecuteCommandAsync();


            return true;
        }





    }
}
