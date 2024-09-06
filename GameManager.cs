using System.Text.RegularExpressions;
using Fleck;

namespace Phi_MGUS;

public static class GameManager
{
    /// <summary>
    /// Room Manager | 房间管理器
    /// </summary>
    public static class RoomManager
    {
        private static readonly List<Room> roomList = new();
        public static void AddRoom(User user, string roomID)
        {
            roomList.Add(new Room(user, roomID));
        }
        
        public static void RemoveRoom(Room instance)
        {
            for(int i = roomList.Count - 1; i >= 0; i--)
            {
                if(roomList[i] == instance)
                {
                    roomList.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// User Manager | 用户管理器
    /// </summary>
    public static class UserManager
    {
        public static readonly List<User> userList = new();
        public static void AddUser(string name, IWebSocketConnection socket, User.UserConfig config)
        {
            userList.Add(new User(name, socket, config));
        }
        
        [Obsolete("Use RemoveUser instead. | 使用 RemoveUser 代替.")]
        public static void Drop(IWebSocketConnection socket)
        {
            RemoveUser(socket);
        }
        
        /// <summary>
        /// Remove user | 移除用户
        /// </summary>
        /// <param name="socket"></param>
        public static void RemoveUser(IWebSocketConnection socket)
        {
            for(int i = userList.Count - 1; i >= 0; i--)
            {
                if(userList[i].userSocket == socket)
                {
                    if (userList[i].userRoom != null)
                    {
                        userList[i].userRoom!.Leave(userList[i]);
                    }
                    userList.RemoveAt(i);
                }
            }
        }
        
        public static bool Contains(IWebSocketConnection socket)
        {
            return userList.Any(x => x.userSocket == socket);
        }
        
        public static User GetUser(IWebSocketConnection socket)
        {
            return userList.First(x => x.userSocket == socket);
        }
        
    }

    /// <summary>
    /// Room | 房间
    /// </summary>
    public class Room
    {
        private List<User> userList;
        public User owner;
        public string roomID;
        /// <summary>
        /// Create room | 创建房间
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="roomID"></param>
        /// <exception cref="ArgumentException">Illegal room ID | 非法房间ID</exception>
        public Room(User owner, string roomID)
        {
            if (roomID.Length > 32)
            {
                throw new ArgumentException("RoomIdentifier cannot exceed 32 digits.");
            }
            if (!Regex.IsMatch(this.roomID, @"^[a-zA-Z0-9]+$"))
            {
                throw new ArgumentException("RoomIdentifier can only use pure English or numbers.");
            }
            this.owner = owner;
            this.roomID = roomID;
            userList = new List<User>();
        }
        /// <summary>
        /// User join room | 用户加入房间
        /// </summary>
        /// <param name="user">用户</param>
        public void Join(User user)
        {
            userList.Add(user);
        }
        /// <summary>
        /// User leave room | 用户离开房间
        /// </summary>
        /// <param name="user">用户</param>
        public void Leave(User user)
        {
            userList.Remove(user);
        }
        /// <summary>
        /// Broadcast message to room | 广播消息到房间
        /// </summary>
        /// <param name="message">被广播信息</param>
        public void Broadcast(string message,User? exceptUser)
        {
            foreach (var user in userList)
            {
                if(user != exceptUser)
                {
                    continue;
                }
                user.userSocket.Send(message);
            }
        }
        /// <summary>
        /// room user count | 房间用户数量
        /// </summary>
        public int Count => userList.Count;
        /// <summary>
        /// user index | 用户索引
        /// </summary>
        /// <param name="index">索引</param>
        public User this[int index]
        {
            get { return userList[index]; }
            set
            {
                userList[index] = value;
            }
        }
    }
    
    /// <summary>
    /// User(Player) | 用户（玩家）
    /// </summary>
    public class User
    {
        public enum Status
        {
            Disconnect = 0,
            AFK = 1,
            InRoom = 2
        }

        /// <summary>
        /// User Config | 用户配置
        /// </summary>
        public class UserConfig
        {
            public bool isSpectator;
            public bool isDebugger;
            public bool isAnonymous;
            public ConnectionMessage.Client.FeatureSupport featureSupport = new();
        }

        public Status userStatus = Status.AFK;
        public UserConfig? userConfig;
        public string userName;
        public IWebSocketConnection userSocket;
        public Room? userRoom;
        public DateTime joinTime = DateTime.Now;
        public string avatarUrl = Program.config.userDefauletAvatarUrl;
        
        
        public User(string name, IWebSocketConnection socket, UserConfig config)
        {
            if(name == null)
            {
                userName = "anonymous";
            }
            else
            {
                userName = name;
            }
            userSocket = socket;
            userConfig = config;
        }

        public void JoinRoom(Room room)
        {
            room.Join(this);
            userRoom = room;
            userStatus = Status.InRoom;
        }

        public void LeaveRoom()
        {
            userRoom!.Leave(this);
            userRoom = null;
            userStatus = Status.AFK;
        }

        public void CreateRoom(string roomID)
        {
            RoomManager.AddRoom(this, roomID);
        }

        public void Remove()
        {
            Disconnect();
        }
        /// <summary>
        /// User disconnect | 用户断开连接
        /// </summary>
        public void Disconnect()
        {
            UserManager.RemoveUser(userSocket);
            if (userRoom != null)
            {
                if (userRoom.owner == this)
                {
                    if (userRoom.Count == 0)
                    {
                        RoomManager.RemoveRoom(userRoom);// Remove room | 移除房间
                    }
                    else
                    {
                        userRoom.owner = userRoom[0];
                    }
                }
            }
        }
    }
}