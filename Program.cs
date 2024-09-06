using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Fleck;
using System.Security.Cryptography.X509Certificates;

namespace Phi_MGUS;

public class Program
{
    public static Config config = new()
    {
        isDebug = true
    };

    //private static GameManager.ClientList clients = new();
    //private static GameManager.UserList users = new();


    private static void Main(string[] args)
    {
        if (File.Exists("config.json"))
        {
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"))!;
        }
        else
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
            LogManager.JustConsoleWrite(
                "We have generated a default configuration file. If modifications are needed, please exit the program and modify \"config.json\", and then run it again.",
                LogManager.LogLevel.Warning);
        }

        //Check private mode
        if (config.isPrivate && string.IsNullOrEmpty(config.Password))
        {
            LogManager.WriteLog("The server is private, but the password is empty.", LogManager.LogLevel.Warning);
        }

        //Start WebSocket Server
        WebSocketServer wssserver = new WebSocketServer($"ws://{config.wssOptions.Host}:{config.wssOptions.Port}");
        WebSocketServer wsserver = new WebSocketServer($"ws://{config.wsOptions.Host}:{config.wsOptions.Port}");
        //set cert
        if (!string.IsNullOrEmpty(config.certPath) && config.wss)
        {
            try
            {
                // set cert
                wssserver.Certificate = new X509Certificate2(config.certPath, config.certPassword);
            }
            catch (Exception e)
            {
                LogManager.WriteLog("Secure connection is not enabled or the certificate is invalid.\n" + e.Message,
                    LogManager.LogLevel.Warning);
                wssserver =
                    new WebSocketServer($"ws://{config.wssOptions.Host}:{config.wssOptions.Prot}");
            }
        }
        else
        {
            LogManager.WriteLog("Secure connection is not enabled or the certificate is invalid.",
                LogManager.LogLevel.Warning);
            config.wss = false;
        }

        //change Fleck Log Level
        FleckLog.Level = LogLevel.Error;
        //对两个server的是否开启进行判断，依次赋值给server并启动

