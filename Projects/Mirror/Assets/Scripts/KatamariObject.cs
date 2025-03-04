using System.Linq;
using Mirror;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.Mirror
{
	[RequireComponent(typeof(NetworkTransformUnreliable), typeof(Rigidbody), typeof(NetworkAuthorityManager))]
	public class KatamariObject : NetworkBehaviour
	{
		#region attributes

		private Transform _attachedTo;
		private float _maxDistance;

		[SerializeField] private NetworkIdentity networkObject;
		[SerializeField] private NetworkAuthorityManager networkAuthority;
		[SerializeField] private Rigidbody rb;
		[SerializeField] private float gravitationalPull = 3000;

		#endregion

		#region lifecycle

		private void Awake()
		{
			if (networkObject == null)
				networkObject = GetComponent<NetworkIdentity>();
			if (networkAuthority == null)
				networkAuthority = GetComponent<NetworkAuthorityManager>();
			if (rb == null)
				rb = GetComponent<Rigidbody>();

			networkAuthority.AuthorshipChanged += OnAuthorshipChanged;
			networkAuthority.OwnershipChanged += OnOwnershipChanged;
		}

		private void OnDestroy()
		{
			networkAuthority.AuthorshipChanged -= OnAuthorshipChanged;
			networkAuthority.OwnershipChanged -= OnOwnershipChanged;
		}

		private void FixedUpdate()
		{
            rb.isKinematic = false;
			if (!networkAuthority.IsOwner)
				return;

			var distance = Vector3.Distance(transform.position, _attachedTo.position);
			var strength = Map(distance, _maxDistance, 0, 0, gravitationalPull);
			rb.AddForce(strength * Time.fixedDeltaTime * (_attachedTo.position - transform.position));
		}

		#endregion

		#region public methods

		public void Attach(Transform trf)
		{
			if (networkAuthority.Owner != 0)
				return;
			
			_attachedTo = trf;
			networkAuthority.CmdRequestOwnership();
		}

		public void Detach()
		{
			if (!networkAuthority.IsOwner)
				return;
			
			networkAuthority.CmdReleaseOwnership();
		}

		#endregion

		#region private methods

		private void OnAuthorshipChanged(uint _)
		{
			if (networkAuthority.IsAuthor)
			{
				syncDirection = SyncDirection.ClientToServer;
			}
			else
			{
				syncDirection = SyncDirection.ServerToClient;
			}
		}
		
		private void OnOwnershipChanged(uint _)
		{
			if (networkAuthority.IsOwner)
			{
				_maxDistance = _attachedTo.GetComponents<Collider>().First(x => x.isTrigger).bounds.size.x;
			}
			else
			{
				_attachedTo = null;
				_maxDistance = 0;
			}
		}

		private void OnCollisionEnter(Collision other)
		{
			if (networkAuthority.IsAuthor 
			    && other.gameObject.TryGetComponent<KatamariObject>(out _)
			    && other.gameObject.TryGetComponent<NetworkAuthorityManager>(out var auth)
			    && auth.Owner == 0)
			{
				auth.CmdRequestAuthority();
			}
		}

		private static float Map(float value, float from1, float from2, float to1, float to2)
		{
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

		#endregion
	}
}

