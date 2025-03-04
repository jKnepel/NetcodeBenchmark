using Mirror;
using UnityEngine;

namespace jKnepel.NetcodeBenchmark.Projects.Mirror
{
    [RequireComponent(typeof(NetworkTransformUnreliable), typeof(Rigidbody))]
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
            syncDirection = SyncDirection.ClientToServer;
        }

        private void FixedUpdate()
        {
            if (!isOwned)
                return;

            Vector2 dir = KatamariManager.DirectionalInput;
            Vector3 delta = new(dir.x, 0, dir.y);
            rb.AddForce(forceMult * Time.fixedDeltaTime * delta);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isOwned || !other.TryGetComponent<KatamariObject>(out var att))
                return;

            if (authority)
                att.Attach(transform);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isOwned || !other.TryGetComponent<KatamariObject>(out var att))
                return;

            if (authority)
                att.Detach();
        }

        #endregion
    }
}