# HtPrintMGT

## 介绍
HtPrintMGT 是一个数码印刷设计制版管理系统，旨在提高印刷行业的工作效率和管理能力。该系统涵盖了从客户管理、订单处理到生产制版、财务结算等全流程业务功能。

## 软件架构
- **前端技术**：使用 [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) 构建，支持交互式 Web 应用程序。
- **后端技术**：基于 .NET 平台，使用 SqlSugar 作为 ORM 框架进行数据库操作。
- **数据库支持**：支持多种数据库类型，包括 SQL Server、MySQL、SQLite 和 Access。

## 安装教程

1. **解压文件**：将项目文件解压到目标目录。
2. **配置数据库连接**：
   - 打开 `appsettings.json` 文件。
   - 修改 `HtdbCon` 字段为你的数据库连接字符串。
   - 设置 `DbType` 字段为对应的数据库类型（如 `SqlServer`, `MySql`, `Sqlite`, `Access`）。
3. **启动项目**：
   - 使用 Visual Studio 或命令行工具运行项目。
   - 确保数据库已正确配置并启动。

## 使用说明

### 数据库配置
- **默认数据库连接字符串**：
  ```json
  "HtdbCon": "Server=(localdb)\\mssqllocaldb;Database=httestdb;Trusted_Connection=True;MultipleActiveResultSets=true"
  ```
  这是 SQL Server LocalDB 的默认连接字符串。如果你使用 SQL Server Express，可以修改为：
  ```json
  "HtdbCon": "server=服务器地址\\SQLEXPRESS;uid=用户名;pwd=密码;database=数据库名;Encrypt=True;Trust Server Certificate=True"
  ```

- **支持的数据库类型**：
  - `SqlServer`：适用于 SQL Server、SQL Server Express、SQL Server Express LocalDB。
  - `MySql`：适用于 MySQL 数据库。
  - `Sqlite`：适用于 SQLite 数据库。
  - `Access`：适用于 Microsoft Access 数据库。

### 初始登录信息
- **手机号**：`123456789`
- **密码**：`123456`

### 主要功能模块
- **客户管理**：管理客户信息，包括联系方式、地址、VIP 状态等。
- **订单处理**：创建和管理订单，包括设计、CTP 输出、印刷、后加工等环节。
- **财务管理**：处理收款账单、支出记录、结算等财务相关操作。
- **员工管理**：管理员工信息、权限分配、部门设置等。
- **报表与统计**：提供各类业务报表和数据分析功能，帮助管理者做出决策。

## 参与贡献

1. **Fork 仓库**：在 Gitee 上 Fork 本仓库。
2. **创建分支**：新建一个功能分支，例如 `Feat_xxx`。
3. **提交代码**：完成开发后提交代码。
4. **提交 Pull Request**：在 Gitee 上提交 Pull Request，等待审核和合并。

## 联系方式
如有任何问题或建议，请提交 Issue 或联系项目维护者。

---

**HtPrintMGT** 是一个功能强大且灵活的数码印刷管理系统，适用于中小型印刷企业。欢迎社区贡献和反馈，共同完善该项目！