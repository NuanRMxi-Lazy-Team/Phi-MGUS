# Phi-MGUS
Phigros 自制谱通用多人游戏服务器
MGUS，也就是Multiplayer Game Universal Server的简写，项目全称**Phigros Multiplayer Game Universal Server**

# 项目介绍
该项目作为的服务端，服务端为一个 `WebSocket` 服务器，客户端只需接入服务端即可进行多人游戏，相关文档在另一个项目中。  
请见：https://github.com/NuanRMxi-Lazy-Team/Phi-MGUS-docs  
放心，还没写完。

我们允许您同时开放websocket服务端和带有证书的websocket服务端，且数据互通，但是我们不建议您这样做，我们只建议您开启其中一个。  
服务器不依赖任何社区，不存储持续存储任何客户端以及用户数据，所有数据交换都由客户端完成并由服务端进行转发。  
使用Json作为信息格式，如果您的目标语言为 `C#`，您完全可以直接复制 `ConnectionMessage.cs` 文件到您的项目，文件中存储了所有信息格式并附带注释。

# 功能支持状态

- [x] WebSocket SSL
- [x] 安全WebSocket与非安全WebSocket同时开放
- [x] 匿名用户 
- [ ] ~~重名校验~~
- [ ] 旁观者
- [x] 服务器可私有
- [ ] 头像
- [x] 新建房间
- [x] 加入房间
- [x] 离开房间
- [x] 自动丢弃房间
- [x] 自动修改房间所有者
- [x] 房间内广播
- [ ] 房间用户数量限制
- [ ] 用户选谱
- [ ] 实时计算玩家成绩
- [ ] 实时聊天
- [ ] 游戏管理