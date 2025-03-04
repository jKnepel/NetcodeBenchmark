using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.Local
{
	[RequireComponent(typeof(Rigidbody))]
	public class KatamariPlayer : MonoBehaviour
	{
		#region attributes

		[SerializeField] private Rigidbody rb;
		[SerializeField] private float forceMult = 100;

		private Vector2 _directionalInput;

		#endregion

		#region lifecycle

		private void Awake()
		{
			if (rb == null)
				rb = GetComponent<Rigidbody>();
		}

		private void FixedUpdate()
		{
			Vector3 delta = new(_directionalInput.x, 0, _directionalInput.y);
			rb.AddForce(forceMult * Time.fixedDeltaTime * delta);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!other.TryGetComponent<KatamariObject>(out var att))
				return;

			att.Attach(transform);
		}

		private void OnTriggerExit(Collider other)
		{
			if (!other.TryGetComponent<KatamariObject>(out var att))
				return;

			att.Detach();
		}

		#endregion
		
		#region public methods
		
		public void SetDirectionalInput(Vector2 dir) => _directionalInput = dir;
		
		#endregion
	}
}
