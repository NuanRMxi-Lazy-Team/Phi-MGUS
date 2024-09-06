using Fleck;

namespace Phi_MGUS;

public static class GameManager
{
    public static class RoomManager
    {
        public static readonly List<Room> roomList = new();
        public static void AddRoom(User user, Room.RoomConfig config)
        {
            roomList.Add(new Room(user, config));
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

    public static class UserManager
    {
        public static readonly List<User> userList = new();
        public static void AddUser(string name, IWebSocketConnection socket, User.UserConfig config)
        {
            userList.Add(new User(name, socket, config));
        }

        public static void RemoveUser(IWebSocketConnection socket)
        {
            for(int i = userList.Count - 1; i >= 0; i--)
            {
                if(userList[i].userSocket == socket)
                {
                    userList.RemoveAt(i);
                }
            }
        }
    }

    public class Room
    {
        public List<User> userList;
        public User owner;
        public RoomConfig? roomConfig;
        public class RoomConfig
        {
            public string? roomName;
            public string? roomPassword;
        }
        public Room(User user, RoomConfig config)
        {
            owner = user;
            roomConfig = config;
            userList = new List<User>();
        }
    }

    public class User
    {
        public enum Status
        {
            Disconnect = 0,
            AFK = 1,
            InRoom = 2
        }

        public class UserConfig
        {
            public bool isSpectator;
            public bool isDebugger;
            public bool isAnonymous;
        }

        public Status userStatus;
        public UserConfig? userConfig;
        public string userName;
        public IWebSocketConnection userSocket;
        public Room? userRoom;
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
            userStatus = Status.AFK;
        }

        public void JoinRoom(Room room)
        {
            userRoom = room;
            userStatus = Status.InRoom;
        }

        public void ExitRoom()
        {
            userRoom = null;
            userStatus = Status.AFK;
        }

        public void CreateRoom(Room.RoomConfig config)
        {
            RoomManager.roomList.Add(new Room(this, config));
        }

        public void Disconnect()
        {
            UserManager.RemoveUser(userSocket);
        }
    }
}