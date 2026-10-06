/* 设备配置管理：增删改查 */
(() => {
    if (!AdminApp.initPage("configs")) return;

    const A = AdminApp;
    let configs = [];
    let users = [];
    let communities = [];

    async function load() {
        const [c, u, m] = await Promise.all([
            A.post("/api/getusersconfig", { RequestType: "requestuser_config", token: A.token() }, "获取配置列表失败"),
            A.post("/api/getuserdata", { RequestType: "list", token: A.token() }),
            A.post("/api/get_communitydata", { RequestType: "list", token: A.token() })
        ]);
        if (!c.ok) return;

        configs = c.data.data || [];
        users = (u.ok && u.data.usersdata) || [];
        communities = (m.ok && m.data.data) || [];
        const commName = id => {
            const cm = communities.find(x => x.id === id);
            return cm ? cm.name : ("未知社区 #" + id);
        };

        const tbody = document.getElementById("ConfigRows");
        const empty = document.getElementById("ConfigEmpty");

        tbody.innerHTML = configs.map(cfg => {
            const overridden = cfg.supernode_ip || (cfg.supernode_port !== null && cfg.supernode_port !== undefined);
            const sn = overridden
                ? '<span class="code">' + A.esc((cfg.supernode_ip || "全局地址") + ":" + (cfg.supernode_port ?? "全局端口")) + "</span>"
                : '<span class="badge gray">跟随全局</span>';
            return `
            <tr>
                <td class="code">${A.esc(cfg.user_id)}</td>
                <td>${A.esc(cfg.device_name)}</td>
                <td>${A.esc(commName(cfg.community_id))}</td>
                <td>${sn}</td>
                <td>${A.secretCell(cfg.key)}</td>
                <td>${A.secretCell(cfg.password)}</td>
                <td>
                    <div class="row-actions">
                        <button class="btn small" data-act="edit" data-id="${A.esc(cfg.user_id)}">编辑</button>
                        <button class="btn small danger" data-act="del" data-id="${A.esc(cfg.user_id)}">解绑</button>
                    </div>
                </td>
            </tr>`;
        }).join("");

        empty.style.display = configs.length ? "none" : "block";
    }

    /* ---------------- 弹窗 ---------------- */

    const dlg = document.getElementById("ConfigDialog");
    let editing = null;

    function fillSelect(sel, entries, value) {
        sel.innerHTML = entries.length
            ? entries.map(e => `<option value="${A.esc(e.v)}">${A.esc(e.t)}</option>`).join("")
            : '<option value="">— 请先创建 —</option>';
        if (value !== undefined && value !== null) sel.value = String(value);
    }

    function openConfigDialog(item) {
        editing = item || null;
        document.getElementById("ConfigDialogTitle").textContent =
            editing ? "编辑设备配置" : "新建设备配置";

        // 用户下拉：新建时只列出尚未拥有配置的用户
        const userSel = document.getElementById("CfgUser");
        if (editing) {
            userSel.innerHTML = `<option value="${A.esc(editing.user_id)}">${A.esc(editing.user_id)}</option>`;
            document.getElementById("CfgUserField").style.display = "none";
        } else {
            const free = users.filter(u => !configs.some(c => c.user_id === u.id));
            fillSelect(userSel, free.map(u => ({ v: u.id, t: u.id + " · " + u.username })));
            document.getElementById("CfgUserField").style.display = "";
        }

        fillSelect(document.getElementById("CfgCommunity"),
            communities.map(cm => ({ v: cm.id, t: cm.name })),
            editing ? editing.community_id : undefined);

        document.getElementById("CfgDevice").value = editing ? editing.device_name : "";
        document.getElementById("CfgDevice").readOnly = !!editing; // 修改以设备名定位，不可改名
        document.getElementById("CfgPassword").value = editing ? (editing.password || "") : "";
        document.getElementById("CfgSnIp").value = editing ? (editing.supernode_ip || "") : "";
        document.getElementById("CfgSnPort").value =
            editing && editing.supernode_port !== null && editing.supernode_port !== undefined
                ? editing.supernode_port : "";

        dlg.showModal();
    }

    document.getElementById("BtnAddConfig").addEventListener("click", () => {
        if (!communities.length) {
            A.toast("请先创建社区", "设备配置必须挂载到一个已存在的社区（控制台 → 社区管理）", "warn", 6000);
            return;
        }
        openConfigDialog(null);
    });
    document.getElementById("ConfigCancel").addEventListener("click", () => dlg.close());

    document.getElementById("ConfigForm").addEventListener("submit", async e => {
        e.preventDefault();
        const saveBtn = document.getElementById("ConfigSave");
        const device = document.getElementById("CfgDevice").value.trim();
        const password = document.getElementById("CfgPassword").value;
        const communityId = Number(document.getElementById("CfgCommunity").value);

        // 自定义超级节点地址/端口：留空 → null（服务端回退到全局配置）
        const snIp = document.getElementById("CfgSnIp").value.trim();
        const snPort = document.getElementById("CfgSnPort").value;
        const snFields = {
            supernode_ip: snIp === "" ? null : snIp,
            supernode_port: snPort === "" ? null : Number(snPort)
        };
        if (snPort !== "" && (!Number.isInteger(snFields.supernode_port) ||
                              snFields.supernode_port < 1 || snFields.supernode_port > 65535)) {
            A.toast("端口无效", "自定义超级节点端口须为 1-65535 的整数", "warn");
            return;
        }

        let res;
        AdminApp.busy(saveBtn, true);
        if (editing) {
            res = await A.post("/api/revise_config", {
                community_id: communityId,
                user_id: editing.user_id,
                username: device,          // 服务端按设备名定位
                password,                  // 修改必须提供密码（重新派生公钥）
                ...snFields,
                token: A.token()
            }, "修改配置失败");
        } else {
            res = await A.post("/api/create_config", {
                community_id: communityId,
                user_id: Number(document.getElementById("CfgUser").value),
                username: device,
                password,
                ...snFields,
                token: A.token()
            }, "创建配置失败");
        }
        AdminApp.busy(saveBtn, false);

        if (res.ok) {
            A.toast(editing ? "配置已修改" : "配置已创建", device, "success");
            dlg.close();
            await load();
        } else if (res.data) {
            A.toast("操作失败", res.data.hint || "", "error");
        }
    });

    /* ---------------- 解绑（删除单条配置） ---------------- */

    document.getElementById("ConfigRows").addEventListener("click", async e => {
        const btn = e.target.closest("button[data-act]");
        if (!btn) return;
        const cfg = configs.find(x => String(x.user_id) === btn.dataset.id);
        if (!cfg) return;

        if (btn.dataset.act === "edit") {
            openConfigDialog(cfg);
            return;
        }

        const yes = await A.confirmDialog({
            title: "解绑设备",
            message: `确定删除设备「${cfg.device_name}」的配置吗？该设备将无法通过 /login 获取边缘配置（用户账号不受影响）。`,
            danger: true, okText: "解绑"
        });
        if (!yes) return;

        const res = await A.post("/api/deleteuserdata", {
            id: cfg.user_id,
            name: cfg.device_name,
            RequestType: "unbind",
            token: A.token()
        }, "解绑失败");
        if (res.ok) {
            A.toast("设备已解绑", cfg.device_name, "success");
            await load();
        } else if (res.data) {
            A.toast("解绑失败", res.data.hint || "", "error");
        }
    });

    load();
})();
