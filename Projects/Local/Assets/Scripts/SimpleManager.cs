using System;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.Local
{
	public class SimpleManager : MonoBehaviour
	{
		#region attributes

		[Header("References")]
		[SerializeField] private GameObject objectPrefab;

		[Header("Values")]
		[SerializeField] private int numberOfObjects = 5;
		[SerializeField] private float spawnDistance = 1f;

		private GameObject[] _networkObjects;
		
		public static Vector2 DirectionalInput { get; private set; }

		private bool _serverStarted;

		#endregion

		#region lifecycle

		private void Awake()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
		}

		public void StartServer()
		{
			_networkObjects = new GameObject[numberOfObjects];
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

			_serverStarted = true;
		}
		
		public void StopServer()
		{
			foreach (var nobj in _networkObjects)
				Destroy(nobj.gameObject);
			_networkObjects = null;
			_serverStarted = false;
		}

		private void Update()
		{
			if (!_serverStarted)
				return;
			
			foreach (var obj in _networkObjects)
			{
				var dir = new Vector3(DirectionalInput.x, 0, DirectionalInput.y);
				obj.transform.Translate(dir * Time.deltaTime, Space.Self);
			}
		}

		public void SetObjectNumber(int val) => numberOfObjects = val;
		public void SetDirectionalInput(int _, Vector2 dir) => DirectionalInput = dir;

		#endregion
	}
}
