using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using System.Collections.Generic;
using UnityEngine;

namespace tarkin.doordash
{
    public class RaycastBreacher : MonoBehaviour
    {
        Player player;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Update()
        {
            if (!Plugin.Enabled.Value)
                return;

            if (player == null || (player.HealthController == null || !player.HealthController.IsAlive))
            {
                Destroy(this);
                return;
            }

            CheckForRam();
        }

        private void CheckForRam()
        {
            if (Time.timeScale == 0f)
                return;

            float effectiveThresholdSqr = Plugin.VelocityThresholdSqr.Value * (Time.timeScale * Time.timeScale);
            if (player.Velocity.sqrMagnitude < effectiveThresholdSqr)
                return;

            Door door = GetBreachableDoorInFrontOfPlayer(new Vector3(0.1f, 1.4f, 0f));
            if (door != null)
                RamDoor(door);
        }

        Door GetBreachableDoorInFrontOfPlayer(Vector3 offsetFromPlayerFloor)
        {
            Vector3 rayOrigin = player.Position + player.Transform.Original.TransformDirection(offsetFromPlayerFloor);
            Vector3 rayDirection = player.Transform.Original.forward;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Plugin.RayDistance.Value, LayersMaskController.PlayerStaticDoorMask))
            {
                Door door = hit.collider.transform.parent?.GetComponent<Door>();

                if (door == null)
                {
                    door = hit.collider.transform.parent?.parent?.GetComponent<Door>(); // some doors have deeper colliders
                    if (door == null)
                        return null;
                }

                bool isStateBreachable = 
                    (door.DoorState == EDoorState.Shut) ||
                    (door.DoorState == EDoorState.Locked && Plugin.BreachLocked.Value);

                if (!isStateBreachable)
                    return null;

                if (!door.Operatable)
                    return null;

                if (WillDoorSwingTowardsPlayer(door, transform.position))
                    return null;

                return door;
            }

            return null;
        }

        void RamDoor(Door door)
        {
            bool wasLocked = door.DoorState == EDoorState.Locked;

            door.LockForInteraction();
            door.SetUser(player);
            door.Interact(new InteractionResult(EInteractionType.Breach));

            AffectPlayer(door, wasLocked);
        }

        void AffectPlayer(Door door, bool wasLocked)
        {
            player.ProceduralWeaponAnimation.ForceReact.AddForce(1, Plugin.RecoilHands.Value, Plugin.RecoilCamera.Value);

            if (Plugin.BurnStamina.Value)
                player.Physical.OnBreach();

            var dmgInfo = new DamageInfo { DamageType = EDamageType.Fall }; // EDamageType.Fall calls native logic for probability of fracture

            MaterialType doorMat = GetDoorMaterialFromBreachSound(door.BreachSound.name);
            float dmg = GetDamageFromDoorMaterial(doorMat);

            if (wasLocked)
                dmg *= Plugin.LockedBreachDamageMultiplier.Value;

            player.ActiveHealthController.ApplyDamage(Plugin.BodyPartToHurt.Value, dmg, dmgInfo);
            player.ActiveHealthController.DoContusion(Plugin.ContusionTime.Value, Plugin.ContusionStrength.Value);
        }

        private static float GetDamageFromDoorMaterial(MaterialType mat)
        {
            float baseDamage = Plugin.ArmDamageBase.Value;

            switch (mat)
            {
                case MaterialType.MetalThick:
                    return baseDamage * 2f;
                case MaterialType.Plastic:
                case MaterialType.WoodThick:
                default:
                    return baseDamage;
            }
        }

        private static MaterialType GetDoorMaterialFromBreachSound(string name)
        {
            if (name.Contains("wood"))
                return MaterialType.WoodThick;
            if (name.Contains("plastic"))
                return MaterialType.Plastic;
            if (name.Contains("grate") || name.Contains("metal"))
                return MaterialType.MetalThick;

            return MaterialType.None;
        }

        private static bool WillDoorSwingTowardsPlayer(Door door, Vector3 targetPosition)
        {
            Vector3 doorPoint = door.transform.TransformPoint(door.viewTarget1);
            Vector2 toTarget = new Vector2(targetPosition.x - doorPoint.x, targetPosition.z - doorPoint.z);

            Vector3 shutForward = door.GetDoorRotation(door.GetAngle(EDoorState.Shut))
                                  * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform);

            Vector3 openForward = door.GetDoorRotation(door.GetAngle(EDoorState.Open))
                                 * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform);

            Vector3 swingBisector = shutForward + openForward;
            Vector2 swingDir2D = new Vector2(swingBisector.x, swingBisector.z);

            return Vector2.Dot(toTarget.normalized, swingDir2D.normalized) > 0f;
        }
    }
}
