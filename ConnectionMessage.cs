using Newtonsoft.Json.Linq;

namespace Phi_MGUP
{
    public static class ConnectionMessage
    {
        /// <summary>
        /// client and server message
        /// </summary>
        public class Message
        {
            public string action = "";
            public string token  = "";
            public JObject data = new JObject();
        }
        public static class Server
        {
            /// <summary>
            /// Get client meta data message
            /// </summary>
            public class GetData
            {
                public readonly string action = "getData"; 
                public string token = "";
                public bool needPassword = false;
            }
            /// <summary>
            /// Room failed message
            /// </summary>
            public class AddRoomFailed
            {
                public readonly string action = "addRoomFailed";
                public string token = "";
                public string reason = "unknown";
            }
            
            /// <summary>
            /// Room success message
            /// </summary>
            public class AddRoomSuccess
            {
                public readonly string action = "addRoomSuccess";
                public string token = "";
            }
        }
        public static class Client
        {
            /// <summary>
            /// Client meta data
            /// </summary>
            public class ClientMetaData
            {
                public readonly string action = "clientMetaData";
                public string token = "";
                public object data = new
                {
                    features = new FeatureSupport(),// Compatibility with lower versions of the .NET Framework
                    identifierName = "anonymous", // client name, example: "NRLT_PhiSim Next"
                    clientVersion = -1, // client Version, anonymous example: -1
                    userName = "anonymous" // client username, anonymous example: "anonymous"
                };
            }

            /// <summary>
            /// Feature support
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

