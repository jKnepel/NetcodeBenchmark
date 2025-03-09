using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.Mirror
{
	public class SimpleManager : NetworkBehaviour
	{
		#region attributes

		[Header("References")]
		[SerializeField] private NetworkIdentity objectPrefab;
		[SerializeField] private NetworkIdentity playerPrefab;
		[SerializeField] private bool isClientAuthoritative; 

		[Header("Values")]
		[SerializeField] private int numberOfObjects = 9;
		[SerializeField] private float spawnDistance = 1f;

		private NetworkIdentity[] _networkObjects;
		private readonly Dictionary<int, GameObject> _playerObjects = new();

		public static Vector2 DirectionalInput { get; private set; }

		#endregion

		#region lifecycle
		
		private void Awake()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
		}

		public override void OnStartServer()
		{
			NetworkServer.OnConnectedEvent += SpawnClient;
			NetworkServer.OnDisconnectedEvent += DespawnClient;
			
			_networkObjects = new NetworkIdentity[numberOfObjects];
			var numberOfColumns = (int)Math.Ceiling(Mathf.Sqrt(numberOfObjects));
			var numberOfRows = (int)Math.Ceiling((float)numberOfObjects / numberOfColumns);
			var startX = -((float)(numberOfColumns - 1) / 2 * spawnDistance);
			var startZ = -((float)(numberOfRows    - 1) / 2 * spawnDistance);

			for (var index = 0; index < numberOfObjects; index++)
			{
				var i = index / numberOfRows;
				var j = index % numberOfRows;

				var x = startX + i * spawnDistance;
				var z = startZ + j * spawnDistance;
				Vector3 position = new(x, objectPrefab.transform.position.y, z);
				var obj = Instantiate(objectPrefab, position, objectPrefab.transform.rotation, transform);
				NetworkServer.Spawn(obj.gameObject);
				_networkObjects[index] = obj;
			}
		}

		private void Update()
		{
			if (isClientAuthoritative && !isClient || !isClientAuthoritative && !isServer) 
				return;
			
			foreach (var obj in _networkObjects)
			{
				var dir = new Vector3(DirectionalInput.x, 0, DirectionalInput.y);
				obj.transform.Translate(dir * Time.deltaTime, Space.Self);
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
			GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity).gameObject;
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
