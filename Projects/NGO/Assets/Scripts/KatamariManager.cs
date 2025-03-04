using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.NGO
{
	public class KatamariManager : NetworkBehaviour
	{
		#region attributes

		[Header("References")]
		[SerializeField] private NetworkManager networkManager;
		[SerializeField] private NetworkObject objectPrefab;
		[SerializeField] private NetworkObject playerPrefab;

		[Header("Values")]
		[SerializeField] private int numberOfObjects = 49;
		[SerializeField] private float spawnDistance = 1f;

		private NetworkObject[] _networkObjects;
		private readonly Dictionary<ulong, NetworkObject> _playerObjects = new();
		
		public static Vector2 DirectionalInput { get; private set; }

		#endregion

		#region lifecycle

		public override void OnNetworkSpawn()
		{
			if (!IsServer) return;
			
			networkManager.OnClientConnectedCallback += SpawnClient;
			networkManager.OnClientDisconnectCallback += DespawnClient;
			
			_networkObjects = new NetworkObject[numberOfObjects];
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
				var obj = Instantiate(objectPrefab, position, objectPrefab.transform.rotation);
				obj.Spawn();
				obj.TrySetParent(NetworkObject);
				_networkObjects[index] = obj;
			}
			
			base.OnNetworkSpawn();
		}
		
		public override void OnNetworkDespawn()
		{
			networkManager.OnClientConnectedCallback -= SpawnClient;
			networkManager.OnClientDisconnectCallback -= DespawnClient;
			
			base.OnNetworkDespawn();
		}

		public void SetObjectNumber(int val) => numberOfObjects = val;
		public void SetDirectionalInput(Vector2 dir) => DirectionalInput = dir;

		#endregion
		
		#region private methods

		private void SpawnClient(ulong clientID)
		{
			var numOfPlayers = networkManager.ConnectedClients.Count;
			var player = Instantiate(playerPrefab, new(-3f + numOfPlayers * 1.5f, 0.5f, -5), Quaternion.identity);
			player.SpawnAsPlayerObject(clientID);
			_playerObjects.Add(clientID, player);
		}

		private void DespawnClient(ulong clientID)
		{
			if (_playerObjects.Remove(clientID, out var player))
				Destroy(player.gameObject);
		}
	
		#endregion
	}
}
