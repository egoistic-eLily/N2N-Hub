/* ============================================================
   N2N User Server — 管理控制台公共脚本
   ============================================================ */
window.AdminApp = (() => {

    const TOKEN_KEY = "token";

    /* ---------------- 令牌 ---------------- */

    function token() {
        return sessionStorage.getItem(TOKEN_KEY);
    }

    // 页面初始化：优先接受 ?token=（服务端页面路由要求），存入 sessionStorage。
    // 注意：服务端页面鉴权只认查询串里的 token，因此地址栏保留 ?token=
    // 以支持 F5 刷新（不清洗 URL）。无可用令牌则回登录页。
    function initPage(active) {
        const qs = new URLSearchParams(location.search);
        const fromUrl = qs.get("token");
        if (fromUrl) {
            sessionStorage.setItem(TOKEN_KEY, fromUrl);
        }
        if (!token()) {
            location.href = "/admin_login";
            return false;
        }
        buildNav(active);
        return true;
    }

    function buildNav(active) {
        const t = encodeURIComponent(token());
        document.querySelectorAll("#Nav a[data-href]").forEach(a => {
            a.href = a.dataset.href + "?token=" + t;
            if (a.dataset.nav === active) a.classList.add("active");
        });
        const logoutBtn = document.getElementById("LogoutBtn");
        if (logoutBtn) logoutBtn.addEventListener("click", doLogout);
    }

    async function doLogout() {
        try {
            await fetch("/api/logout", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ RequestType: "logout", token: token() })
            });
        } catch (_) { /* 网络失败也照常登出 */ }
        sessionStorage.removeItem(TOKEN_KEY);
        location.href = "/admin_login";
    }

    /* ---------------- 请求 ---------------- */

    // 统一 POST；返回 { ok, status, data }。
    // recode 2000（会话过期）→ 清令牌跳超时页；网络异常 → toast。
    async function post(path, payload, defaultHint = "操作失败") {
        let resp, data;
        try {
            resp = await fetch(path, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            data = await resp.json();
        } catch (e) {
            toast("网络错误", "无法连接到服务器或响应不是有效数据", "error");
            return { ok: false, status: 0, data: null };
        }

        if (data && data.recode === 2000) {
            sessionStorage.removeItem(TOKEN_KEY);
            location.href = "/timeout.html";
            return { ok: false, status: resp.status, data };
        }

        const ok = resp.ok && data && data.success === true;
        return { ok, status: resp.status, data };
    }

    /* ---------------- Toast ---------------- */

    function ensureToastBox() {
        let box = document.querySelector(".toast-box");
        if (!box) {
            box = document.createElement("div");
            box.className = "toast-box";
            document.body.appendChild(box);
        }
        return box;
    }

    function toast(title, hint = "", type = "info", stayMs = 4200) {
        const el = document.createElement("div");
        el.className = "toast " + type;
        el.innerHTML =
            '<div><div class="t-title"></div>' +
            (hint ? '<div class="t-hint"></div>' : "") +
            '</div>';
        el.querySelector(".t-title").textContent = title;
        if (hint) el.querySelector(".t-hint").textContent = hint;
        ensureToastBox().appendChild(el);
        setTimeout(() => el.remove(), stayMs);
    }

    /* ---------------- 弹窗 ---------------- */

    // 页面需包含 id=ConfirmDialog 的 <dialog>；confirm() 返回 Promise<boolean>
    function confirmDialog({ title = "确认操作", message = "", danger = false, okText = "确认" }) {
        const dlg = document.getElementById("ConfirmDialog");
        if (!dlg) return Promise.resolve(false);
        return new Promise(resolve => {
            dlg.querySelector(".confirm-msg").textContent = message;
            const okBtn = dlg.querySelector("#ConfirmOk");
            okBtn.textContent = okText;
            okBtn.className = "btn " + (danger ? "solid-danger" : "primary");
            const done = val => { dlg.close(); resolve(val); };
            okBtn.onclick = () => done(true);
            dlg.querySelector("#ConfirmCancel").onclick = () => done(false);
            dlg.oncancel = () => resolve(false);
            dlg.showModal();
        });
    }

    function openDialog(id) {
        const dlg = document.getElementById(id);
        if (dlg) dlg.showModal();
        return dlg;
    }

    /* ---------------- 工具 ---------------- */

    function esc(v) {
        return String(v ?? "").replace(/[&<>"']/g,
            c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
    }

    // 布尔 → 状态徽标
    function boolBadge(v, onText = "启用", offText = "禁用") {
        return v
            ? '<span class="badge green">' + onText + "</span>"
            : '<span class="badge gray">' + offText + "</span>";
    }

    // 掩码展示：默认显示 ••••，点击眼睛切换；原始值存于 dataset
    function secretCell(raw) {
        if (raw === null || raw === undefined || raw === "") {
            return '<span class="badge gray">未设置</span>';
        }
        const safe = esc(raw);
        return '<span class="secret" data-raw="' + safe.replace(/"/g, "&quot;") + '">' +
               '<span class="val">••••••••</span>' +
               '<button type="button" title="显示/隐藏">👁</button></span>';
    }

    // 事件委托：全页的掩码切换按钮
    document.addEventListener("click", e => {
        const btn = e.target.closest(".secret button");
        if (!btn) return;
        const wrap = btn.closest(".secret");
        const val = wrap.querySelector(".val");
        if (val.dataset.shown === "1") {
            val.textContent = "••••••••";
            val.dataset.shown = "";
        } else {
            val.textContent = wrap.dataset.raw;
            val.dataset.shown = "1";
        }
    });

    // 按 value 禁用/启用按钮
    function busy(btn, on) {
        if (btn) btn.disabled = on;
    }

    // CommunityType 数字 → 展示徽标（_regex=0, _fixed=1）
    function communityTypeBadge(t) {
        return t === 1
            ? '<span class="badge indigo">固定名</span>'
            : '<span class="badge gray">正则</span>';
    }

    return {
        token, initPage, post, toast,
        confirmDialog, openDialog,
        esc, boolBadge, secretCell, busy, communityTypeBadge
    };
})();
