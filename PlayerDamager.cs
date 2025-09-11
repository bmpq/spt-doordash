using Comfort.Common;
using EFT;
using EFT.Ballistics;
using Systems.Effects;
using UnityEngine;

namespace tarkin.doordash
{
    public class PlayerDamager : MonoBehaviour
    {
        private Rigidbody self;

        void Start()
        {
            self = GetComponentInParent<Rigidbody>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (self == null || self.velocity.sqrMagnitude < 0.5f)
                return;

            if (other.gameObject.layer == LayerMaskClass.PlayerLayer)
            {
                if (other.gameObject.TryGetComponent<Player>(out Player hitReceiver) && !hitReceiver.IsYourPlayer)
                {
                    Hit(other.gameObject,
                        Vector3.Lerp(self.position, other.transform.position, 0.5f),
                        Vector3.Normalize(self.position - other.transform.position),
                        self.velocity);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (self == null || collision.impulse.sqrMagnitude < 0.5f)
                return;

            ContactPoint contact = collision.GetContact(0);
            Vector3 hitPoint = contact.point;
            Vector3 hitNormal = contact.normal;

            if (collision.gameObject.layer == LayerMaskClass.PlayerLayer)
            {
                if (collision.gameObject.TryGetComponent<Player>(out Player hitReceiver) && !hitReceiver.IsYourPlayer)
                {
                    Hit(collision.gameObject, hitPoint, hitNormal, self.velocity);
                }
            }
        }

        private void Hit(GameObject gameObject, Vector3 hitPoint, Vector3 hitNormal, Vector3 impulse)
        {
            BodyPartCollider[] bodyPartColliders = gameObject.GetComponentsInChildren<BodyPartCollider>();
            if (bodyPartColliders.Length == 0)
                return;

            BodyPartCollider closestBodyPart = null;
            float closestDistanceSqr = float.MaxValue;

            foreach (var bpc in bodyPartColliders)
            {
                Vector3 partPosition = bpc.transform.position;
                float distSqr = (hitPoint - partPosition).sqrMagnitude;

                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    closestBodyPart = bpc;
                }
            }

            if (closestBodyPart != null)
            {
                RaycastHit fakeHit = new RaycastHit();
                fakeHit.point = hitPoint;
                fakeHit.normal = hitNormal;
                float force = impulse.sqrMagnitude;
                Hit(impulse.normalized, closestBodyPart, fakeHit, force * 100f);
            }
        }


        public static void Hit(Vector3 impactDirection, BallisticCollider ballisticCollider, RaycastHit hit, float dmg)
        {
            if (ballisticCollider == null)
            {
                return;
            }

            var mainPlayerBridge = Singleton<GameWorld>.Instance?.GetAlivePlayerBridgeByProfileID(Singleton<GameWorld>.Instance?.MainPlayer?.ProfileId);

            DamageInfoStruct damageInfo = new DamageInfoStruct
            {
                DamageType = EDamageType.Btr,
                Damage = dmg,
                ArmorDamage = dmg * 0.5f,
                StaminaBurnRate = dmg * 0.1f,
                PenetrationPower = 5,
                MasterOrigin = hit.point,
                Direction = impactDirection,
                HitNormal = hit.normal,
                HitPoint = hit.point,
                Player = mainPlayerBridge,
                IsForwardHit = true,
                HittedBallisticCollider = ballisticCollider,

                BlockedBy = null,
                DeflectedBy = null
            };

            ballisticCollider.ApplyHit(damageInfo, ShotIdStruct.EMPTY_SHOT_ID);

            Singleton<Effects>.Instance?.Emit(ballisticCollider.TypeOfMaterial, ballisticCollider, hit.point, hit.normal, 1f);
        }
    }
}