        WebSocketServer server = null;

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
                    clients.Drop(socket);
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
                    clients.Drop(socket);
                    socket.Close();
                };
            });
        }

        if (!config.ws && !config.wss)
        {
            throw new Exception("No server is enabled.");
        }

        // 服务器已启动并同时使用了ws和wss或服务器已启动并只使用了某一个
        
        LogManager.WriteLog("The server has been started");
        while (true)
        {
            string command = Console.ReadLine();
            if (command == "clientlist")
            {
                //print all client
                LogManager.WriteLog("Client List:");
                foreach (var client in clients)
                {
                    LogManager.WriteLog($"{client.token} - {client.user?.userName ?? "null"}");
                }
            }
            else if (command == "userlist")
            {
                //print all user
                LogManager.WriteLog("User List:");
                foreach (var user in users)
                {
                    LogManager.WriteLog($"{user.userName} - {user.Client.token}");
                }
            }
        }
    }

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
            clients.Drop(socket);
            socket.Close();
            return;
        }

        var msg = JsonConvert.DeserializeObject<ConnectionMessage.Message>(message)!;
        if (msg.action == "clientMetaData")
        {
            //to ConnectionMessage.Client.ClientMetaData | 将客户端发送的消息转换为 ConnectionMessage.Client.ClientMetaData
            var clientMetaData =
                JsonConvert.DeserializeObject<ConnectionMessage.Client.ClientMetaData>(message)!;
            if (clients.GetClientAsToken(msg.token) != null)
            {
                //illegal client, Drop it | 非法客户端，丢弃并断开连接
                LogManager.WriteLog("illegal client, Drop it.", LogManager.LogLevel.Warning);
                await socket.Send(JsonConvert.SerializeObject(new ConnectionMessage.Server.JoinServerFailed()
                {
                    reason = "The client is already connected to the server.\nYour connection will be closed.",
                    token = clientMetaData.token
                }));
                clients.Drop(socket);
                socket.Close();
                return;
            }

            var data = (dynamic)clientMetaData.data;
            ConnectionMessage.Client.FeatureSupport featSup = data.features;
            if (config.isPrivate)
            {
                if (string.IsNullOrEmpty(data.password) || data.password != config.Password)
                {
                    await socket.Send(JsonConvert.SerializeObject(new ConnectionMessage.Server.JoinServerFailed()
                    {
                        reason =
                            "The server is private, but no password was provided or the password is invalid.\nYour connection will be closed.",
                        token = clientMetaData.token
                    }));
                    //Authentication failed, Drop and disconnect | 认证失败，丢弃并断开连接
                    clients.Drop(clientMetaData.token);
                    socket.Close();
                }
            }

            LogManager.WriteLog($"Client {clientMetaData.token} connected.");
            LogManager.WriteLog($"Raw Message: {message}", LogManager.LogLevel.Debug);
            clients.Join(new GameManager.Client
            {
                socket = socket,
                token = clientMetaData.token,
                featureSupport = GameManager.Client.FeatureSupport.ToGameManagerFeatureSupport(featSup)
            });
            await socket.Send(JsonConvert.SerializeObject(new ConnectionMessage.Server.JoinServerSuccess
            {
                token = clientMetaData.token
            }));
        }
        else if (msg.action == "register")
        {
            var regData = JsonConvert.DeserializeObject<ConnectionMessage.Client.Register>(message)!;
            if (clients.GetClientAsSocket(socket) != null)
            {
                //TODO: add client registered event | 添加客户端注册事件
                GameManager.User user = new GameManager.User
                {
                    Client = clients.GetClientAsSocket(socket)!,
                    userName = ((dynamic)regData.data).userName,
                    isAnonymous = ((dynamic)regData.data).isAnonymous,
                    isDebugger = ((dynamic)regData.data).isDebugger,
                    isSpectator = ((dynamic)regData.data).isSpectator
                };
                if (user.isDebugger || user.isSpectator)
                {
                    if (((dynamic)regData.data).authentication != "nan")
                    {
                        LogManager.WriteLog(
                            $"Client {regData.token} failed to register as {user.userName},because authentication failed.");
                        //Send RegisterFailed and reason | 发送注册失败以及原因消息
                        await socket.Send(JsonConvert.SerializeObject(new ConnectionMessage.Server.RegisterFailed
                        {
                            reason = "Authentication failed" //鉴权失败
                        }));
                        return;
                    }
                }

                if (users.Register(user))
                {
                    clients.GetClientAsSocket(socket)!.user = user;
                    LogManager.WriteLog($"Client {regData.token} registered as {user.userName}.");
                    //Send RegisterSuccess message | 发送注册成功消息
                    await socket.Send(JsonConvert.SerializeObject(new ConnectionMessage.Server.RegisterSuccess()));
                }
                else
                {
                    LogManager.WriteLog(
                        $"Client {regData.token} failed to register as {user.userName},because username already exists.");
                    //Send RegisterFailed and reason | 发送注册失败以及原因消息
                    await socket.Send(JsonConvert.SerializeObject(new ConnectionMessage.Server.RegisterFailed
                    {
                        reason = "username already exists" // username already exists | 用户名已存在
                    }));
                }
            }
            else
            {
                //illegal client, Drop it | 非法客户端，丢弃并断开连接
                clients.Drop(socket);
            }
        }
    }

    private static async Task ServerOnOpen(IWebSocketConnection socket)
    {
        await socket.Send(
            JsonConvert.SerializeObject(
                new ConnectionMessage.Server.GetData()
                {
                    token = Guid.NewGuid().ToString(),
                    needPassword = config.isPrivate
                }
            )
        );
    }

    private static async Task ServerOnError(Exception e, IWebSocketConnection socket)
    {
        LogManager.WriteLog(e.Message, LogManager.LogLevel.Debug);
        //WriteLog | 输出日志
        LogManager.WriteLog("Socket has been drop, because of an error: " + e.Message, LogManager.LogLevel.Error);


        //Drop and disconnect | 丢弃并断开连接
        clients.Drop(socket);
        socket.Close();
    }
    
    private static async Task ServerOnClose(IWebSocketConnection socket)
    {
        LogManager.WriteLog("Client disconnected.");
        clients.Drop(socket);
        // TODO: add client disconnected event
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
    }

    /// <summary>
    /// Broadcast message to all clients | 广播消息给所有客户端
    /// </summary>
    /// <param name="message"></param>
    public static void Broadcast(string message)
    {
        //Broadcast message to all clients
        foreach (var client in clients)
        {
            client.socket.Send(message);
        }
    }
}