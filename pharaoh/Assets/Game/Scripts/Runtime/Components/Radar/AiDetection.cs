﻿using System.Collections;
using System.Collections.Generic;
using Pharaoh.Tools;
using Pharaoh.Tools.Debug;
using UnityEngine;

namespace Pharaoh.Gameplay.Components
{
    public class AiDetection : Detection<CircleCollider2D>
    {
        [SerializeField, Tooltip("TargetFinder FOV"), Range(1f, 360f)]
        private float overlappingFov = 270f;
        
        [SerializeField, Tooltip("Target loosing time"), Range(1f, 20f)]
        private float loosingTime = 2f;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[3];
        private float _angleToTarget;
        private int _hitSize;

        private bool _searchingTarget;
        private bool _hasLostTarget;

        protected override void FixedUpdate()
        {
            if (!canDetect) return;
            base.FixedUpdate();
            
            // if colliders are behind an obstacle, clear it
            var removables = new List<Collider2D>();
            foreach (var coll in _colliders)
            {
                Vector2 center = transform.TransformPoint(detectionCollider.offset);
                Vector2 direction = (coll.transform.position - transform.position).normalized;
                float distance = Vector2.Distance(coll.transform.position, transform.position);

                _angleToTarget = Vector2.Angle(transform.right, direction);
                _hitSize = Physics2D.RaycastNonAlloc(center, direction, _hits, distance, whatIsObstacle);
                // target is inside angle but raycast detect an obstacle, collider become null
                if (_angleToTarget > overlappingFov / 2 || (_angleToTarget <= overlappingFov / 2 && _hitSize > 0))
                {
                    removables.Add(coll);
                }
            }

            foreach (var removable in removables)
            {
                _colliders.Remove(removable);
                onOverlapExit?.Invoke(removable);
            }
        }

        public bool HasLostTarget()
        {
            if (_searchingTarget == false)
            {
                StartCoroutine(LostTarget());
                _hasLostTarget = false;
                _searchingTarget = true;
            }

            return _hasLostTarget;
        }

        private IEnumerator LostTarget()
        {
            yield return new WaitForSeconds(loosingTime);
            _hasLostTarget = true;
            _searchingTarget = false;
        }

        public bool TryGetByLayerMask(LayerMask layer, out GameObject obj)
        {
            obj = GetByLayerMask(layer);
            return obj != null;
        }
        
        public GameObject GetByLayerMask(LayerMask layer)
        {
            if (overlappedCount <= 0) return null;

            // check if the mask as multiple layers
            var layerIndexes = layer.GetLayerIndexes();
            if (layerIndexes.Length > 1)
            {
                LogHandler.SendMessage($"Too much layers for just one gameObject", MessageType.Error);
                return null;
            }

            foreach (var coll in _colliders)
            {
                if (!coll || !coll.gameObject.activeInHierarchy || !coll.gameObject.HasLayer(layer)) continue;
                return coll.gameObject;
            }

            return null;
        }

        public bool OverlapPoint(Vector2 point) => detectionCollider && detectionCollider.OverlapPoint(point);


        #region Editor Debug

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Vector2 DirectionFromAngle(float eulerY, float degreeAngle)
            {
                degreeAngle += eulerY;
                return new Vector2(Mathf.Sin(degreeAngle * Mathf.Deg2Rad), Mathf.Cos(degreeAngle * Mathf.Deg2Rad));
            }

            if (!TryGetComponent(out Collider2D coll)) return;
            Vector3 center = transform.TransformPoint(coll.offset);

            // Draws a disc around the player displaying the targeting range
            UnityEditor.Handles.color = new Color(1f, 0.7531517f, 0f, 1f);
            UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, detectionCollider.radius);

            if (!detectionCollider) return;

            var isFacingRight = Vector2.Dot(transform.right, Vector2.right) >= 0.0f;
            float eulerY = -transform.eulerAngles.z + 90f * (isFacingRight ? 1f : -1f);
            Vector3 angle0 = DirectionFromAngle(eulerY, -overlappingFov / 2);
            Vector3 angle1 = DirectionFromAngle(eulerY, overlappingFov / 2);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, center + angle0 * detectionCollider.radius);
            Gizmos.DrawLine(center, center + angle1 * detectionCollider.radius);

            if (_overlapColliders is not { Length: > 0 }) return;

            Gizmos.color = Color.green;

            foreach (var overlap in _overlapColliders)
            {
                if (overlap == null) continue;
                Gizmos.DrawLine(transform.position, overlap.transform.position);
            }
        }
#endif

        #endregion
    }
}