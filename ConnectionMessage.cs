using System.Text.RegularExpressions;
using System;
namespace Phi_MGUS
{
    public static class ConnectionMessage
    {
        /// <summary>
        /// client and server message | 客户端与服务器消息
        /// </summary>
        public class Message
        {
            public string action = "";
            //public string token = "";
        }

        /// <summary>
        /// Server to client message | 服务器到客户端消息
        /// </summary>
        public static class Server
        {
            /// <summary>
            /// Get client metadata message | 获取客户端元数据消息
            /// </summary>
            public class GetData : Message
            {
                public new readonly string action = "getData";
                public bool needPassword = false;
            }
            
            /// <summary>
            /// Join server failed message | 加入服务器失败消息
            /// </summary>
            public class JoinServerFailed : Message
            {
                public new readonly string action = "joinServerFailed";
                public string reason = "unknown";
            }

            /// <summary>
            /// Join server success message | 加入服务器成功消息
            /// </summary>
            public class JoinServerSuccess : Message
            {
                public new readonly string action = "joinServerSuccess";
            }

            /// <summary>
            /// Room new failed message | 房间新建失败消息
            /// </summary>
            public class NewRoomFailed : Message
            {
                public new readonly string action = "newRoomFailed";
                public string reason = "unknown";
            }

            /// <summary>
            /// Room new success message | 房间新建成功消息
            /// </summary>
            public class NewRoomSuccess : Message
            {
                public new readonly string action = "newRoomSuccess";
            }
            /// <summary>
            /// Room join failed message | 房间加入成功消息
            /// </summary>
            public class JoinRoomFailed : Message
            {
                public new readonly string action = "joinRoomFaild";
                public string reason = "unknown";
            }
            /// <summary>
            /// Room join success message | 房间加入成功消息
            /// </summary>
            public class JoinRoomSuccess : Message
            {
                public new readonly string action = "joinRoomSuccess";
            }
            /// <summary>
            /// Leave room failed message | 离开房间失败消息
            /// </summary>
            public class LeaveRoomFailed : Message
            {
                public new readonly string action = "leaveRoomFailed";
                public string reason = "unknown";
            }
            /// <summary>
            /// Leave room success message | 离开房间成功消息
            /// </summary>
            public class LeaveRoomSuccess : Message
            {
                public new readonly string action = "leaveRoomSuccess";
            }
        }

        /// <summary>
        /// Client to server message | 客户端到服务器消息
        /// </summary>
        public static class Client
        {
            /// <summary>
            /// Client meta data | 客户端元数据
            /// </summary>
            public class ClientMetaData : Message
            {
                public new readonly string action = "clientMetaData";

                public Data data = new Data();

                public class Data
                {
                    public FeatureSupport features = new FeatureSupport();
                    public string identifierName = "anonymous";
                    public int identifierVersion = -1;
                    public string userName = null; // If it is an anonymous user, this value is null
                    public string password = null; // if server is private，this value is not null
                    public bool isDebugger = false;
                    public bool isSpectator = false;
                }
            }

            /// <summary>
            /// Feature support | 功能支持
            /// </summary>
            public class FeatureSupport
            {
                public bool RealTimeUpload = false;
                public bool VotingSelection = false;
                public bool RealTimeLeaderboard = false;
                public bool RealTimeChat = false;
            }
            /// <summary>
            /// New room | 新建房间
            /// </summary>
            public class NewRoom : Message
            {
                public readonly string action = "newRoom";
                public Data data = new Data
                {
                    //RoomID is a random string, length is 16 | 房间ID是随机字符串，长度为16
                    roomID = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16)
                };
                
                public class Data
                {
                    public int maxUser = 8;
                    private string _roomID;
                    public string roomID
                    {
                        set
                        {
                            if (value.Length > 32)
                            {
                                throw new ArgumentException("RoomIdentifier cannot exceed 32 digits.");// 不能超过32位
                            }
                            if (!Regex.IsMatch(value, @"^[a-zA-Z0-9]+$"))
                            {
                                throw new ArgumentException("RoomIdentifier can only use English or numbers.");// 只能使用英文或数字
                            }
                            _roomID = value;
                        }
                        get
                        {
                            return _roomID;
                        }
                    }// Only English or numbers can be used, and cannot exceed 32 digits | 只能使用英文或数字，且不超过32位
                    
                }
                public NewRoom(int? maxUser = 8, string roomID = null)
                {
                    if (roomID == null)
                    {
                        roomID = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
                    }
                    else
                    {
                        data.roomID = roomID;
                    }
                    if (maxUser == null)
                    {
                        maxUser = 8;
                    }
                    else
                    {
                        data.maxUser = maxUser.Value;
                    }
                }
            }
            
            /// <summary>
            /// Join room | 加入房间
            /// </summary>
            public class JoinRoom : Message
            {
                public readonly string action = "joinRoom";
                public Data data = new Data();
                
                public class Data
                {
                    public string roomID = "";
                }

                public JoinRoom(string roomID)
                {
                    data.roomID = roomID;
                }
            }
            /// <summary>
            /// Leave room | 离开房间
            /// </summary>
            public class LeaveRoom : Message
            {
                public readonly string action = "leaveRoom";
            }
        }
    }
}