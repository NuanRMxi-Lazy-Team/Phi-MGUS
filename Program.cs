using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Fleck;
using System.Security.Cryptography.X509Certificates;
using static Phi_MGUS.GameManager;

namespace Phi_MGUS;

public class Program
{
    public static Config config = new()
    {
        isDebug = true
    };

    private const string Version = "Release 0.0.0-Build 00004";

    private static void Main(string[] args)
    {
        LogManager.WriteLog("Phi-MGUS " + Version + " | " + "Starting...");
        //Load config | 读取配置
        if (File.Exists("config.json"))
        {
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"))!;
            if (config.isDebug)
            {
                LogManager.WriteLog("Debug mode is enabled.", LogManager.LogLevel.Warning);
            }
        }
        else
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
            LogManager.JustConsoleWrite(
                "We have generated a default configuration file. If modifications are needed, please exit the program and modify \"config.json\", and then run it again.",
                LogManager.LogLevel.Warning);
        }

        //Check private mode | 检测是否为私有模式
        if (config.isPrivate && string.IsNullOrEmpty(config.Password))
        {
            LogManager.WriteLog("The server is private, but the password is empty.", LogManager.LogLevel.Warning);
        }

        //Start WebSocket Server | 启动 WebSocket 服务器
        WebSocketServer wssserver = new WebSocketServer($"wss://{config.wssOptions.Host}:{config.wssOptions.Port}");
        WebSocketServer wsserver = new WebSocketServer($"ws://{config.wsOptions.Host}:{config.wsOptions.Port}");
        //set cert | 设置证书
        if (!string.IsNullOrEmpty(config.certPath) && config.wss)
        {
            try
            {
                // set cert
                wssserver.Certificate = new X509Certificate2(config.certPath, config.certPassword);
                wssserver.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                                System.Security.Authentication.SslProtocols.Tls13;
            }
            catch (Exception e)
            {
                LogManager.WriteLog("Secure connection is not enabled or the certificate is invalid.\n" + e.Message,
                    LogManager.LogLevel.Warning);
                wssserver =
                    new WebSocketServer($"ws://{config.wssOptions.Host}:{config.wssOptions.Port}");
            }
        }
        else
        {
            LogManager.WriteLog("Secure connection is not enabled or the certificate is invalid.",
                LogManager.LogLevel.Warning);
            config.wss = false;
        }

        //change Fleck Log Level | 设置 Fleck 日志等级
        FleckLog.Level = LogLevel.Error;

        if (config.wss)
        {
            wssserver.Start(socket =>
            {
                socket.OnOpen += async () => await ServerOnOpen(socket);
                socket.OnMessage += async message => await ServerOnMessage(message, socket);
                socket.OnError += async e => await ServerOnError(e, socket);
                socket.OnClose += async () => await ServerOnClose(socket);

                socket.OnBinary += bytes =>
                {
                    _ = bytes;
                    LogManager.WriteLog("illegal Binary data received, Drop.", LogManager.LogLevel.Debug);

                    //Drop and disconnect | 断开连接
                    UserManager.RemoveUser(socket);
                    socket.Close();
                };
            });
        }

        if (config.ws)
        {
            wsserver.Start(socket =>
            {
                socket.OnOpen += async () => await ServerOnOpen(socket);
                socket.OnMessage += async message => await ServerOnMessage(message, socket);
                socket.OnError += async e => await ServerOnError(e, socket);
                socket.OnClose += async () => await ServerOnClose(socket);

                socket.OnBinary += bytes =>
                {
                    _ = bytes;
                    LogManager.WriteLog("illegal Binary data received, Drop.", LogManager.LogLevel.Debug);

                    //Drop and disconnect | 断开连接
                    UserManager.RemoveUser(socket);
                    socket.Close();
                };
            });
        }

        if (!config.ws && !config.wss)
        {
            throw new Exception("No server is enabled.");
        }

