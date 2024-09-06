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
        }
    }
}