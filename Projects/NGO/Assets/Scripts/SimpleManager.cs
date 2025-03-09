using System;
using Unity.Netcode;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.NGO
{
	public class SimpleManager : NetworkBehaviour
	{
		#region attributes

		[Header("References")]
		[SerializeField] private NetworkObject objectPrefab;
		[SerializeField] private bool isClientAuthoritative; 

		[Header("Values")]
		[SerializeField] private int numberOfObjects = 9;
		[SerializeField] private float spawnDistance = 1f;

		private NetworkObject[] _networkObjects;

		public static Vector2 DirectionalInput { get; private set; }

		#endregion

		#region lifecycle
		
		private void Awake()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
		}

		public override void OnNetworkSpawn()
		{
			if (!IsServer) return;
			
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
				var obj = Instantiate(objectPrefab, position, objectPrefab.transform.rotation, transform);
				obj.Spawn();
				_networkObjects[index] = obj;
			}
			
			base.OnNetworkSpawn();
		}

		private void Update()
		{
			if (!IsSpawned)
				return;
			
			if (isClientAuthoritative && !IsClient || !isClientAuthoritative && !IsServer) 
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
	}
}
