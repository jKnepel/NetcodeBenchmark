using System;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.NetcodeBenchmark.Projects.FishNet
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkAuthorityManager : NetworkBehaviour
    {
        private readonly SyncVar<NetworkConnection> _author = new();
        private readonly SyncVar<NetworkConnection> _elevatedOwner = new();
    
        public NetworkConnection Author => _author.Value;
        public bool IsAuthor => Author == LocalConnection;
        public new NetworkConnection Owner => _elevatedOwner.Value;
        public new bool IsOwner => Owner == LocalConnection;

        public Action<NetworkConnection> AuthorshipChanged;
        public Action<NetworkConnection> OwnershipChanged;

        private void Awake()
        {
            _author.OnChange += on_author;
            _elevatedOwner.OnChange += on_owner;
        }

        private void OnDestroy()
        {
            _author.OnChange -= on_author;
            _elevatedOwner.OnChange -= on_owner;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _elevatedOwner.Value = null;
        }
    
        [ServerRpc(RequireOwnership = false)]
        public void RequestAuthority(NetworkConnection conn = null)
        {
            if ((_elevatedOwner.Value != null && _elevatedOwner.Value.IsValid) || _author.Value == conn)
                return;
            
            GiveOwnership(conn);
        }
    
        [ServerRpc(RequireOwnership = false)]
        public void ReleaseAuthority(NetworkConnection conn = null)
        {
            if (_author.Value != conn || _elevatedOwner.Value != null) 
                return;
            
            
            RemoveOwnership();
        }
    
        [ServerRpc(RequireOwnership = false)]
        public void RequestOwnership(NetworkConnection conn = null)
        {
            if (_elevatedOwner.Value != null && _elevatedOwner.Value.IsValid)
                return;
            
            _elevatedOwner.Value = conn;
            GiveOwnership(conn);
        }
    
        [ServerRpc(RequireOwnership = false)]
        public void ReleaseOwnership(NetworkConnection conn = null)
        {
            if (_author.Value != conn) 
                return;
            
            _elevatedOwner.Value = null;
        }
    
        private new void GiveOwnership(NetworkConnection conn = null)
        {
            _author.Value = conn;
            base.GiveOwnership(conn);
        }
    
        private new void RemoveOwnership()
        {
            _author.Value = null;
            base.RemoveOwnership();
        }
        
        private void on_author(NetworkConnection prev, NetworkConnection next, bool asServer)
        {
            AuthorshipChanged?.Invoke(prev);
        }

        private void on_owner(NetworkConnection prev, NetworkConnection next, bool asServer)
        {
            OwnershipChanged?.Invoke(prev);
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
