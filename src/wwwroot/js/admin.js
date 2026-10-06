/* 控制台：统计 + 服务控制 + 社区管理 */
(() => {
    if (!AdminApp.initPage("admin")) return;

    const A = AdminApp;
    let communities = [];

    /* ---------------- 统计 ---------------- */

    async function loadStats() {
        const [u, c, m] = await Promise.all([
            A.post("/api/getuserdata", { RequestType: "list", token: A.token() }),
            A.post("/api/getusersconfig", { RequestType: "requestuser_config", token: A.token() }),
            A.post("/api/get_communitydata", { RequestType: "list", token: A.token() })
        ]);
        document.getElementById("StatUsers").textContent =
            u.ok ? u.data.usersdata.length : "—";
        document.getElementById("StatConfigs").textContent =
            c.ok ? c.data.data.length : "—";
        document.getElementById("StatCommunities").textContent =
            m.ok ? m.data.data.length : "—";
    }

    /* ---------------- 社区管理 ---------------- */

    async function loadCommunities() {
        const res = await A.post("/api/get_communitydata",
            { RequestType: "list", token: A.token() }, "获取社区列表失败");
        if (!res.ok) return;

        communities = res.data.data || [];
        const tbody = document.getElementById("CommunityRows");
        const empty = document.getElementById("CommunityEmpty");

        tbody.innerHTML = communities.map(c => `
            <tr>
                <td class="code">${A.esc(c.id)}</td>
                <td>${A.esc(c.name)}</td>
                <td>${A.communityTypeBadge(c.type)}</td>
                <td>${c.network ? '<span class="code">' + A.esc(c.network) + "</span>" : '<span style="color:var(--muted)">—</span>'}</td>
                <td>${A.secretCell(c.communitykey)}</td>
                <td>
                    <div class="row-actions">
                        <button class="btn small" data-act="edit" data-id="${A.esc(c.id)}">编辑</button>
                        <button class="btn small danger" data-act="del" data-id="${A.esc(c.id)}">删除</button>
                    </div>
                </td>
            </tr>`).join("");

        empty.style.display = communities.length ? "none" : "block";
    }

    document.getElementById("CommunityRows").addEventListener("click", async e => {
        const btn = e.target.closest("button[data-act]");
        if (!btn) return;
        const item = communities.find(x => String(x.id) === btn.dataset.id);
        if (!item) return;

        if (btn.dataset.act === "edit") {
            openCommunityDialog(item);
        } else {
            const yes = await A.confirmDialog({
                title: "删除社区",
                message: `确定删除社区「${item.name}」吗？该社区仍有绑定用户时将被拒绝。`,
                danger: true, okText: "删除"
            });
            if (!yes) return;
            const res = await A.post("/api/delete_community",
                { id: item.id, name: item.name, RequestType: "delete", token: A.token() },
                "删除社区失败");
            if (res.ok) {
                A.toast("社区已删除", item.name, "success");
                await refreshAll();
            } else if (res.data) {
                A.toast("删除失败", res.data.hint || "", "error");
            }
        }
    });

    /* ---------------- 社区 弹窗 ---------------- */

    const dlg = document.getElementById("CommunityDialog");
    let editing = null; // null = 新建

    function openCommunityDialog(item) {
        editing = item || null;
        document.getElementById("CommunityDialogTitle").textContent =
            editing ? "编辑社区" : "新建社区";
        document.getElementById("CName").value = editing ? editing.name : "";
        document.getElementById("CName").readOnly = !!editing; // 修改以名称定位，不可改名
        const typeStr = editing ? (editing.type === 1 ? "_fixed" : "_regex") : "_fixed";
        document.getElementById("CType").value = typeStr;
        document.getElementById("CNetwork").value = editing ? (editing.network || "") : "";
        document.getElementById("CKey").value = editing ? (editing.communitykey || "") : "";
        toggleNetwork();
        dlg.showModal();
    }

    function toggleNetwork() {
        const isFixed = document.getElementById("CType").value === "_fixed";
        document.getElementById("CNetworkField").style.display = isFixed ? "" : "none";
    }
    document.getElementById("CType").addEventListener("change", toggleNetwork);
    document.getElementById("BtnAddCommunity").addEventListener("click", () => openCommunityDialog(null));
    document.getElementById("CommunityCancel").addEventListener("click", () => dlg.close());

    document.getElementById("CommunityForm").addEventListener("submit", async e => {
        e.preventDefault();
        const saveBtn = document.getElementById("CommunitySave");
        const name = document.getElementById("CName").value.trim();
        const type = document.getElementById("CType").value;
        const network = document.getElementById("CNetwork").value.trim();
        const communityKey = document.getElementById("CKey").value.trim();

        const payload = {
            name,
            type,
            network: type === "_fixed" && network !== "" ? network : null,
            communityKey: communityKey !== "" ? communityKey : null,
            requestType: editing ? "revise" : "create",
            token: A.token()
        };

        const path = editing ? "/api/revise_community" : "/api/create_community";
        AdminApp.busy(saveBtn, true);
        const res = await A.post(path, payload, editing ? "修改社区失败" : "创建社区失败");
        AdminApp.busy(saveBtn, false);

        if (res.ok) {
            A.toast(editing ? "社区已修改" : "社区已创建", name, "success");
            dlg.close();
            await refreshAll();
        } else if (res.data) {
            A.toast("操作失败", res.data.hint || "", "error");
        }
    });

    /* ---------------- 服务控制 ---------------- */

    async function serviceAction(path, confirmOpts, okTitle) {
        if (confirmOpts) {
            const yes = await A.confirmDialog(confirmOpts);
            if (!yes) return;
        }
        const res = await A.post(path, { RequestType: "service", token: A.token() }, "操作失败");
        if (res.ok) {
            A.toast(okTitle, (res.data && res.data.hint) || "", "success", 6000);
        } else if (res.data) {
            A.toast(okTitle + "失败", res.data.hint || "", "error", 6500);
        }
    }

    document.getElementById("BtnHotReload").addEventListener("click", () =>
        serviceAction("/api/supernodeserver/hotreload", null, "热重载完成"));

    document.getElementById("BtnStart").addEventListener("click", () =>
        serviceAction("/api/supernodeserver/start",
            { title: "启动 Supernode", message: "确认启动超级节点服务？", okText: "启动" },
            "服务已启动"));

    document.getElementById("BtnStop").addEventListener("click", () =>
        serviceAction("/api/supernodeserver/stop",
            { title: "停止 Supernode", message: "确认停止超级节点服务？所有在线用户将断开连接。", danger: true, okText: "停止" },
            "服务已停止"));

    document.getElementById("BtnHardReboot").addEventListener("click", () =>
        serviceAction("/api/supernodeserver/hardreboot",
            { title: "硬重启 Supernode", message: "确认硬重启超级节点服务？在线用户将短暂断开。", danger: true, okText: "硬重启" },
            "硬重启完成"));

    /* ---------------- 初始化 ---------------- */

    async function refreshAll() {
        await Promise.all([loadStats(), loadCommunities()]);
    }
    refreshAll();
})();
