﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pharaoh.Tools
{
    public static class Helper
    {
        public static bool IsSharingSameInstance(this UnityEngine.MonoBehaviour objectToCompare,
            UnityEngine.GameObject comparisonObject)
        {
            return objectToCompare.gameObject.GetInstanceID() == comparisonObject.GetInstanceID();
        }

        public static void LookAt2D(this Transform transform, Transform target)
        {
            transform.rotation = Quaternion.AngleAxis(AngleLookAt2D(transform, target), Vector3.up);
        }

        public static void LookAt2D(this Transform transform, Vector3 target)
        {
            transform.rotation = Quaternion.AngleAxis(AngleLookAt2D(transform, target), Vector3.up);
        }

        public static float AngleLookAt2D(this Transform transform, Transform target)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        public static float AngleLookAt2D(this Transform transform, Vector3 target)
        {
            Vector2 direction = (target - transform.position).normalized;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        public static bool HasLayer(this GameObject go, LayerMask mask) => (mask.value & (1 << go.layer)) > 0;

        public static bool HasLayer(this LayerMask layerMask, int layer) => layerMask == (layerMask | 1 << layer);

        public static bool[] HasLayers(this LayerMask layerMask)
        {
            var hasLayers = new bool[32];
            for (int i = 0; i < 32; i++) hasLayers[i] = layerMask.HasLayer(i);
            return hasLayers;
        }

        public static int[] GetLayerIndexes(this LayerMask layerMask)
        {
            var hasLayers = new List<int>();
            for (int i = 0; i < 32; i++) if (layerMask.HasLayer(i)) hasLayers.Add(i);
            return hasLayers.ToArray();
        }

        public static int GetLayerIndex(this LayerMask layerMask)
        {
            for (int i = 0; i < 32; i++) if (layerMask.HasLayer(i)) return i;
            return -1;
        }


        public static bool IsCollidingHimself(this GameObject gameObject, Collider2D other, bool parentDepth = false, bool childrenDepth = false)
        {
            if (other.gameObject == gameObject) return true;
            if (!parentDepth && !childrenDepth) return false;
            
            var listColliders = new System.Collections.Generic.List<Collider2D>();

            if (parentDepth) listColliders.AddRange(gameObject.GetComponentsInParent<Collider2D>());
            if (childrenDepth) listColliders.AddRange(gameObject.GetComponentsInParent<Collider2D>());
            
            return listColliders.Count > 0 && listColliders.Any(coll => coll.gameObject == other.gameObject);
        }

        public static bool IsCollidingHimself(this GameObject gameObject, Collider other, bool parentDepth = false, bool childrenDepth = false)
        {
            if (other.gameObject == gameObject) return true;
            if (!parentDepth && !childrenDepth) return false;
            
            var listColliders = new System.Collections.Generic.List<Collider>();

            if (parentDepth) listColliders.AddRange(gameObject.GetComponentsInParent<Collider>());
            if (childrenDepth) listColliders.AddRange(gameObject.GetComponentsInParent<Collider>());

            // to exclude all potential collider of the gameObject holding this damager
            return listColliders.Count > 0 && listColliders.Any(coll => coll.gameObject == other.gameObject);
        }

        public static int OverlapNonAlloc(this Collider collider, ref Collider[] colliders, LayerMask layerMask)
        {
            int size = 0;
            Vector3 center = Vector3.zero;
            switch (collider)
            {
                case BoxCollider box:
                    center = box.transform.TransformPoint(box.center);
                    size = Physics.OverlapBoxNonAlloc(center, box.size / 2, colliders, box.transform.rotation, layerMask);
                    break;
                case SphereCollider sphere:
                    center = sphere.transform.TransformPoint(sphere.center);
                    size = Physics.OverlapSphereNonAlloc(center, sphere.radius, colliders, layerMask);
                    break;
                case CapsuleCollider capsule:
                    ///* https://roundwide.com/physics-overlap-capsule/ *///
                    var direction = new Vector3 { [capsule.direction] = 1 };
                    var offset = capsule.height / 2 - capsule.radius;
                    var point0 = capsule.transform.TransformPoint(capsule.center - direction * offset);
                    var point1 = capsule.transform.TransformPoint(capsule.center + direction * offset);
                    var r = capsule.transform.TransformVector(capsule.radius, capsule.radius, capsule.radius);
                    var radius = Enumerable.Range(0, 3).Select(xyz => xyz == capsule.direction ? 0 : r[xyz]).Select(Mathf.Abs).Max();
                    size = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, colliders, layerMask);
                    break;
                default:
                    throw new NotImplementedException("Not implemented overlap non alloc method for this collider");
            }

            return size;
        }

        public static int OverlapNonAlloc(this Collider2D collider, ref Collider2D[] colliders, LayerMask layerMask)
        {
            int size = 0;
            Vector2 point = Vector2.zero;
            switch (collider)
            {
                case BoxCollider2D box:
                    point = collider.transform.TransformPoint(box.offset);
                    size = Physics2D.OverlapBoxNonAlloc(point, box.size, box.transform.rotation.x, colliders, layerMask);
                    break;
                case CircleCollider2D sphere:
                    point = collider.transform.TransformPoint(sphere.offset);
                    size = Physics2D.OverlapCircleNonAlloc(point, sphere.radius, colliders, layerMask);
                    break;
                case CapsuleCollider2D capsule:
                    point = collider.transform.TransformPoint(capsule.offset);
                    size = Physics2D.OverlapCapsuleNonAlloc(point, capsule.size, capsule.direction, capsule.transform.rotation.x, colliders, layerMask);
                    break;
                case PolygonCollider2D polygon:
                    ContactFilter2D legacyFilter = new ContactFilter2D();
                    legacyFilter.useTriggers = Physics2D.queriesHitTriggers;
                    legacyFilter.SetLayerMask(layerMask);
                    legacyFilter.SetDepth(float.NegativeInfinity, float.PositiveInfinity);
                    point = collider.transform.TransformPoint(polygon.offset);
                    size = Physics2D.OverlapCollider(collider, legacyFilter, colliders);
                    break;
                default:
                    throw new NotImplementedException("Not implemented overlap non alloc method for this collider");
            }

            return size;
        }

        public static Collider2D[] OverlapAll(this Collider2D collider, LayerMask layerMask)
        {
            Collider2D[] colls;
            Vector2 center = Vector2.zero;
            switch (collider)
            {
                case BoxCollider2D box:
                    center = box.transform.TransformPoint(box.offset);
                    colls = Physics2D.OverlapBoxAll(center, box.size, box.transform.rotation.z, layerMask);
                    break;
                case CircleCollider2D sphere:
                    center = sphere.transform.TransformPoint(sphere.offset);
                    colls = Physics2D.OverlapCircleAll(center, sphere.radius, layerMask);
                    break;
                case CapsuleCollider2D capsule:
                    center = capsule.transform.TransformPoint(capsule.offset);
                    colls = Physics2D.OverlapCapsuleAll(center, capsule.size, capsule.direction, capsule.transform.rotation.x, layerMask);
                    break;
                default:
                    throw new NotImplementedException("Not implemented overlap all method for this collider");
            }

            return colls;
        }
    }
}