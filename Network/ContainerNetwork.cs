using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ContainerResizer.Systems;
using Mirror;
using Mirror.RemoteCalls;

namespace ContainerResizer.Network
{
    public class ContainerNetwork : NetworkBehaviour
    {
        //Static Properties
        public static bool IsClient => _isClient;
        public static bool IsServer => _isServer;
        public static bool IsLocal => _isLocal;
        public static ContainerNetwork Instance => _instance;
        
        private static bool _isClient;
        private static bool _isServer;
        private static bool _isLocal;

        private static ContainerNetwork _instance;
        
        //Properties
        [SyncVar]
        public string NetworkID;
        public PlatformUserId playerID;
        private NetworkedPlayer _player => Player.instance.networkedPlayer;

        private HashSet<NetworkConnection> _clients = new ();

        public string NetworkNetworkID
        {
            get => NetworkID;
            [param: In]
            set
            {
                if (SyncVarEqual(value, ref NetworkID))
                    return;
                string networkId = NetworkID;
                SetSyncVar(value, ref NetworkID, 1UL);
            }
        }


        #region Sync Methods

        protected override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
        {
            bool flag = base.SerializeSyncVars(writer, forceAll);
            if (forceAll)
            {
                writer.WriteString(NetworkID);
                return true;
            }
            writer.WriteULong(syncVarDirtyBits);
            if (((long) syncVarDirtyBits & 1L) != 0L)
            {
                writer.WriteString(NetworkID);
                flag = true;
            }
            return flag;
        }

        protected override void DeserializeSyncVars(NetworkReader reader, bool initialState)
        {
            base.DeserializeSyncVars(reader, initialState);
            if (initialState)
            {
                string networkId = NetworkID;
                NetworkNetworkID = reader.ReadString();
            }
            else
            {
                if (((long) reader.ReadULong() & 1L) == 0L)
                    return;
                string networkId = NetworkID;
                NetworkNetworkID = reader.ReadString();
            }
        }        

        #endregion

        #region Static Methods
        static ContainerNetwork()
        {
            
            RemoteCallHelper.RegisterCommandDelegate(typeof(ContainerNetwork), "RequestContainerRegistry",
                new CmdDelegate(InvokeUserCode_RequestContainerRegistry), true);

            RemoteCallHelper.RegisterCommandDelegate(typeof(ContainerNetwork), nameof(UpdateContainer),
                new CmdDelegate(InvokeUserCode_UpdateContainer), true);
            
            RemoteCallHelper.RegisterRpcDelegate(typeof(ContainerNetwork), "LoadContainerRegistryFromServer",
                new CmdDelegate(InvokeUserCode_LoadContainerRegistryFromServer));

            RemoteCallHelper.RegisterRpcDelegate(typeof(ContainerNetwork), nameof(UpdateContainerFromServer),
                new CmdDelegate(InvokeUserCode_UpdateContainerFromServer));
        }

        private static void InvokeUserCode_RequestContainerRegistry(NetworkBehaviour obj, NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            if (!NetworkServer.active)
                ContainerResizer.Log.LogError("Command RequestContainerRegistry called on client.");
            else
                ((ContainerNetwork)obj).UserCode_RequestContainerRegistry(senderConnection);
        }
        
        private static void InvokeUserCode_UpdateContainer(NetworkBehaviour obj, NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            if (!NetworkServer.active)
                ContainerResizer.Log.LogError("Command UpdateContainer called on client.");
            else
                ((ContainerNetwork)obj).UserCode_UpdateContainer(reader.ReadUInt(), reader.ReadInt(), senderConnection);
        }
        
        protected static void InvokeUserCode_LoadContainerRegistryFromServer(
            NetworkBehaviour obj,
            NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            if (!NetworkClient.active)
                ContainerResizer.Log.LogError("TargetRPC LoadContainerRegistryFromServer called on server.");
            else
                ((ContainerNetwork)obj).UserCode_LoadContainerRegistryFromServer(NetworkClient.connection, reader.ReadList<string>());
        }

        protected static void InvokeUserCode_UpdateContainerFromServer(
            NetworkBehaviour obj,
            NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            if (!NetworkClient.active)
                ContainerResizer.Log.LogError("TargetRPC LoadContainerRegistryFromServer called on server.");
            else
                ((ContainerNetwork)obj).UserCode_UpdateContainerFromServer(NetworkClient.connection, reader.ReadUInt(), reader.ReadInt());
        }

        #endregion

        #region Instantiated Methods

        private void Awake()
        {
            _instance = this;
            syncMode = SyncMode.Owner;
        }

        public override void OnStartClient()
        {
            ContainerResizer.Log.LogDebug($"OnStartClient() for Network ID {netId}");
            if (connectionToClient == null)
            {
                return;
            }

            _isClient = isClient;
            _isServer = isServer;
            _isLocal = isLocalPlayer;
            
            if (IsClient)
                _clients.Add(connectionToClient);
        }

