using System.Net.Sockets;
using Fleck;

namespace Phi_MGUS;

public static class GameManager
{
    /// <summary>
    /// Client List | 客户端列表
    /// </summary>
    public class ClientList : List<Client>
    {
        /// <summary>
        /// Token List | Token列表
        /// </summary>
        private List<string> tokens = new();
        
        /// <summary>
        /// Using Token to Drop Client
        /// </summary>
        /// <param name="token"></param>
        public void Drop(string token)
        {
            for (var i = 0; i < Count; i++)
            {
                if (this[i].token == token)
                {
                    this[i].socket.Close();
                    RemoveAt(i);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Using Socket to Drop Client
        /// </summary>
        /// <param name="socket"></param>
        public void Drop(IWebSocketConnection socket)
        {
            for (var i = 0; i < Count; i++)
            {
                if (this[i].socket == socket)
                {
                    this[i].socket.Close();
                    RemoveAt(i);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Join the client to the server
        /// </summary>
        /// <param name="client"></param>
        public void Join(Client client)
        {
            if (!tokens.Contains(client.token))
            {
                tokens.Add(client.token);
                Add(client);
            }
        }

        /// <summary>
        /// Using Token to Find Clients
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Client or null</returns>
        public Client? GetClientAsToken(string token)
        {
            // check if token exists
            if (!tokens.Contains(token)) return null;
            //find token in tokens，and use index to get client
            return this[tokens.IndexOf(token)];
        }
        
        /// <summary>
        /// Using Socket to Find Clients
        /// </summary>
        /// <param name="socket"></param>
        /// <returns>Client or null</returns>
        public Client? GetClientAsSocket(IWebSocketConnection socket)
        {
            //find socket in clients，and use index to get client
            for (var i = 0; i < Count; i++)
            {
                if (this[i].socket == socket) return this[i];
            }
            return null;
        }
    }
    
    /// <summary>
    /// User List | 用户列表
    /// </summary>
    public class UserList : List<User>
    {
        private List<string> userNameList = new();
        
        
        /// <summary>
        /// register | 注册
        /// </summary>
        /// <param name="user"></param>
        public bool Register(User user)
        {
            // Check if the username exists, return false if it exists, and prevent non anonymous users from using anonymous usernames. | 判断用户名是否存在，如果存在，返回false，并阻止非匿名用户使用匿名用户名
            if (!user.isAnonymous && user.userName == "anonymous")
                return false;
            if (user.isAnonymous)
            {
                if (userNameList.Contains(user.userName))
                    return false;
            }
            Add(user);
            userNameList.Add(user.userName);
            return true;
        }
    }
    
    /// <summary>
    /// Client | 客户端
    /// </summary>
    public class Client
    {
        public string token = "";
        public IWebSocketConnection socket;
        public FeatureSupport featureSupport = new();
        public User? user;

        public class FeatureSupport : ConnectionMessage.Client.FeatureSupport
        {
            /// <summary>
            /// Class conversion, used to decouple from ConnectMessage.cs | class转换，用于与ConnectionMessage.cs解耦
            /// </summary>
            /// <param name="connectionMessageClientFeatureSupport"></param>
            /// <returns>FeatureSupport</returns>
            public static FeatureSupport ToGameManagerFeatureSupport(ConnectionMessage.Client.FeatureSupport connectionMessageClientFeatureSupport)
            {
                return new FeatureSupport
                {
                    RealTimeUpload = connectionMessageClientFeatureSupport.RealTimeUpload,
                    VotingSelection = connectionMessageClientFeatureSupport.VotingSelection,
                    RealTimeLeaderboard = connectionMessageClientFeatureSupport.RealTimeLeaderboard,
                    RealTimeChat = connectionMessageClientFeatureSupport.RealTimeChat
                };
            }
        }
    }
    
    public class RoomList : List<Room>
    {
        
    }
    /// <summary>
    /// Room | 多人房间
    /// </summary>
    public class Room
    {
        public string roomName = "undefined";
        public string roomPassword = "";
        public string roomUUID = "";
        public List<User> userList = new();
        public User owner = new();
    }
    /// <summary>
    /// User | 用户
    /// </summary>
    public class User
    {
        public Client Client = new();
        public string userName = "anonymous";
        public bool isSpectator = false;
        public bool isDebugger = false;
        public bool isAnonymous = true;
    }
}