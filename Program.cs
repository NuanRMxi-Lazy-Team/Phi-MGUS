using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Fleck;
using System.Security.Cryptography.X509Certificates;

namespace Phi_MGUP;

public class Program
{
    public static Config config = new()
    {
        isDebug = true
    };
    private static IDictionary<string, IWebSocketConnection> clients = new Dictionary<string, IWebSocketConnection>();//client list, key is token
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
        if (config.isPrivate && config.Password == "")
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
            catch (Exception)
            {
                LogManager.WriteLog("Secure connection is not enabled or the certificate is invalid.",
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
        FleckLog.Level = LogLevel.Debug;
        server.Start(socket =>
        {
            socket.OnOpen += () => ServerOnOpen(socket);

            socket.OnMessage += async message =>
            {
                var msg = JsonConvert.DeserializeObject<ConnectionMessage.Message>(message)!;
                if (msg.action == "clientMetaData")
                {
                    if (clients.ContainsKey(msg.token))
                    {
                        //illegal client, Drop it
                        DropClient(msg.token);
                        socket.Close();
                    }
                    //to ConnectionMessage.Client.ClientMetaData
                    var clientMetaData =
                        JsonConvert.DeserializeObject<ConnectionMessage.Client.ClientMetaData>(message)!;
                    LogManager.WriteLog($"Client {clientMetaData.token} connected.");
                    LogManager.WriteLog($"Raw Message: {message}", LogManager.LogLevel.Debug);
                    clients.Add(clientMetaData.token, socket);
                }
            };

            socket.OnError += async e =>
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

                socket.Close();
            };
            
            socket.OnClose += async () =>
            {
                LogManager.WriteLog("Client disconnected.");
            };

            socket.OnBinary += bytes =>
            {
                LogManager.WriteLog("illegal Binary data received, Fucked.", LogManager.LogLevel.Debug);
                socket.Close();
            };
        });
        while (true)
        {
            Console.ReadLine();
        }
    }
    
    private static void ServerOnMessage(string message)
    {
        
    }
    
    private static async void ServerOnOpen(IWebSocketConnection socket)
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

    private static void ServerOnError(Exception e)
    {
        
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
    /// Remove client from client list(value)
    /// </summary>
    /// <param name="socket"></param>
    public static void DropClient(IWebSocketConnection socket)
    {
        //use value to find key,and delete it
        foreach (var client in clients)
        {
            if (client.Value == socket)
            {
                clients.Remove(client.Key);
                break;
            }
        }
    }
    /// <summary>
    /// Remove client from client list(key)
    /// </summary>
    /// <param name="token"></param>
    public static void DropClient(string token)
    {
        clients.Remove(token);
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
            client.Value.Send(message);
        }
    }
}