        public override void OnStartLocalPlayer()
        {
            ContainerResizer.Log.LogDebug($"OnStartLocalPlayer() for Network ID {netId}");
            RequestContainerRegistry();
        }
        
        public override void OnStartServer()
        {
            if (connectionToClient == null)
            {
                return;
            }
            ContainerResizer.Log.LogDebug($"OnStartServer() for Network ID {netId}");
                
            if (!hasAuthority)
            {
                NetworkNetworkID = (string)connectionToClient.authenticationData;
                playerID = Platform.mgr.ClientIdFromString(NetworkID);
                ContainerResizer.Log.LogDebug($"No Authority for Network ID {netId} / Player ID {playerID}");
            }
            else
            {
                NetworkNetworkID = "host";
                playerID = Platform.mgr.LocalPlayerId();
                ContainerResizer.Log.LogDebug($"Has Authority for Network ID {netId} / Player ID {playerID}");
            }
            _isClient = isClient;
            _isServer = isServer;
            _isLocal = isLocalPlayer;
        }

        public override void OnStopClient()
        {
            if (connectionToClient == null)
            {
                return;
            }
            ContainerResizer.Log.LogDebug($"OnStopClient() for Network ID {netId}");
            
            if (IsClient)
                _clients.Remove(connectionToClient);
        }

        [Command]
        public void UpdateContainer(uint instanceId, int slotCount,NetworkConnectionToClient sender = null)
        {
            if (isServer)
                return;
            ContainerResizer.Log.LogDebug($"Updating Container {instanceId}...");
            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
            writer.WriteUInt(instanceId);
            writer.WriteInt(slotCount);
            SendCommandInternal(typeof(ContainerNetwork), nameof(UpdateContainer), writer, 0);
            NetworkWriterPool.Recycle(writer);
        }

        
        [Command]
        public void RequestContainerRegistry(NetworkConnectionToClient sender = null)
        {
            if (isServer)
                return;
            ContainerResizer.Log.LogDebug($"Requesting Container Registry...");
            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
            SendCommandInternal(typeof(ContainerNetwork), nameof(RequestContainerRegistry), writer, 0);
            NetworkWriterPool.Recycle(writer);
        }

        [TargetRpc]
        public void LoadContainerRegistryFromServer(
            NetworkConnection connection)
        {
            ContainerResizer.Log.LogDebug($"Container Registry Requested by NetworkedPlayer");
            var writer = NetworkWriterPool.GetWriter();
            var exportList = new List<string>();
            foreach (var record in ContainerManager.ExportRegistry())
            {
                exportList.Add(record.ToString());
            }
            writer.WriteList(exportList);
            SendTargetRPCInternal(connection, typeof(ContainerNetwork), nameof(LoadContainerRegistryFromServer), writer, 0);
            NetworkWriterPool.Recycle(writer);
        }

        [TargetRpc]
        public void UpdateContainerFromServer(uint instanceId, int slotCount, NetworkConnection connection = null)
        {
            ContainerResizer.Log.LogDebug($"Sending Updated Container...");
            var writer = NetworkWriterPool.GetWriter();
            writer.WriteUInt(instanceId);
            writer.WriteInt(slotCount);

            foreach (var client in _clients)
            {
                if (client == connection)
                    continue;
                
                SendTargetRPCInternal(connection, typeof(ContainerNetwork), nameof(UpdateContainerFromServer), writer, 0);    
            }
            
            
            NetworkWriterPool.Recycle(writer);
        }

        private void UserCode_UpdateContainer(uint instanceId, int slotCount, NetworkConnectionToClient sender = null)
        {
            if (ContainerManager.TryResizeChest(instanceId,slotCount,out _))
                UpdateContainerFromServer(instanceId, slotCount, sender );
        }

        private void UserCode_RequestContainerRegistry(NetworkConnectionToClient sender)
        {
            StartCoroutine(PackageCoreCount(sender));
        }

        private void UserCode_LoadContainerRegistryFromServer(NetworkConnection connection,
            List<string> importList)
        {
            ContainerResizer.Log.LogDebug($"Container Registry Received by Host/Server");
            ContainerManager.ImportRegistry(importList);
        }

        private void UserCode_UpdateContainerFromServer(NetworkConnection connection, uint instanceId, int slotCount)
        {
            ContainerResizer.Log.LogDebug($"Container Update Received by Host/Server");
            ContainerManager.TryResizeChest(instanceId, slotCount, out _);
        }

        private IEnumerator PackageCoreCount(NetworkConnectionToClient sender)
        {
            while (SaveState.isSaving)
                yield return null;

            LoadContainerRegistryFromServer(sender);
        }

        #endregion
    }
}