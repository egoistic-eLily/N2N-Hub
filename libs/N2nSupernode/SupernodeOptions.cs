namespace N2nSupernode;

// SupernodeHost 的启动配置（纯数据）。
//
// 职责边界：所有配置文件（community.list、config.json、证书等）的
// 创建、校验与维护完全由主程序负责；本库不做任何文件 I/O，
// 只接收本配置对象并按其内容拉起 supernode 进程。
public sealed class SupernodeOptions
{
    // supernode 可执行文件路径（Windows: thirdparty/bin/supernode.exe；
    // Linux: 自行编译的 supernode 二进制路径）
    public string ExecutablePath { get; set; } = "supernode";

    // 主 UDP 监听端口（edge 的 -l 参数指向 本机IP:该端口）
    public int Port { get; set; } = 7654;

    // 管理端口（回环 UDP，仅本机可访问；0 = 禁用管理口）
    public int ManagementPort { get; set; } = 5644;

    // 社区白名单文件路径（-c 参数，原样透传）
    public string? CommunityListPath { get; set; }

    // 联邦名称（-F 参数，原样透传；null = 使用 n2n 默认联邦）
    public string? FederationName { get; set; }

    // 是否追加 -v（详细日志）
    public bool Verbose { get; set; }

    // 服务对外域名（仅作为信息载体随配置传入，supernode 本身不消费；
    // 供主程序在日志/edge 配置下发时取用）
    public string? Url { get; set; }

    // 附加命令行参数（原样追加）
    public IList<string> ExtraArguments { get; } = new List<string>();

    // 是否允许 supernode 自动降权（Linux）。n2n 默认在未指定 -u/-g 时会查找
    // "n2n"/"nobody" 用户并 setuid 过去（supernode.c:695）——若宿主以 root 运行
    // 且白名单位于 nobody 不可穿越的路径（如 /root），降权后将无法读取白名单。
    // 置为 false 时本库传入两个非零的 -u/-g，触发 supernode.c:701 的重置逻辑
    // 跳过降权，使 supernode 保持与宿主进程相同的用户身份。
    public bool DropPrivileges { get; set; } = true;

    // 组装命令行参数
    internal string BuildArguments()
    {
        var args = new List<string>();

        if (!string.IsNullOrWhiteSpace(CommunityListPath))
        {
            args.Add($"-c \"{CommunityListPath}\"");
        }

        args.Add($"-p {Port}");

        if (ManagementPort > 0)
        {
            args.Add($"-t {ManagementPort}");
        }

        if (!string.IsNullOrWhiteSpace(FederationName))
        {
            args.Add($"-F \"{FederationName}\"");
        }

        if (Verbose)
        {
            args.Add("-v");
        }

        if (OperatingSystem.IsLinux() && !DropPrivileges)
        {
            // 两个非零值会在 supernode.c:701-702 被重置为 0，从而跳过降权
            args.Add("-u 1");
            args.Add("-g 1");
        }

        foreach (string extra in ExtraArguments)
        {
            if (!string.IsNullOrWhiteSpace(extra))
            {
                args.Add(extra.Trim());
            }
        }

        return string.Join(' ', args);
    }
}
