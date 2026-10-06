/* 管理员登录页 */
(async () => {
    const form = document.getElementById("LoginForm");
    const errBox = document.getElementById("LoginError");
    const btn = document.getElementById("LoginBtn");

    // 已有会话时先校验其有效性再自动进入控制台。
    // 服务端重启后内存令牌会全部失效，旧 token 必须清掉，否则会死循环。
    const saved = sessionStorage.getItem("token");
    if (saved) {
        try {
            const resp = await fetch("/api/getuserdata", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ RequestType: "check", token: saved })
            });
            const data = await resp.json().catch(() => null);
            if (resp.ok && data && data.success === true) {
                location.replace("/admin?token=" + encodeURIComponent(saved));
                return;
            }
        } catch (_) { /* 服务不可达时停留在登录页 */ }
        // 令牌无效：清除，正常显示登录表单
        sessionStorage.removeItem("token");
    }

    form.addEventListener("submit", async e => {
        e.preventDefault();
        errBox.classList.remove("show");

        const username = document.getElementById("username").value.trim();
        const password = document.getElementById("password").value;

        if (!username || !password) {
            showErr("请输入用户名和密码");
            return;
        }

        AdminApp.busy(btn, true);
        let resp, data;
        try {
            resp = await fetch("/api/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });
            data = await resp.json();
        } catch (_) {
            showErr("无法连接到服务器");
            AdminApp.busy(btn, false);
            return;
        }
        AdminApp.busy(btn, false);

        if (resp.ok && data.success === true && data.token) {
            sessionStorage.setItem("token", data.token);
            location.href = "/admin?token=" + encodeURIComponent(data.token);
        } else {
            showErr("用户名或密码错误");
        }
    });

    function showErr(msg) {
        errBox.textContent = msg;
        errBox.classList.add("show");
    }
})();
