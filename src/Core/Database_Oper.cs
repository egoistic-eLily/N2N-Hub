using LiteDB;
using N2N_USER_SERVER.API;
using N2N_USER_SERVER.Bootstrap;
using N2N_USER_SERVER.Core.Supernode_Parser;

namespace N2N_USER_SERVER.Core
{
    //用户账号文档模型
    public class User
    {
        [BsonId]
        public int id { get; set; }
        public string username { get; set; }
        public string password_hash { get; set; }
        public bool enabled { get; set; }
    }
    public class CommunityConfig
    {
        [BsonId]
        public int id { get; set; }
        public string name { get; set; }
        public Supernode_Parser.CommunityType type { get; set; }
        public string? network { get; set; }
        public string? communitykey { get; set; }
    }

    public class UserConfig
    {
        [BsonId]
        public int user_id { get; set; }
        public int community_id { get; set; }
        public string? device_name { get; set; }
        public string? key { get; set; }
        public string? password { get; set; }
        public string? supernode_ip {get;set;}
        public int? supernode_port {get;set;}
    }

    public class ClientUserConfig
    {
        public int user_id { get; set; }
        public string password {get;set;}
        public string? device_name { get; set; }
        public string community_name { get; set; }
        public string? communitykey { get; set; }
        public string key { get; set; }
        public string supernode_ip { get; set; }
        public int supernode_port { get; set; }
        public int encrypt_algorithm { get; set; }
    }

    //提供LiteDB实例的封装，IDisposable-可释放
    public class DatabaseService : IDisposable
    {
        private readonly LiteDatabase _database;

        //构造函数：初始化并建立索引
        public DatabaseService()
        {
            _database = new LiteDatabase(Initialization.config.userdbpath);
            var users = _database.GetCollection<User>("users");
            var communit = _database.GetCollection<CommunityConfig>("communityconfig");
            var userconfig = _database.GetCollection<UserConfig>("userconfig");
            communit.EnsureIndex(x => x.name, true);
            users.EnsureIndex(x => x.username, true);
            userconfig.EnsureIndex(x => x.device_name);
        }

        //users集合
        public ILiteCollection<User> Users
        {
            get
            {
                return _database.GetCollection<User>("users");
            }
        }

        //Community集合
        public ILiteCollection<CommunityConfig> CommunityConfig
        {
            get
            {
                return _database.GetCollection<CommunityConfig>("communityconfig");
            }
        }

        public ILiteCollection<UserConfig> UsersConfig
        {
            get
            {
                return _database.GetCollection<UserConfig>("userconfig");
            }
        }

        public void Dispose()
        {
            _database.Dispose();
        }
    }

    public static class DatabaseOper
    {
        #region API操作

        //数据库实例
        public static DatabaseService lite = new DatabaseService();

        //登录处理

        public static WriteResults LoginProcessing(string username, string inputPassword, out ClientUserConfig? userconfig)
        {
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"开始验证用户-{username}");
            int? userId = UserVerification(username, inputPassword, lite.Users);

