using Mirror;
using UnityEngine;
using UnityEditor;

namespace jKnepel.NetcodeBenchmark.Projects.Mirror
{
    public class NetworkManagerController : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;

        private void Awake()
        {
            if (networkManager == null)
                networkManager = FindObjectOfType<NetworkManager>();
        }

        public void StartServer() => networkManager.StartServer();
        public void StopServer() => networkManager.StopServer();
        public void StartClient() => networkManager.StartClient();
        public void StopClient() => networkManager.StopClient();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkManagerController))]
    public class NetworkManagerControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var controller = (NetworkManagerController)target;

            if (GUILayout.Button("Start Server"))
                controller.StartServer();
            
            if (GUILayout.Button("Stop Server"))
                controller.StopServer();
            
            if (GUILayout.Button("Start Client"))
                controller.StartClient();
            
            if (GUILayout.Button("Stop Client"))
                controller.StopClient();
        }
    }
#endif
}