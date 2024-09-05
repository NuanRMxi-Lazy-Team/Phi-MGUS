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

    private static GameManager.ClientList clients = new();
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
        WebSocketServer server =
            new WebSocketServer($"{(config.wss ? "wss" : "ws")}://{config.Host}:{config.Port}");
        //set cert
        if (!string.IsNullOrEmpty(config.certPath) && config.wss)
        {
            try
            {
                // set cert
                server.Certificate = new X509Certificate2(config.certPath, config.certPassword);
            }
            catch (Exception e)
            {
                LogManager.WriteLog("Secure connection is not enabled or the certificate is invalid.\n" + e.Message,
                    LogManager.LogLevel.Warning);
                server =
                    new WebSocketServer($"ws://{config.Host}:{config.Port}");
            }
        }
        else
        {
            LogManager.WriteLog("Secure connection is not enabled or the certificate is invalid.",
                LogManager.LogLevel.Warning);
            server =
                new WebSocketServer($"ws://{config.Host}:{config.Port}");
        }

        //change Fleck Log Level
        FleckLog.Level = LogLevel.Error;

        server.Start(socket =>
        {
            socket.OnOpen += async () => await ServerOnOpen(socket);
            socket.OnMessage += async message => await ServerOnMessage(message, socket);
            socket.OnError += async e => await ServerOnError(e, socket);

            socket.OnClose += () =>
            {
                LogManager.WriteLog("Client disconnected.");
                // TODO: add client disconnected event
            };

            socket.OnBinary += bytes =>
            {
                _ = bytes;
                LogManager.WriteLog("illegal Binary data received, Drop.", LogManager.LogLevel.Debug);

                //Drop and disconnect | 断开连接
                clients.Drop(socket);
                socket.Close();
            };
        });
        while (true)
        {
            Console.ReadLine();
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
                clients.Drop(socket);
                socket.Close();
                return;
            }

            var data = (dynamic)clientMetaData.data;
            ConnectionMessage.Client.FeatureSupport featSup =
                JsonConvert.DeserializeObject<ConnectionMessage.Client.FeatureSupport>(data.features)!;
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
        }
        else if (msg.action == "Register")
        {
            var regData = JsonConvert.DeserializeObject<ConnectionMessage.Client.Register>(message)!;
            if (clients.GetClientAsToken(regData.token) != null)
            {
                //TODO: add client registered event | 添加客户端注册事件
                GameManager.User user = JsonConvert.DeserializeObject<GameManager.User>((dynamic)regData.data)!;
                user.client = clients.GetClientAsToken(regData.token)!;
                if (clients.users.Register(user))
                {
                    await socket.Send(JsonConvert.SerializeObject(new ConnectionMessage.Server.RegisterSuccess()));
                }
                else
                {
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
        if (e is AggregateException)
        {
            LogManager.WriteLog("The client used an incorrect connection method.", LogManager.LogLevel.Debug);
        }
        else
        {
            LogManager.WriteLog("Socket has been drop", LogManager.LogLevel.Debug);
        }

        //Drop and disconnect| 丢弃并断开连接
        clients.Drop(socket);
        socket.Close();
    }

    /// <summary>
    /// Server configuration file
    /// </summary>
    public class Config
    {
        public string Host = "0.0.0.0";
        public int Port = 14157;
        public bool isPrivate = false;
        public bool RoomChat = false;
        public bool isDebug = false;
        public string Password = "";
        public bool wss = false;
        public string certPath = "path/to/your/certificate.pfx";
        public string certPassword = "your_certificate_password";
    }

    /// <summary>
    /// Broadcast message to all clients
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