        if (config.ws && config.wss)
        {
            LogManager.WriteLog(
                "Is it expected that both secure and insecure connections will be enabled simultaneously?",
                LogManager.LogLevel.Warning
            );
        }

        //LogManager.WriteLog("The server has been started");
        if (config.ws)
        {
            LogManager.WriteLog($"The server is listening on ws://{config.wsOptions.Host}:{config.wsOptions.Port}");
        }

        if (config.wss)
        {
            LogManager.WriteLog($"The server is listening on wss://{config.wssOptions.Host}:{config.wssOptions.Port}");
        }

        while (true)
        {
            var command = Console.ReadLine();
            var commands = command.Split(' ');

            try
            {
                if (commands[0] == "user")
                {
                    if (commands[1] == "list")
                    {
                        //print all user | 输出所有用户
                        LogManager.WriteLog("User List:");
                        foreach (var user in UserManager.UserList)
                        {
                            LogManager.WriteLog(
                                $"{user.name} Joined at {user.joinTime.ToString("yyyy-mm-dd hh:mm:ss")} {user.room?.roomID ?? "No Room"}");
                        }
                    }
                }

                if (commands[0] == "room")
                {
                    if (commands[1] == "list")
                    {
                        LogManager.WriteLog("Room List:");
                        foreach (var room in RoomManager.RoomList)
                        {
                            LogManager.WriteLog($"{room.roomID} Owner: {room.owner.name} User Count: {room.UserCount}");
                        }
                    }
                    else if (RoomManager.GetRoom(commands[1]) != null)
                    {
                        //print room info | 输出房间信息
                        var room = RoomManager.GetRoom(commands[1]);
                        LogManager.WriteLog(
                            $"Room Info: {room.roomID} Owner: {room.owner.name} User Count: {room.UserCount}");
                        LogManager.WriteLog("Member:");
                        for (int i = 0; i < room.UserCount; i++)
                        {
                            LogManager.WriteLog(
                                "Name: " + room[i].name + (room.owner == room[i] ? " (Owner)" : ""));
                        }
                    }
                }
            }
            catch
            {
                LogManager.WriteLog("Invalid command, please check the command again.", LogManager.LogLevel.Warning);
            }
        }
    }

    /// <summary>
    /// Server On Message | 服务器收到消息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="socket"></param>
    private static async Task ServerOnMessage(string message, IWebSocketConnection socket)
    {
        //Attempt serialization, if serialization fails, discard directly | 序列化失败直接丢弃
        try
        {
            JObject.Parse(message);
        }
        catch
        {
            LogManager.WriteLog("illegal data received, Drop.", LogManager.LogLevel.Warning);
            //Drop | 丢弃并断开连接
            if (UserManager.Contains(socket))
                UserManager.RemoveUser(socket);
            socket.Close();
            return;
        }

        var msg = JsonConvert.DeserializeObject<ConnectionMessage.Message>(message)!;
        if (msg.action == "clientMetaData")
        {
            //to ConnectionMessage.Client.ClientMetaData | 将客户端发送的消息转换为 ConnectionMessage.Client.ClientMetaData
            var clientMetaData =
                JsonConvert.DeserializeObject<ConnectionMessage.Client.ClientMetaData>(message)!;
            if (UserManager.Contains(socket))
            {
                //illegal client, Drop it | 非法客户端，丢弃并断开连接
                LogManager.WriteLog("illegal client, Drop it.", LogManager.LogLevel.Warning);
                await socket.Send(new ConnectionMessage.Server.JoinServerFailed
                {
                    reason = ConnectionMessage.Server.JoinServerFailed.ReasonType
                        .JoinFailedByIllegalClient //客户端已经连接服务器，您的连接将被关闭。
                }.Serialize());
                LogManager.WriteLog(
                    $"There are illegal clients attempting to pass metadata multiple times: {UserManager.GetUser(socket).name}");
                UserManager.GetUser(socket).Disconnect();
                return;
            }

            var data = clientMetaData.data;
            if (config.isPrivate)
            {
                if (string.IsNullOrEmpty(data.password) || data.password != config.Password)
                {
                    if (string.IsNullOrEmpty(data.password))
                    {
                        await socket.Send(new ConnectionMessage.Server.JoinServerFailed
                        {
                            reason =
                                ConnectionMessage.Server.JoinServerFailed.ReasonType
                                    .AuthFailedByPwdNull //服务器是私有的，但提供的密码为空。您的连接将被关闭。
                        }.Serialize());
                    }
                    else
                    {
                        await socket.Send(new ConnectionMessage.Server.JoinServerFailed
                        {
                            reason =
                                ConnectionMessage.Server.JoinServerFailed.ReasonType
                                    .AuthFailedByPwdIncorrect //服务器是私有的，但提供的密码无效。您的连接将被关闭。
                        }.Serialize());
                    }

                    LogManager.WriteLog(
                        $"The client attempted to connect to the server, but authentication failed: {data.userName}");
                    //Authentication failed, Drop and disconnect | 认证失败，丢弃并断开连接
                    UserManager.GetUser(socket).Disconnect();
                    return;
                }
            }

            // Add user | 添加用户
            LogManager.WriteLog($"User {clientMetaData.data.userName} connected.");
            LogManager.WriteLog($"Raw Message: {message}", LogManager.LogLevel.Debug);
            //Check all parameters | 检查所有参数
            if (data.isDebugger == null || data.isSpectator == null || data.features == null)
            {
                await socket.Send(new ConnectionMessage.Server.JoinServerFailed
                {
                    reason = ConnectionMessage.Server.JoinServerFailed.ReasonType
                        .JoinFailedByInvalidParameter //非法参数，请检查后重试。
                }.Serialize());
                return;
            }

            UserManager.AddUser(clientMetaData.data.userName, socket, new User.UserConfig
            {
                isAnonymous = data.userName == null,
                isDebugger = data.isDebugger,
                isSpectator = data.isSpectator,
                featureSupport = data.features
            });
            await socket.Send(new ConnectionMessage.Server.JoinServerSuccess().Serialize());
            return;
        }

        // Check if the user has completed the return of metadata | 检查用户是否已经返回元数据
        if (!UserManager.Contains(socket))
        {
            socket.Close();
            LogManager.WriteLog("illegal client, Drop it.", LogManager.LogLevel.Warning);
            return;
        }

        if (msg.action == "newRoom")
        {
            // To ConnectionMessage.Client.NewRoom | 将客户端发送的消息转换为 ConnectionMessage.Client.NewRoom
            ConnectionMessage.Client.NewRoom newRoom =
                JsonConvert.DeserializeObject<ConnectionMessage.Client.NewRoom>(message)!;
            var user = UserManager.GetUser(socket);
            if (user.status == User.Status.InRoom)
            {
                await socket.Send(new ConnectionMessage.Server.NewRoomFailed
                {
                    reason = ConnectionMessage.Server.NewRoomFailed.ReasonType.AlreadyInRoom // 你已经在房间里了。 
                }.Serialize());
                LogManager.WriteLog($"User {user.name} tried to create a room while in a room."); // 用户尝试在房间里创建房间。
                return;
            }

            var room = RoomManager.GetRoom(newRoom.data.roomID);
            if (room != null)
            {
                await socket.Send(new ConnectionMessage.Server.NewRoomFailed
                {
                    reason = ConnectionMessage.Server.NewRoomFailed.ReasonType.RoomAlreadyExists // 房间已存在。
                }.Serialize());
                LogManager.WriteLog(
                    $"User {user.name} tried to create a room with the same name."); // 用户尝试使用相同的名称创建房间。
                return;
            }

            try
            {
                RoomManager.AddRoom(user, newRoom.data.roomID, newRoom.data.maxUser);
                await socket.Send(new ConnectionMessage.Server.NewRoomSuccess().Serialize());
                LogManager.WriteLog($"User {user.name} has created a new room {user.room!.roomID}.");
            }
            catch (ArgumentException e)
            {
                await socket.Send(new ConnectionMessage.Server.NewRoomFailed
                {
                    reason = ConnectionMessage.Server.NewRoomFailed.ReasonType.RoomIdentifierInvalid // 房间名不合法。
                }.Serialize());
                LogManager.WriteLog($"User {user.name} tried to create a room with an illegal name: {e.Message}");
            }
            catch (Exception e)
            {
                await socket.Send(new ConnectionMessage.Server.NewRoomFailed().Serialize());
                // Failed to create room | 创建房间失败
                LogManager.WriteLog($"User {user.name} tried to create a room, but failed: {e.Message}",
                    LogManager.LogLevel.Error);
            }
        }

        if (msg.action == "joinRoom")
        {
            // To ConnectionMessage.Client.JoinRoom | 将客户端发送的消息转换为 ConnectionMessage.Client.JoinRoom
            ConnectionMessage.Client.JoinRoom joinRoom =
                JsonConvert.DeserializeObject<ConnectionMessage.Client.JoinRoom>(message)!;
            var user = UserManager.GetUser(socket);
            if (user.status == User.Status.InRoom)
            {
                await socket.Send(new ConnectionMessage.Server.JoinRoomFailed
                {
                    reason = ConnectionMessage.Server.JoinRoomFailed.ReasonType.AlreadyInRoom // 你已经在房间里了。 
                }.Serialize());
                LogManager.WriteLog($"User {user.name} tried to join a room while in a room."); // 用户尝试在房间里加入房间。
                return;
            }

            //Find the room | 查找房间
            var room = RoomManager.GetRoom(joinRoom.data.roomID);
            if (room != null)
            {
                if (room.UserCount ! < room.maxUser)
                {
                    await socket.Send(new ConnectionMessage.Server.JoinRoomFailed
                    {
                        reason = ConnectionMessage.Server.JoinRoomFailed.ReasonType.RoomIsFull // 房间不存在。
                    }.Serialize());
                    LogManager.WriteLog($"User {user.name} tried to join a full room."); // 用户尝试加入不存在的房间。
                }
                else
                {
                    room.Join(user);
                    LogManager.WriteLog($"User {user.name} has joined the {room.roomID} room.");
                    await socket.Send(new ConnectionMessage.Server.JoinRoomSuccess
                    {
                        data =
                        {
                            roomID = room.roomID,
                            userList = room.userList.Select(u => u.name).ToList(),
                            chartMD5 = room.chartInfo.MD5,
                            chartUrl = room.chartInfo.url,
                            roomOwner = room.owner.name
                        }
                        
                    }.Serialize());
                }
            }
            else
            {
                await socket.Send(new ConnectionMessage.Server.JoinRoomFailed
                {
                    reason = ConnectionMessage.Server.JoinRoomFailed.ReasonType.RoomNotFound // 房间不存在。
                }.Serialize());
                LogManager.WriteLog($"User {user.name} tried to join a room that does not exist."); // 用户尝试加入不存在的房间。
            }

            return;
        }

        if (msg.action == "leaveRoom")
        {
            var user = UserManager.GetUser(socket);
            if (user.status == User.Status.InRoom)
            {
                user.room!.Leave(user);
                LogManager.WriteLog($"User {user.name} has left the {user.room!.roomID} room.");
                await socket.Send(new ConnectionMessage.Server.LeaveRoomSuccess().Serialize());
            }
            else
            {
                await socket.Send(new ConnectionMessage.Server.LeaveRoomFailed
                {
                    reason = ConnectionMessage.Server.LeaveRoomFailed.ReasonType.NotInRoom // 你不在房间里。
                }.Serialize());
                LogManager.WriteLog($"User {user.name} tried to leave a room while not in a room."); // 用户尝试离开房间，但未在房间里。
            }
        }

        if (msg.action == "selectChart")
        {
            var user = UserManager.GetUser(socket);
            var data = JsonConvert.DeserializeObject<ConnectionMessage.Client.SelectChart>(message)!.data;
            if (user.status == User.Status.InRoom)
            {
                var room = user.room!;
                if (room.owner == user)
                {
                    //Select chart | 选择谱面
                    room.chartInfo = new Room.ChartInfo(data.chartMD5, data.chartUrl);
                    LogManager.WriteLog(
                        $"User {user.name} has selected a chart {data.chartMD5} in the {room.roomID} room.");
                }
                else
                {
                    await socket.Send(new ConnectionMessage.Server.SelectChartFailed
                    {
                        reason = ConnectionMessage.Server.SelectChartFailed.ReasonType.InsufficientPermissions // 权限不足。
                    }.Serialize());
                    LogManager.WriteLog(
                        $"User {user.name} tried to select a chart while not being the owner of the room.");
                }
            }
            else
            {
                await socket.Send(new ConnectionMessage.Server.SelectChartFailed
                {
                    reason = ConnectionMessage.Server.SelectChartFailed.ReasonType.NotInRoom // 你不在房间里。
                }.Serialize());
                LogManager.WriteLog($"User {user.name} tried to select a chart while not in a room.");
            }
        }

        if (msg.action == "gameStart")
        {
            var user = UserManager.GetUser(socket);
            if (user.status != User.Status.InRoom)
            {
                //Not in room, do not do anything | 不在房间里，不做任何操作
                return;
            }
            var room = user.room!;
            if (room.owner != user)
            {
                //Insufficient permissions,do not do anything | 权限不足，不做任何操作
                //Let the front end judge for itself | 让前端自己判断
                return;
            }
            else
            {
                if (room.chartInfo != null)
                {
                    room.GameStart();
                }
                else
                {
                    //No chart selected, do not do anything | 未选择谱面，不做任何操作
                    //Let the front end judge for itself | 让前端自己判断
                }
                return;
            }
        }
    }

    private static async Task ServerOnOpen(IWebSocketConnection socket)
    {
        // Send GetData message, get metadata. | 发送 GetData 消息，获取元数据
        await socket.Send(
            new ConnectionMessage.Server.GetData
            {
                needPassword = config.isPrivate
            }.Serialize()
        );
    }

    private static async Task ServerOnError(Exception e, IWebSocketConnection socket)
    {
        LogManager.WriteLog(e.Message, LogManager.LogLevel.Debug);
        //WriteLog, Socket has been drop | 输出日志，连接被丢弃
        LogManager.WriteLog("Socket has been drop, because of an error: " + e.Message, LogManager.LogLevel.Debug);
        LogManager.WriteLog($"User {UserManager.GetUser(socket).name} unexpectedly disconnected",
            LogManager.LogLevel.Warning);

        //Drop and disconnect | 丢弃并断开连接
        UserManager.GetUser(socket).Disconnect();
        socket.Close();
    }

    private static async Task ServerOnClose(IWebSocketConnection socket)
    {
        LogManager.WriteLog($"user{UserManager.GetUser(socket).name} disconnected.");
        UserManager.GetUser(socket).Disconnect();
    }

    /// <summary>
    /// Server configuration file | 服务器配置文件
    /// </summary>
    public class Config
    {
        public bool isPrivate = false;
        public bool RoomChat = false;
        public bool isDebug = false;
        public string Password = "";
        public bool ws = true;
        public bool wss = false;

        public dynamic wsOptions = new
        {
            Host = "0.0.0.0",
            Port = 14156
        };

        public dynamic wssOptions = new
        {
            Host = "0.0.0.0",
            Port = 14157
        };

        public string certPath = "path/to/your/certificate.pfx";
        public string certPassword = "your_certificate_password";
        public string userDefauletAvatarUrl = "";
    }

    /// <summary>
    /// Broadcast message to all clients | 广播消息给所有客户端
    /// </summary>
    /// <param name="message"></param>
    public static void Broadcast(string message)
    {
        //Broadcast message to all clients
        foreach (var user in UserManager.UserList)
        {
            user.userSocket.Send(message);
        }
    }
}