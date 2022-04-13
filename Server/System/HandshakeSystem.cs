using LmpCommon.Enums;
using LmpCommon.Message.Data.Handshake;
using LmpCommon.Message.Data.PlayerConnection;
using LmpCommon.Message.Server;
using Server.Client;
using Server.Context;
using Server.Log;
using Server.Plugin;
using Server.Server;
using System.Linq;

namespace Server.System
{
    public partial class HandshakeSystem
    {
        private string Reason { get; set; }

        public void HandleHandshakeRequest(ClientStructure client, HandshakeRequestMsgData data)
        {
            if (ServerContext.Clients.Any(c => c.Value.UniqueIdentifier == data.UniqueIdentifier))
            {
                // Client already connected
                LunaLog.Debug($"Ignoring handshake request from already connected client {data.PlayerName} ({data.UniqueIdentifier}");
                return;
            }

            var valid = CheckServerFull(client);
            valid &= valid && CheckUsernameLength(client, data.PlayerName);
            valid &= valid && CheckUsernameCharacters(client, data.PlayerName);
            valid &= valid && CheckPlayerIsAlreadyConnected(client, data.PlayerName);
            valid &= valid && CheckUsernameIsReserved(client, data.PlayerName);
            valid &= valid && CheckPlayerIsBanned(client, data.UniqueIdentifier);

            if (!valid)
            {
                LunaLog.Normal($"Client {data.PlayerName} ({data.UniqueIdentifier}) failed to handshake: {Reason}. Disconnecting");
                client.DisconnectClient = true;
                ClientConnectionHandler.DisconnectClient(client, Reason);
            }
            else
            {
                client.PlayerName = data.PlayerName;
                client.UniqueIdentifier = data.UniqueIdentifier;
                client.Authenticated = true;

                LmpPluginHandler.FireOnClientAuthenticated(client);

                LunaLog.Normal($"Client {data.PlayerName} ({data.UniqueIdentifier}) handshake successfully, Version: {data.MajorVersion}.{data.MinorVersion}.{data.BuildVersion}");

                HandshakeSystemSender.SendHandshakeReply(client, HandshakeReply.HandshookSuccessfully, "success");

                var msgData = ServerContext.ServerMessageFactory.CreateNewMessageData<PlayerConnectionJoinMsgData>();
                msgData.PlayerName = client.PlayerName;
                MessageQueuer.RelayMessage<PlayerConnectionSrvMsg>(client, msgData);

                LunaLog.Debug($"Online Players: {ServerContext.PlayerCount}, connected: {ClientRetriever.GetClients().Length}");
            }
        }
    }
}
