using EFT;
using EFT.Interactive;
using System.Collections.Generic;
using UnityEngine;

namespace tarkin.doordash
{
    [RequireComponent(typeof(Player))]
    public class RaycastBreacher : MonoBehaviour
    {
        Player player;

        void Start()
        {
            player = GetComponent<Player>();
        }

        void Update()
        {
            if (!Plugin.Enabled.Value)
                return;

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
            if (door == null)
                door = GetBreachableDoorInFrontOfPlayer(new Vector3(-0.1f, 1.4f, 0f));
            if (door != null)
                RamDoor(door);
        }

        Door GetBreachableDoorInFrontOfPlayer(Vector3 offsetFromFloor)
        {
            Vector3 rayOrigin = transform.position + offsetFromFloor;
            Vector3 rayDirection = transform.forward;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Plugin.RayDistance.Value, LayerMaskClass.PlayerStaticDoorMask))
            {
                Door door = hit.collider.transform.parent?.GetComponent<Door>();

                if (door == null)
                    door = hit.collider.transform.parent?.parent?.GetComponent<Door>(); // some doors have deeper colliders

                if (door == null)
                    return null;

                if (door.DoorState != EDoorState.Shut) // excludes locked
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
            door.LockForInteraction();
            door.SetUser(player);
            door.Interact(new InteractionResult(EInteractionType.Breach));
            player.ProceduralWeaponAnimation.ForceReact.AddForce(1, 4, 2);

            player.Physical.OnBreach();
        }

        public bool WillDoorSwingTowardsPlayer(Door door, Vector3 playerPosition)
        {
            Vector3 doorHingePos = door.transform.position;

            Vector3 shutNormal = door.GetDoorRotation(door.GetAngle(EDoorState.Shut))
                               * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform);

            Vector3 openNormal = door.GetDoorRotation(door.GetAngle(EDoorState.Open))
                               * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform);

            Vector3 swingDirection = shutNormal + openNormal;
            Vector3 doorToPlayer = playerPosition - doorHingePos;

            Vector2 swingDirection2D = new Vector2(swingDirection.x, swingDirection.z).normalized;
            Vector2 doorToPlayer2D = new Vector2(doorToPlayer.x, doorToPlayer.z).normalized;

            float dotProduct = Vector2.Dot(doorToPlayer2D, swingDirection2D);
            bool swingsTowardsPlayer = dotProduct > 0f;

            return swingsTowardsPlayer;
        }
    }
}
