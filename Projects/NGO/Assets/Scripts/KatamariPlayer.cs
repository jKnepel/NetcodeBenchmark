using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.NGO
{
	[RequireComponent(typeof(NetworkTransform), typeof(Rigidbody))]
	public class KatamariPlayer : NetworkBehaviour
	{
		#region attributes

		[SerializeField] private Rigidbody rb;
		[SerializeField] private float forceMult = 50000;

		#endregion

		#region lifecycle

		private void Awake()
		{
			if (rb == null)
				rb = GetComponent<Rigidbody>();
		}

		private void FixedUpdate()
		{
			if (!IsOwner)
				return;

			Vector2 dir = KatamariManager.DirectionalInput;
			Vector3 delta = new(dir.x, 0, dir.y);
			rb.AddForce(forceMult * Time.fixedDeltaTime * delta);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!IsOwner || !other.TryGetComponent<KatamariObject>(out var att))
				return;

			att.Attach(transform);
		}

		private void OnTriggerExit(Collider other)
		{
			if (!IsOwner || !other.TryGetComponent<KatamariObject>(out var att))
				return;

			att.Detach();
		}

		#endregion
	}
}