            if (userId == null)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn, $"用户登录失败-{username}");
                userconfig = null;
                return new WriteResults()
                {
                    type = "LoginProcessing",
                    success = false,
                    recode = 401,
                    hint = "用户名或密码错误"
                };
            }

            var REDATA = new ClientUserConfig();

            var RE = GetUser_Config();

            if (!RE.success)
            {
                userconfig = null;
                return RE;
            }
            userconfig = REDATA;
            return RE;


            WriteResults GetUser_Config()
            {
                try
                {
                    var user = lite.UsersConfig.FindById(userId);
                    var Community = lite.CommunityConfig.FindById(user == null ? -1 : user.community_id);
                    if (user == null || Community == null)
                    {
                        return new WriteResults()
                        {
                            type = "LoginProcessing",
                            success = false,
                            recode = 401,
                            hint = "用户不存在或用户所属社区不存在"
                        };
                    }
                    REDATA.user_id = user.user_id;
                    REDATA.password = user.password;
                    REDATA.device_name = user.device_name;
                    REDATA.key = user?.key == null ? "" : user.key;
                    REDATA.communitykey = Community.communitykey;
                    REDATA.community_name = Community.name;
                    REDATA.encrypt_algorithm = 4;
                    //按用户覆盖超级节点地址/端口；未设置(null/空/历史行缺失字段)
                    //或显式为 -1 时，回退到全局 config.url 与 config.supernode_port
                    REDATA.supernode_ip = string.IsNullOrWhiteSpace(user.supernode_ip)
                        ? Initialization.config.url
                        : user.supernode_ip;
                    REDATA.supernode_port = (user.supernode_port == null)
                        ? Initialization.config.supernode_port
                        : user.supernode_port.Value;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"数据库错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "LoginProcessing",
                        success = false,
                        recode = 500,
                        hint = "数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"获取用户配置时 未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "LoginProcessing",
                        success = false,
                        recode = 500,
                        hint = "未知错误"
                    };
                }
                return new WriteResults()
                {
                    type = "LoginProcessing",
                    success = true,
                    recode = 200,
                    hint = "OK"
                };
            }


        }

        //验证账户，成功返回用户id
        private static int? UserVerification(string username, string inputPassword, ILiteCollection<User> users)
        {
            try
            {
                User? user = users.FindOne(x => x.username == username);

                if (user == null) return null;
                if (!user.enabled) return null;

                if (BCrypt.Net.BCrypt.Verify(inputPassword, user.password_hash))
                {
                    return user.id;
                }
                else
                {
                    return null;
                }
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"数据库错误-{ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"数据库-未知错误-{ex.Message}");
                return null;
            }
        }
        #endregion

        #region 后端管理

        //写操作的结果
        public class WriteResults
        {
            //操作类型
            public string type { get; set; }
            //是否操作成功
            public bool success { get; set; }
            //返回代码
            public int recode { get; set; }
            //其他提示
            public string hint { get; set; }
        }

        //插入用户
        public static WriteResults AddUser(string username, string password, bool enabled)
        {

            var RE = InsertData(lite.Users);
            if (RE.success)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户创建成功-{username}");
                return RE;
            }
            else
            {
                return RE;
            }

            WriteResults InsertData(ILiteCollection<User> users)
            {
                var user = new User()
                {
                    username = username,
                    password_hash = BCrypt.Net.BCrypt.HashPassword(password),
                    enabled = enabled
                };
                try
                {
                    if (users.FindOne(x => x.username == username) != null)
                    {
                        return new WriteResults()
                        {
                            type = "AddUser",
                            success = false,
                            recode = 401,
                            hint = $"该用户名已存在 - {username}"
                        };
                    }

                    users.Insert(user);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "AddUser",
                        success = false,
                        recode = 500,
                        hint = "插入用户时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "AddUser",
                        success = false,
                        recode = 500,
                        hint = "插入用户时 其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "AddUser",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
        }


        //新增社区
        public static WriteResults AddCommunity(CommunityConfig Community)
        {
            //仅null表示该项为空
            if (string.IsNullOrWhiteSpace(Community.network)) Community.network = null;
            if (string.IsNullOrWhiteSpace(Community.communitykey)) Community.communitykey = null;

            return InsertCommunity(lite.CommunityConfig);

            WriteResults InsertCommunity(ILiteCollection<CommunityConfig> configs)
            {
                try
                {
                    //请求的社区的id已存在或请求的社区name已存在
                    if (configs.FindById(Community.id) != null || configs.FindOne(x => x.name == Community.name) != null)
                    {
                        return new WriteResults()
                        {
                            type = "AddCommunity",
                            success = false,
                            recode = 401,
                            hint = "请求的社区的id已存在或请求的社区name已存在"
                        };
                    }

                    var _community = new CommunityConfig
                    {
                        name = Community.name,
                        type = Community.type,
                        network = Community.network == null ? null : Community.network,
                        communitykey = Community.communitykey == null ? null : Community.communitykey
                    };

                    configs.Insert(_community);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "AddCommunity",
                        success = false,
                        recode = 500,
                        hint = "新增社区时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "AddCommunity",
                        success = false,
                        recode = 500,
                        hint = "新增社区时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "AddCommunity",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };

            }
        }

        public static WriteResults AddUserConfig(UserConfig Users)
        {

            return InsertUserConfig(lite.UsersConfig);

            WriteResults InsertUserConfig(ILiteCollection<UserConfig> configs)
            {
                try
                {
                    //先看它是否在主用户表里存在
                    if (lite.Users.FindById(Users.user_id) == null)
                    {
                        return new WriteResults()
                        {
                            type = "AddUserConfig",
                            success = false,
                            recode = 401,
                            hint = "请求的用户不属于用户表中"
                        };
                    }
                    //用户所属社区不存在 或 用户id已存在 或 用户设备名已存在
                    if (lite.CommunityConfig.FindById(Users.community_id) == null || configs.FindById(Users.user_id) != null || configs.FindOne(x => x.device_name == Users.device_name) != null)
                    {
                        return new WriteResults()
                        {
                            type = "AddUserConfig",
                            success = false,
                            recode = 401,
                            hint = "请求所属社区不存在或用户id用户/设备名存在问题"
                        };
                    }

                    var _config = new UserConfig()
                    {
                        user_id = Users.user_id,
                        community_id = Users.community_id,
                        device_name = Users.device_name,
                        key = N2nKeygen.Core.N2nUserKey.Generate(Users.device_name, Users.password),
                        password = Users.password,
                        supernode_ip = Users.supernode_ip == null ? null : Users.supernode_ip,
                        supernode_port = Users.supernode_port == -1 ? null : Users.supernode_port
                    };

                    configs.Insert(_config);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "AddUserConfig",
                        success = false,
                        recode = 500,
                        hint = "新增用户时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "AddUserConfig",
                        success = false,
                        recode = 500,
                        hint = "新增用户时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "AddUserConfig",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
        }

        //修改用户数据
        public static WriteResults ReviseUser(User data)
        {
            //先判断用户是否存在
            if (!isUser(data.id))
            {
                return new WriteResults()
                {
                    type = "ReviseUser",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            if (ReviseData(lite.Users))
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户修改成功-id{data.id}");
                return new WriteResults()
                {
                    type = "ReviseUser",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "ReviseUser",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }

            bool ReviseData(ILiteCollection<User> users)
            {
                try
                {
                    var user = users.FindById(data.id);
                    user.username = data.username;
                    //约定密码为"null"时表示不修改
                    if (data.password_hash != "null")
                    {
                        user.password_hash = data.password_hash;
                    }
                    user.enabled = data.enabled;

                    users.Update(user);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"修改用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"修改用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid)
            {
                try
                {
                    var user = lite.Users.FindById(userid);
                    if (user == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }
        }

        //修改社区-仅允许修改网络和key
        public static WriteResults ReviseCommunity(CommunityConfig Community)
        {
            if (string.IsNullOrWhiteSpace(Community.network)) Community.network = null;
            if (string.IsNullOrWhiteSpace(Community.communitykey)) Community.communitykey = null;


            return Revise(lite.CommunityConfig);

            WriteResults Revise(ILiteCollection<CommunityConfig> configs)
            {
                try
                {
                    var _community = configs.FindOne(x => x.name == Community.name);
                    _community.network = Community?.network == null ? _community.network : Community.network;
                    _community.communitykey = Community?.communitykey == null ? _community.communitykey : Community.communitykey;

                    configs.Update(_community);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "ReviseCommunity",
                        success = false,
                        recode = 500,
                        hint = "修改社区时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "ReviseCommunity",
                        success = false,
                        recode = 500,
                        hint = "修改社区时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "ReviseCommunity",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
        }

        public static WriteResults Revise_UserConfig(UserConfig userConfig)
        {

            return (Revise(lite.UsersConfig));

            WriteResults Revise(ILiteCollection<UserConfig> user)
            {
                try
                {
                    var _user = user.FindOne(x => x.device_name == userConfig.device_name);
                    _user.device_name = userConfig.device_name;
                    _user.key = N2nKeygen.Core.N2nUserKey.Generate(userConfig.device_name, userConfig.password);
                    _user.community_id = userConfig.community_id;
                    _user.password = userConfig.password;
                    _user.supernode_ip = userConfig.supernode_ip == null ? null : userConfig.supernode_ip;
                    _user.supernode_port = userConfig.supernode_port == -1 ? null : userConfig.supernode_port;
                    user.Update(_user);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "Revise_UserConfig",
                        success = false,
                        recode = 500,
                        hint = "修改用户数据时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "Revise_UserConfig",
                        success = false,
                        recode = 500,
                        hint = "修改用户数据时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "Revise_UserConfig",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };

            }
        }



        //删除用户及其边缘配置
        public static WriteResults DeleteUser(int userid)
        {
            //先判断用户是否存在
            if (!isUser(userid))
            {
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            bool userDeleted = Delete(lite.Users);
            bool configDeleted = DeleteConfig();

            if (userDeleted && configDeleted)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户删除成功-id{userid}");
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else if (!userDeleted)
            {
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = false,
                    recode = 500,
                    hint = "User deletion failed"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = false,
                    recode = 500,
                    hint = "UserConfig deletion failed"
                };
            }

            bool Delete(ILiteCollection<User> users)
            {
                try
                {
                    users.Delete(userid);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"删除用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"删除用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool DeleteConfig()
            {
                return(DeleteUserData(userid).success);
            }

            bool isUser(int userid)
            {
                try
                {
                    var user = lite.Users.FindById(userid);
                    if (user == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }
        }

        //删除社区(只允许在无绑定用户的情况下)

        public static WriteResults Delete_Community(int id)
        {
            return Delete(lite.CommunityConfig);

            WriteResults Delete(ILiteCollection<CommunityConfig> community)
            {
                try
                {
                    //检测该社区是否有绑定的用户
                    if (lite.UsersConfig.FindOne(x => x.community_id == id) != null)
                    {
                        return new WriteResults()
                        {
                            type = "Delete_Community",
                            success = false,
                            recode = 500,
                            hint = "该社区仍然有绑定的用户"
                        };
                    }

                    community.Delete(id);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "Delete_Community",
                        success = false,
                        recode = 500,
                        hint = "删除社区时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "Delete_Community",
                        success = false,
                        recode = 500,
                        hint = "删除社区时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "Delete_Community",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
        }
        public static WriteResults Delete_Community(string Community_Name)
        {
            return Delete(lite.CommunityConfig);

            WriteResults Delete(ILiteCollection<CommunityConfig> community)
            {
                try
                {
                    //获取该社区的id
                    int? id = community.FindOne(x => x.name == Community_Name)?.id;

                    if (id == null)
                    {
                        return new WriteResults()
                        {
                            type = "Delete_Community",
                            success = false,
                            recode = 500,
                            hint = $"没有找到为:{Community_Name} 的社区"
                        };
                    }

                    //检测该社区是否有绑定的用户
                    if (lite.UsersConfig.FindOne(x => x.community_id == id) != null)
                    {
                        return new WriteResults()
                        {
                            type = "Delete_Community",
                            success = false,
                            recode = 500,
                            hint = "该社区仍然有绑定的用户"
                        };
                    }

                    community.Delete(id);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "Delete_Community",
                        success = false,
                        recode = 500,
                        hint = "删除社区时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "Delete_Community",
                        success = false,
                        recode = 500,
                        hint = "删除社区时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "Delete_Community",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
        }

        //删除用户配置

        public static WriteResults DeleteUserData(int id)
        {
            return Delete(lite.UsersConfig);

            WriteResults Delete(ILiteCollection<UserConfig> community)
            {
                try
                {
                    community.Delete(id);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "DeleteUserData",
                        success = false,
                        recode = 500,
                        hint = "删除用户配置时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "DeleteUserData",
                        success = false,
                        recode = 500,
                        hint = "删除用户配置时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "DeleteUserData",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
        }
        //
        public static WriteResults DeleteUserData(string username)
        {
            return Delete(lite.UsersConfig);

            WriteResults Delete(ILiteCollection<UserConfig> community)
            {
                try
                {
                    int? id = lite.UsersConfig.FindOne(x => x.device_name == username)?.user_id;
                    if (id == null)
                    {
                        return new WriteResults()
                        {
                            type = "Delete_Community",
                            success = false,
                            recode = 500,
                            hint = $"没有找到为:{username} 的用户 "
                        };
                    }
                    community.Delete(id);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "DeleteUserData",
                        success = false,
                        recode = 500,
                        hint = "删除用户配置时 数据库错误"
                    };
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return new WriteResults()
                    {
                        type = "DeleteUserData",
                        success = false,
                        recode = 500,
                        hint = "删除用户配置时  其他错误"
                    };
                }
                return new WriteResults()
                {
                    type = "DeleteUserData",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
        }

        //获取所有用户
        public static List<User> GetUserDataAll()
        {
            try
            {
                return lite.Users.FindAll().ToList();
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户列表失败-{ex.Message}");
                return new List<User>() { };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户列表失败-未知错误-{ex.Message}");
                return new List<User>() { };
            }
        }

        //获取所有社区
        public static WriteResults GetCommunityAll(out List<CommunityConfig> communityConfigs)
        {
            try
            {
                communityConfigs = lite.CommunityConfig.FindAll().ToList();
                return new WriteResults()
                {
                    type = "GetCommunityAll",
                    success = true,
                    recode = 200,
                    hint = "OK"
                };
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询社区失败-{ex.Message}");
                communityConfigs = null;
                return new WriteResults()
                {
                    type = "GetCommunityAll",
                    success = false,
                    recode = 500,
                    hint = "查询社区时 数据库错误 "
                };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询社区失败-未知错误-{ex.Message}");
                communityConfigs = null;
                return new WriteResults()
                {
                    type = "GetCommunityAll",
                    success = false,
                    recode = 500,
                    hint = "查询社区时 未知错误"
                };
            }

        }

        public static WriteResults Get_UserConfigAll(out List<UserConfig> UserConfig)
        {
            try
            {
                UserConfig = lite.UsersConfig.FindAll().ToList();
                return new WriteResults()
                {
                    type = "Get_UserConfigAll",
                    success = true,
                    recode = 200,
                    hint = "OK"
                };
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户数据失败-{ex.Message}");
                UserConfig = null;
                return new WriteResults()
                {
                    type = "Get_UserConfigAll",
                    success = false,
                    recode = 500,
                    hint = "查询用户配置时 数据库错误"
                };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户数据失败-未知错误-{ex.Message}");
                UserConfig = null;
                return new WriteResults()
                {
                    type = "Get_UserConfigAll",
                    success = false,
                    recode = 500,
                    hint = "询用户配置时 未知错误"
                };
            }
        }

        public static WriteResults SynchronousListFile()
        {
            var CommunityAll = lite.CommunityConfig.FindAll().ToList();
            var CommunityFile = new Supernode_Parser.CommunityFile();
            var CommunityList = new List<Supernode_Parser.Community>();
            var users = new List<Supernode_Parser.User>();
            try
            {
                foreach (var community in CommunityAll)
                {
                    //每次循环重置
                    users = new List<Supernode_Parser.User>();
                    //查询每个绑定至该社区的User
                    foreach (var user in lite.UsersConfig.Find(x => x.community_id == community.id))
                    {
                        users.Add(new Supernode_Parser.User()
                        {
                            Id = user.user_id,
                            username = user.device_name,
                            key = user.key
                        });
                    }
                    CommunityList.Add(new Supernode_Parser.Community
                    {
                        Name = community.name,
                        Type = community.type,
                        Network = community.network,
                        Users = users
                    });
                }

                CommunityFile.Communitys = CommunityList;

                var RE = Supernode_Parser.Parser.SerializationCommunityFile(CommunityFile);

                if (!RE.success)
                {
                    return new WriteResults()
                    {
                        type = RE.type,
                        success = false,
                        recode = RE.recode,
                        hint = RE.hint
                    };
                }

                return new WriteResults()
                {
                    type = RE.type,
                    success = true,
                    recode = RE.recode,
                    hint = RE.hint
                };
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"将数据库同步至LIST文件失败-{ex.Message}");
                return new WriteResults()
                {
                    type = "SynchronousListFile",
                    success = false,
                    recode = 500,
                    hint = "将数据库同步至LIST文件失败 数据库错误"
                };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"将数据库同步至LIST文件失败-未知错误-{ex.Message}");
                return new WriteResults()
                {
                    type = "SynchronousListFile",
                    success = false,
                    recode = 500,
                    hint = "将数据库同步至LIST文件失败 未知错误"
                };
            }

        }
        #endregion
    }
}