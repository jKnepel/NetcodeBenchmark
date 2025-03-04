using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.Mirror
{
    public class KatamariManager : NetworkBehaviour
    {
        #region attributes

        [Header("References")]
        [SerializeField] private GameObject objectPrefab;
        [SerializeField] private GameObject playerPrefab;

        [Header("Values")]
        [SerializeField] private int numberOfObjects = 49;
        [SerializeField] private float spawnDistance = 1f;

        private GameObject[] _networkObjects;
        private readonly Dictionary<int, GameObject> _playerObjects = new();

        public static Vector2 DirectionalInput { get; private set; }

        #endregion

        #region lifecycle

        public override void OnStartServer()
        {
            base.OnStartServer();

            NetworkServer.OnConnectedEvent += SpawnClient;
            NetworkServer.OnDisconnectedEvent += DespawnClient;

            _networkObjects = new GameObject[numberOfObjects];
            int numberOfColumns = (int)Math.Ceiling(Mathf.Sqrt(numberOfObjects));
            int numberOfRows = (int)Math.Ceiling((float)numberOfObjects / numberOfColumns);
            float startX = -((float)(numberOfColumns - 1) / 2 * spawnDistance);
            float startZ = -((float)(numberOfRows - 1) / 2 * spawnDistance);

            for (int index = 0; index < numberOfObjects; index++)
            {
                int i = index / numberOfRows;
                int j = index % numberOfRows;

                float x = startX + i * spawnDistance;
                float z = startZ + j * spawnDistance;
                Vector3 position = new(x, objectPrefab.transform.position.y, z);
                GameObject obj = Instantiate(objectPrefab, position, objectPrefab.transform.rotation, transform);
                NetworkServer.Spawn(obj);
                _networkObjects[index] = obj;
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            foreach (var obj in _networkObjects)
            {
                if (obj != null)
                {
                    NetworkServer.Destroy(obj);
                }
            }
        }

        public void SetObjectNumber(int val) => numberOfObjects = val;
        public void SetDirectionalInput(Vector2 dir) => DirectionalInput = dir;

        #endregion

        #region private methods

        private void SpawnClient(NetworkConnectionToClient conn)
        {
            int numOfPlayers = NetworkServer.connections.Count;
            Vector3 spawnPosition = new(-3f + numOfPlayers * 1.5f, 0.5f, -5);
            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player);
            _playerObjects[conn.connectionId] = player;
        }

        private void DespawnClient(NetworkConnectionToClient conn)
        {
            if (_playerObjects.TryGetValue(conn.connectionId, out GameObject player))
            {
                NetworkServer.Destroy(player);
                _playerObjects.Remove(conn.connectionId);
            }
        }

        #endregion
    }
}