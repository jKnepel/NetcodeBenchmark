using System;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.Local
{
	public class KatamariManager : MonoBehaviour
	{
		#region attributes

		[Header("References")]
		[SerializeField] private KatamariObject objectPrefab;
		[SerializeField] private KatamariPlayer playerPrefab;

		[Header("Values")]
		[SerializeField] private int numberOfObjects = 49;
		[SerializeField] private float spawnDistance = 1f;

		private KatamariObject[] _networkObjects;
		private readonly List<KatamariPlayer> _playerObjects = new();
		
		#endregion

		#region lifecycle

		public void StartServer()
		{
			_networkObjects = new KatamariObject[numberOfObjects];
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
				_networkObjects[index] = obj;
			}
		}

		public void StopServer()
		{
			foreach (var nobj in _networkObjects)
				Destroy(nobj.gameObject);
			_networkObjects = null;
			foreach (var player in _playerObjects)
				Destroy(player.gameObject);
			_playerObjects.Clear();
		}
		
		public void StartClient()
		{
			var player = Instantiate(playerPrefab, new(-3f + 1.5f * (_playerObjects.Count + 1), 0.5f, -5), Quaternion.identity);
			_playerObjects.Add(player);
		}

		public void StopClient()
		{
			if (_playerObjects.Count == 0)
				return;
			
			Destroy(_playerObjects[^1].gameObject);
			_playerObjects.RemoveAt(_playerObjects.Count - 1);
		}

		public void SetObjectNumber(int val) => numberOfObjects = val;
		public void SetDirectionalInput(int clientID, Vector2 dir) => _playerObjects[clientID].SetDirectionalInput(dir);

		#endregion
	}
}
