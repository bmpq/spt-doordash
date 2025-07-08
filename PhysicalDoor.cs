using Comfort.Common;
using EFT;
using EFT.Interactive;
using System.Collections.Generic;
using UnityEngine;

namespace tarkin.doordash
{
    internal class PhysicalDoor : MonoBehaviour
    {
        Door door;

        Dictionary<Collider, bool> originalColliderStates = new Dictionary<Collider, bool>();
        Vector3 originalLocalPos;
        Quaternion originalLocalRot;
        int originalLayer;

        Rigidbody rb;

        BoxCollider box;
        PlayerDamager colListener;

        void Start()
        {
            door = GetComponent<Door>();
            MeshCollider meshCollider = door.Collider as MeshCollider;
            if (meshCollider == null)
            {
                Destroy(this);
                return;
            }

            originalLocalPos = transform.localPosition;
            originalLocalRot = transform.localRotation;

            foreach (var item in GetComponentsInChildren<Collider>())
            {
                originalColliderStates.Add(item, item.enabled);
                item.enabled = false;
            }

            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 50;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            originalLayer = gameObject.layer;
            gameObject.layer = LayerMaskClass.LowPolyColliderLayer;

            box = gameObject.AddComponent<BoxCollider>();
            box.enabled = true;
            box.isTrigger = false;
            SetBoxColliderDimensionsFromMesh(box, meshCollider.sharedMesh, door.Collider.transform);

            float subtractXY = 0.1f; // less chance to get stuck in the doorframe
            float addZ = 0.1f; // less chance to phase through things

            // it is unknown what orientation the box has, but we can still implement the needed logic based on the smallest dimension, which would be thickness
            Vector3 boxSize = box.size;
            Vector3 newSize = new Vector3();

            Vector3 smallestDimension = Vector3.zero;

            if (boxSize.x <= boxSize.y && boxSize.x <= boxSize.z)
            {
                // X is smallest
                newSize.x = boxSize.x + addZ;
                newSize.y = boxSize.y - subtractXY;
                newSize.z = boxSize.z - subtractXY;

                smallestDimension.x = 1;
            }
            else if (boxSize.y <= boxSize.z)
            {
                // Y is smallest
                newSize.x = boxSize.x - subtractXY;
                newSize.y = boxSize.y + addZ;
                newSize.z = boxSize.z - subtractXY;

                smallestDimension.y = 1;
            }
            else
            {
                // Z is smallest
                newSize.x = boxSize.x - subtractXY;
                newSize.y = boxSize.y - subtractXY;
                newSize.z = boxSize.z + addZ;

                smallestDimension.z = 1;
            }

            box.size = newSize;

            Vector3 playerPos = Singleton<GameWorld>.Instance.MainPlayer.Position;
            Vector3 dir = transform.position - playerPos;
            dir.y = 0; // less ugly kick this way
            dir = dir.normalized;

            rb.AddForce(dir * Plugin.DislodgeForce.Value, ForceMode.VelocityChange);
            rb.angularVelocity = Random.onUnitSphere * Plugin.DislodgeForce.Value / 2f;

            // particles are in local space for some reason, and setting the sim space to world makes them stop showing up idk why
            // so we just set the gameobject itself to world space
            door.HitEffect?.transform.SetParent(null, true);


            GameObject colDamager = new GameObject("PhysicalPlayerDamager");
            colDamager.transform.SetParent(transform, false);
            BoxCollider trigger = colDamager.AddComponent<BoxCollider>();
            trigger.size = box.size * 1.3f + smallestDimension * 0.2f;
            trigger.center = box.center;
            trigger.isTrigger = true;
            colListener = colDamager.AddComponent<PlayerDamager>();
        }

        // restore everything. used only for debugging for now.
        void OnDestroy()
        {
            Destroy(colListener.gameObject);
            Destroy(rb);
            Destroy(box);

            transform.localPosition = originalLocalPos;
            transform.localRotation = originalLocalRot;

            foreach (var kvp in originalColliderStates)
            {
                kvp.Key.enabled = kvp.Value;
            }

            gameObject.layer = originalLayer;

            if (door.InitialDoorState == EDoorState.Locked)
                door.DoorState = EDoorState.Locked;
            else
                door.DoorState = EDoorState.Shut;
        }

        static void SetBoxColliderDimensionsFromMesh(BoxCollider boxCollider, Mesh mesh, Transform meshTransform)
        {
            boxCollider.size = Vector3.Scale(mesh.bounds.size, meshTransform.localScale);
            Vector3 localCenter = Vector3.Scale(mesh.bounds.center, meshTransform.localScale);
            boxCollider.center = meshTransform.localPosition + localCenter;
        }
    }
}
