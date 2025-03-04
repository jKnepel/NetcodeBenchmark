using System;
using Mirror;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.NetcodeBenchmark.Projects.Mirror
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkAuthorityManager : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnAuthorChanged))] private uint _author;
        [SyncVar(hook = nameof(OnOwnerChanged))] private uint _elevatedOwner;
    
        public uint Author => _author;
        public bool IsAuthor => Application.isPlaying && isClient && Author == NetworkClient.connection.identity.netId;
        public uint Owner => _elevatedOwner;
        public bool IsOwner => Application.isPlaying && isClient && Owner == NetworkClient.connection.identity.netId;
    
        public event Action<uint> AuthorshipChanged;
        public event Action<uint> OwnershipChanged;
    
        [Command(requiresAuthority = false)]
        public void CmdRequestAuthority(NetworkConnectionToClient sender = null)
        {
            if (_elevatedOwner != 0 || _author == sender.identity.netId)
                return;
    
            GiveOwnership(sender);
        }
    
        [Command(requiresAuthority = false)]
        public void CmdReleaseAuthority(NetworkConnectionToClient sender = null)
        {
            if (_elevatedOwner != 0 || _author != sender.identity.netId)
                return;
                
            RemoveOwnership();
        }
    
        [Command(requiresAuthority = false)]
        public void CmdRequestOwnership(NetworkConnectionToClient sender = null)
        {
            if (_elevatedOwner != 0)
                return;
    
            _elevatedOwner = sender.identity.netId;
            GiveOwnership(sender);
        }
    
        [Command(requiresAuthority = false)]
        public void CmdReleaseOwnership(NetworkConnectionToClient sender = null)
        {
            if (_author != sender.identity.netId)
                return;
            
            _elevatedOwner = 0;
        }
    
        private void GiveOwnership(NetworkConnectionToClient conn)
        {
            _author = conn.identity.netId;
            netIdentity.AssignClientAuthority(conn);
        }
    
        private void RemoveOwnership()
        {
            _author = 0;
            netIdentity.RemoveClientAuthority();
        }
    
        private void OnAuthorChanged(uint previous, uint next)
        {
            AuthorshipChanged?.Invoke(previous);
        }
    
        private void OnOwnerChanged(uint previous, uint next)
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