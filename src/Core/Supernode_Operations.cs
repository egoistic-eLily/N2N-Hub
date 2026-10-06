using N2nSupernode;

namespace N2N_USER_SERVER.Core.Supernode
{
    static class SupernodeOperations
    {
        public static async Task Init()
        {
            SupernodeHost.MessageReceived += information;
            SupernodeHost.Exited += Exitedinformation;
            await Start_Supernode();
        }
        public async static Task<Core.DatabaseOper.WriteResults> Start_Supernode()
        {
            try
            {
                var options = new N2nSupernode.SupernodeOptions()
                {
                    ExecutablePath = Bootstrap.Initialization.config.supernode_path,
                    Port = Bootstrap.Initialization.config.supernode_port,
                    ManagementPort = Bootstrap.Initialization.config.supernode_ManagementPort,
                    CommunityListPath = Bootstrap.Initialization.config.supernode_CommunityListPath
                };
                //supernode 默认会 daemon(0,0) 自守护化（sn_utils.c: daemon=1）：
                //父进程立即退出、stdout 转向 /dev/null，导致宿主 IsRunning 恒为
                //false 且收不到任何日志。-f 强制前台运行，使其成为宿主的直接子进程。
                options.ExtraArguments.Add("-f");
                //与宿主保持同一用户身份：禁用 n2n 的自动降权，否则以 root 运行时
                //supernode 会降为 nobody，无法读取 /root 等路径下的白名单文件。
                options.DropPrivileges = false;
                //开启详细日志：注册/认证失败的详细信息（含 "authentication failed"、
                //来源地址等）位于 TRACE_INFO 级别，默认级别下不输出。
                //调试期常开；如嫌日志量大，可在确认问题后改回 false。
                options.Verbose = true;

                N2nSupernode.SupernodeHost.Start(options);
            }
            catch(Exception)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn,"Start_Supernode ：未知错误");
                return new DatabaseOper.WriteResults()
                {
                    type = "Start_Supernode",
                    success = false,
                    recode = 500,
                    hint = $"服务未知错误,请考虑硬重启"
                };
            }
            return new DatabaseOper.WriteResults()
            {
                type = "Start_Supernode",
                success = true,
                recode = 200,
                hint = $"OK"
            };
        }

        public async static Task<Core.DatabaseOper.WriteResults> HotReload_Supernode()
        {
            if(Bootstrap.Initialization.config.supernode_ManagementPort <= 0)
            {
                return new DatabaseOper.WriteResults()
                {
                    type = "HotReload_Supernode",
                    success = false,
                    recode = 500,
                    hint = "管理接口未开启，请使用硬重启"
                };
            }
            if (!SupernodeHost.IsRunning)
            {
                return new DatabaseOper.WriteResults()
                {
                    type = "HotReload_Supernode",
                    success = false,
                    recode = 500,
                    hint = "Supernode服务未运行，请先启动"
                };
            }

            try
            {
                string reply = await SupernodeHost.SendManagementCommandAsync("reload_communities");
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"reload_communities 应答: {reply}");
                return new DatabaseOper.WriteResults()
                {
                    type = "HotReload_Supernode",
                    success = true,
                    recode = 200,
                    hint = $"返回-{reply}"
                };
            }
            catch (TimeoutException)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn,"reload_communities 超时：supernode 可能未运行或管理口无响应");
                return new DatabaseOper.WriteResults()
                {
                    type = "HotReload_Supernode",
                    success = false,
                    recode = 504,
                    hint = $"请求超时请考虑硬重启"
                };
            }
            catch(Exception)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn,"HotReload_Supernode ：未知错误");
                return new DatabaseOper.WriteResults()
                {
                    type = "HotReload_Supernode",
                    success = false,
                    recode = 500,
                    hint = $"服务未知错误,请考虑硬重启"
                };
            }
        }

        public async static Task<Core.DatabaseOper.WriteResults> HardReboot_Supernode()
        {

            try
            {
                Stop_Supernode();
                await Task.Delay(2000);
                await Start_Supernode();
                await Task.Delay(2000);
                if (!SupernodeHost.IsRunning)
                {
                    return new DatabaseOper.WriteResults()
                    {
                        type = "HardReboot_Supernode",
                        success = false,
                        recode = 500,
                        hint = $"严重警告 我们尝试重启 但Supernode似乎仍然未启动 \n 您可以选择再试一次 或手动排除错误"
                    };
                }

                return new DatabaseOper.WriteResults()
                {
                    type = "HardReboot_Supernode",
                    success = true,
                    recode = 200,
                    hint = $"成功重启"
                };
            }
            catch(Exception)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn,"HardReboot_Supernode ：未知错误");
                return new DatabaseOper.WriteResults()
                {
                    type = "HardReboot_Supernode",
                    success = false,
                    recode = 500,
                    hint = $"服务未知错误,请考虑硬重启"
                };
            }

        }

        public static void Stop_Supernode()
        {
            N2nSupernode.SupernodeHost.Stop();
        }
        private static void information(object? sender, SupernodeMessage m)
        {
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"Supernode: {m.Timestamp}  Level: {m.Level} Source: {m.Source} Text:{m.Text}");
        }
        private static void Exitedinformation(object? sender, int code)
        {
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"Supernode已退出: code:{code}");
        }
    }
}