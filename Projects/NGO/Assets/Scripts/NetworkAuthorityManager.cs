using System;
using Unity.Netcode;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.NetcodeBenchmark.Projects.NGO
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkAuthorityManager : NetworkBehaviour
    {
        private readonly NetworkVariable<ulong> _author = new(readPerm: NetworkVariableReadPermission.Everyone);
        private readonly NetworkVariable<ulong> _elevatedOwner = new(readPerm: NetworkVariableReadPermission.Everyone);
    
        public ulong Author => _author.Value;
        public bool IsAuthor => NetworkManager && NetworkManager.IsClient && Author == NetworkManager.LocalClientId;
        public ulong Owner => _elevatedOwner.Value;
        public new bool IsOwner => NetworkManager && NetworkManager.IsClient && Owner == NetworkManager.LocalClientId;
    
        public event Action<ulong> AuthorshipChanged;
        public event Action<ulong> OwnershipChanged;
    
        private void Awake()
        {
            _author.OnValueChanged += OnAuthorChanged;
            _elevatedOwner.OnValueChanged += OnOwnerChanged;
        }
    
        private void OnDestroy()
        {
            _author.OnValueChanged -= OnAuthorChanged;
            _elevatedOwner.OnValueChanged -= OnOwnerChanged;
        }
    
        [Rpc(SendTo.Server)]
        public void RequestAuthorityServerRpc(RpcParams rpcParams = default)
        {
            if (_elevatedOwner.Value != 0 || _author.Value == rpcParams.Receive.SenderClientId)
                return;
    
            GiveOwnership(rpcParams.Receive.SenderClientId);
        }
    
        [Rpc(SendTo.Server)]
        public void ReleaseAuthorityServerRpc(RpcParams rpcParams = default)
        {
            if (_elevatedOwner.Value != 0 || _author.Value != rpcParams.Receive.SenderClientId)
                return;
                
            RemoveOwnership();
        }
    
        [Rpc(SendTo.Server)]
        public void RequestOwnershipServerRpc(RpcParams rpcParams = default)
        {
            if (_elevatedOwner.Value != 0)
                return;
    
            _elevatedOwner.Value = rpcParams.Receive.SenderClientId;
            GiveOwnership(rpcParams.Receive.SenderClientId);
        }
    
        [Rpc(SendTo.Server)]
        public void ReleaseOwnershipServerRpc(RpcParams rpcParams = default)
        {
            if (_author.Value != rpcParams.Receive.SenderClientId)
                return;
            
            _elevatedOwner.Value = 0;
        }
    
        private void GiveOwnership(ulong clientId)
        {
            _author.Value = clientId;
            GetComponent<NetworkObject>().ChangeOwnership(clientId);
        }
    
        private void RemoveOwnership()
        {
            _author.Value = 0;
            GetComponent<NetworkObject>().RemoveOwnership();
        }
    
        private void OnAuthorChanged(ulong previous, ulong next)
        {
            AuthorshipChanged?.Invoke(previous);
        }
    
        private void OnOwnerChanged(ulong previous, ulong next)
        {
            OwnershipChanged?.Invoke(previous);
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkAuthorityManager))]
    public class NetworkAuthorityManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var manager = (NetworkAuthorityManager)target;
    
            EditorGUILayout.Toggle("Is Author", manager.IsAuthor);
            EditorGUILayout.Toggle("Is Owner", manager.IsOwner);
        }
    }
#endif
}
