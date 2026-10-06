/* 用户管理：账号增删改查 */
(() => {
    if (!AdminApp.initPage("users")) return;

    const A = AdminApp;
    let users = [];        // getuserdata.usersdata
    let configs = [];      // getusersconfig.data（用于展示绑定状态）

    async function load() {
        const [u, c] = await Promise.all([
            A.post("/api/getuserdata", { RequestType: "list", token: A.token() }, "获取用户列表失败"),
            A.post("/api/getusersconfig", { RequestType: "requestuser_config", token: A.token() })
        ]);
        if (!u.ok) return;
        users = u.data.usersdata || [];
        configs = (c.ok && c.data.data) || [];

        const boundIds = new Set(configs.map(x => x.user_id));
        const tbody = document.getElementById("UserRows");
        const empty = document.getElementById("UserEmpty");

        tbody.innerHTML = users.map(u => `
            <tr>
                <td class="code">${A.esc(u.id)}</td>
                <td>${A.esc(u.username)}</td>
                <td>${A.boolBadge(u.enabled)}</td>
                <td>${boundIds.has(u.id)
                    ? '<span class="badge indigo">已配置</span>'
                    : '<span class="badge gray">未配置</span>'}</td>
                <td>
                    <div class="row-actions">
                        <button class="btn small" data-act="edit" data-id="${A.esc(u.id)}">编辑</button>
                        <button class="btn small danger" data-act="del" data-id="${A.esc(u.id)}">删除</button>
                    </div>
                </td>
            </tr>`).join("");

        empty.style.display = users.length ? "none" : "block";
    }

    /* ---------------- 弹窗 ---------------- */

    const dlg = document.getElementById("UserDialog");
    let editing = null;

    function openUserDialog(user) {
        editing = user || null;
        document.getElementById("UserDialogTitle").textContent =
            editing ? "编辑用户" : "新建用户";
        document.getElementById("UPasswordTip").textContent =
            editing ? "留空表示不修改密码" : "";
        const pwd = document.getElementById("UPassword");
        pwd.value = "";
        pwd.required = !editing;
        pwd.placeholder = editing ? "不修改请留空" : "";
        document.getElementById("UUsername").value = editing ? editing.username : "";
        document.getElementById("UEnabled").checked = editing ? !!editing.enabled : true;
        dlg.showModal();
    }

    document.getElementById("BtnAddUser").addEventListener("click", () => openUserDialog(null));
    document.getElementById("UserCancel").addEventListener("click", () => dlg.close());

    document.getElementById("UserForm").addEventListener("submit", async e => {
        e.preventDefault();
        const saveBtn = document.getElementById("UserSave");
        const username = document.getElementById("UUsername").value.trim();
        const password = document.getElementById("UPassword").value;
        const enabled = document.getElementById("UEnabled").checked;

        if (!editing && !password) {
            A.toast("请填写密码", "新建用户必须设置密码", "warn");
            return;
        }

        let res;
        AdminApp.busy(saveBtn, true);
        if (editing) {
            // 修改：password_hash 传明文（服务端 BCrypt），约定 "null" = 不修改
            res = await A.post("/api/revise_user", {
                id: editing.id,
                username,
                password_hash: password === "" ? "null" : password,
                enabled,
                token: A.token()
            }, "修改用户失败");
        } else {
            res = await A.post("/api/create_user", {
                token: A.token(),
                username,
                password,
                enabled
            }, "创建用户失败");
        }
        AdminApp.busy(saveBtn, false);

        if (res.ok) {
            A.toast(editing ? "用户已修改" : "用户已创建", username, "success");
            dlg.close();
            await load();
        } else if (res.data) {
            A.toast("操作失败", res.data.hint || "", "error");
        }
    });

    /* ---------------- 删除 ---------------- */

    document.getElementById("UserRows").addEventListener("click", async e => {
        const btn = e.target.closest("button[data-act]");
        if (!btn) return;
        const user = users.find(x => String(x.id) === btn.dataset.id);
        if (!user) return;

        if (btn.dataset.act === "edit") {
            openUserDialog(user);
            return;
        }

        const hasConfig = configs.some(x => x.user_id === user.id);
        const yes = await A.confirmDialog({
            title: "删除用户",
            message: `确定删除用户「${user.username}」吗？` +
                     (hasConfig ? "该用户的设备配置将被一并删除。" : "") +
                     "此操作不可撤销。",
            danger: true, okText: "删除"
        });
        if (!yes) return;

        const res = await A.post("/api/delete_user", {
            id: user.id,
            username: user.username,
            token: A.token()
        }, "删除用户失败");
        if (res.ok) {
            A.toast("用户已删除", user.username, "success");
            await load();
        } else if (res.data) {
            A.toast("删除失败", res.data.hint || "", "error");
        }
    });

    load();
})();
