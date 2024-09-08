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
        public static readonly List<Room> roomList = new();
        public static void AddRoom(User user, string roomID)
        {
            roomList.Add(new Room(user, roomID));
            user.userStatus = User.Status.InRoom;
        }
        
        /// <summary>
        /// Dissolve the room | 解散房间
        /// </summary>
        /// <param name="room">房间</param>
        public static void RemoveRoom(Room room)
        {
            for(var i = roomList.Count - 1; i >= 0; i--)
            {
                if(roomList[i] == room)
                {
                    roomList.RemoveAt(i);
                }
            }
        }
        /// <summary>
        /// Remove room by roomID | 根据房间ID删除房间
        /// </summary>
        /// <param name="roomID">房间ID</param>
        public static void RemoveRoom(string roomID)
        {
            for(var i = roomList.Count - 1; i >= 0; i--)
            {
                if(roomList[i].roomID == roomID)
                {
                    roomList.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Get room by roomID | 根据房间ID获取房间
        /// </summary>
        /// <param name="roomID">房间ID</param>
        /// <returns>Room | 房间</returns>
        public static Room? GetRoom(string roomID)
        {
            return roomList.FirstOrDefault(x => x.roomID == roomID);
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
        /// <param name="socket">用户对应Socket</param>
        public static void RemoveUser(IWebSocketConnection socket)
        {
            for(int i = userList.Count - 1; i >= 0; i--)
            {
                if(userList[i].userSocket == socket)
                {
                    if (userList[i].room != null)
                    {
                        userList[i].room!.Leave(userList[i]);
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
        /// <param name="owner">房间所有者</param>
        /// <param name="roomID">房间ID</param>
        /// <exception cref="ArgumentException">Illegal room ID | 非法房间ID</exception>
        public Room(User owner, string roomID)
        {
            if (roomID.Length > 32)
            {
                throw new ArgumentException("RoomIdentifier cannot exceed 32 digits."); // 房间ID长度不能超过32位
            }
            if (!Regex.IsMatch(roomID, @"^[a-zA-Z0-9]+$"))
            {
                throw new ArgumentException("RoomIdentifier can only use English or numbers.");// 房间ID只能使用英文或数字
            }
            this.owner = owner;
            owner.room = this;
            this.roomID = roomID;
            userList = new();
            LogManager.WriteLog($"New room created: {roomID} by {owner.name}");
        }
        /// <summary>
        /// User join room | 用户加入房间
        /// </summary>
        /// <param name="user">用户</param>
        public void Join(User user)
        {
            userList.Add(user);
            user.room = this;
        }
        /// <summary>
        /// User leave room | 用户离开房间
        /// </summary>
        /// <param name="user">用户</param>
        public void Leave(User user)
        {
            userList.Remove(user);
            if (userList.Count == 0)
            {
                RoomManager.RemoveRoom(this);
                LogManager.WriteLog($"{roomID} user all left, room removed.");
            }
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
            get => userList[index]; 
            set => userList[index] = value;
        }
    }
    
    /// <summary>
    /// User(Player) | 用户（玩家）
    /// </summary>
    public class User
    {
        public enum Status
        {
            AFK,
            InRoom 
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
        public string name;
        public IWebSocketConnection userSocket;
        public Room? room;
        public DateTime joinTime = DateTime.Now;
        public string avatarUrl = Program.config.userDefauletAvatarUrl;
        
        
        public User(string name, IWebSocketConnection socket, UserConfig config)
        {
            if(name == null)
            {
                this.name = "anonymous";
            }
            else
            {
                this.name = name;
            }
            userSocket = socket;
            userConfig = config;
        }

        

        /// <summary>
        /// New Room | 创建房间
        /// </summary>
        /// <param name="roomID">房间ID</param>
        /// <exception cref="ArgumentException">房间已存在</exception>
        public void CreateRoom(string roomID)
        {
            if (RoomManager.GetRoom(roomID) != null)
            {
                throw new ArgumentException("RoomID already exists.");
            }
            else
            {
                RoomManager.AddRoom(this, roomID);
            }
        }
        
        /// <summary>
        /// User disconnect | 用户断开连接
        /// </summary>
        public void Disconnect()
        {
            UserManager.RemoveUser(userSocket);
            if (room != null)
            {
                if (room.owner == this)
                {
                    if (room.Count == 0)
                    {
                        RoomManager.RemoveRoom(room);// Remove room | 移除房间
                    }
                    else
                    {
                        room.owner = room[0];
                    }
                }
            }
        }
    }
}