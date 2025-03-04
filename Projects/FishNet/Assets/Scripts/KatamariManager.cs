using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.FishNet
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
		private readonly Dictionary<NetworkConnection, NetworkObject> _playerObjects = new();
		
		public static Vector2 DirectionalInput { get; private set; }

		#endregion

		#region lifecycle

		public override void OnStartServer()
		{
			base.OnStartServer();
			networkManager.ServerManager.OnRemoteConnectionState += SpawnClient;
			
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
				obj.SetParent(this);
				Spawn(obj);
				_networkObjects[index] = obj;
			}
		}
		
		public override void OnStopServer()
		{
			networkManager.ServerManager.OnRemoteConnectionState -= SpawnClient;
		}

		public void SetObjectNumber(int val) => numberOfObjects = val;
		public void SetDirectionalInput(Vector2 dir) => DirectionalInput = dir;

		#endregion
		
		#region private methods

		private void SpawnClient(NetworkConnection networkConnection, RemoteConnectionStateArgs state)
		{
			if (state.ConnectionState == RemoteConnectionState.Started)
			{
				networkConnection.OnLoadedStartScenes += LoadedStartScenes;

				return;
				void LoadedStartScenes(NetworkConnection conn, bool asServer)
				{
					var numOfPlayers = networkManager.ServerManager.Clients.Count;
					var player = Instantiate(playerPrefab, new(-3f + numOfPlayers * 1.5f, 0.5f, -5), Quaternion.identity);
					Spawn(player, networkConnection);
					_playerObjects.Add(networkConnection, player);
				}
			}
			else
			{
				if (_playerObjects.Remove(networkConnection, out var player))
					Destroy(player.gameObject);
			}
		}
	
		#endregion
	}
}
