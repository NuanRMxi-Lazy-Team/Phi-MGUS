using Newtonsoft.Json.Linq;

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
            public string token = "";
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
            /// Room add failed message | 房间新建失败消息
            /// </summary>
            public class AddRoomFailed : Message
            {
                public new readonly string action = "addRoomFailed";
                public string reason = "unknown";
            }

            /// <summary>
            /// Room success message | 房间新建成功消息
            /// </summary>
            public class AddRoomSuccess : Message
            {
                public new readonly string action = "addRoomSuccess";
            }

            /// <summary>
            /// Register failed message | 注册失败消息
            /// </summary>
            public class RegisterFailed : Message
            {
                public new readonly string action = "registerFailed";
                public string reason = "unknown";
            }

            /// <summary>
            /// Register success message | 注册成功消息
            /// </summary>
            public class RegisterSuccess : Message
            {
                public new readonly string action = "registerSuccess";
                public string token = "";
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
                public string token = "";

                public object data = new
                {
                    features = new FeatureSupport(), // Compatibility with lower versions of the .NET Framework
                    identifierName = "anonymous", // client name, example: "NRLT_PhiSim Next"
                    clientVersion = -1, // client Version, anonymous example: -1
                    userName = "anonymous", // client username, anonymous example: "anonymous"
                    password = "" // if server is private，this field should be filled
                };
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
            /// Register message | 注册消息
            /// </summary>
            public class Register : Message
            {
                public new readonly string action = "register";
                public object data = new
                {
                    isAnonymous = true,
                    userName = "anonymous",
                    isSpectator = false,
                    isDebugger = false,
                    authentication = "" //if Spectator or Debugger，this field should be filled
                };
            }
        }
    }
}