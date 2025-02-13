﻿using System;
using System.Linq;
using Pharaoh.Gameplay.Components;
using Pharaoh.Gameplay.Components.Movement;
using Pharaoh.Tools.Debug;
using Pharaoh.Tools.Inputs;
using UnityEngine;
using UnityEngine.Events;

namespace Pharaoh.Gameplay
{

    public class SnatchHookBehaviour : HookBehaviour
    {
        private readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();
        private Coroutine _snatchCoroutine;

        private DefenseGear _defenseGear;
        private bool _hasBeenReleased;
        
        /// <summary>
        /// test if you can pull the block 
        /// </summary>
        private bool canBePulled
        {
            get
            {
                if (!_hook) return false;
                float offset = _hook.pullData.offset;
                float x = transform.position.x;
                float hookX = _hook.transform.position.x;
                return x > (hookX + offset) || x < (hookX - offset);
            }
        }

        /// <summary>
        /// test if something is between the block & the player
        /// </summary>
        private bool isBlocked 
        {
            get
            {
                if (!_hook) return false;
                
                Vector2 position = transform.position;
                Vector2 hookPosition = _hook.center;
                Vector2 direction = hookPosition - position;
                bool isFacingRight = Mathf.Sign(-direction.x) >= 1f;

                if (direction.x >= 0.0f && !isFacingRight || direction.x <= 0.0f && isFacingRight)
                {
                    direction.x *= -1f;
                }
            
                int size = Physics2D.RaycastNonAlloc(hookPosition, direction.normalized, 
                    _hits, direction.magnitude, _hook.whatIsObstacle);
                return size > 0 && _hits[0].collider.gameObject == _hook.gameObject;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (!TryGetComponent(out _defenseGear)) {}
        }
        
        protected void OnEnable()
        {
            inputs.hookInteractStartedEvent += OnHookInteractStart;
            inputs.movePerformedEvent += OnMove;
            inputs.jumpStartedEvent += OnJump;
            inputs.dashStartedEvent += OnDash;
        }

        protected void OnDisable()
        {
            inputs.hookInteractStartedEvent -= OnHookInteractStart;
            inputs.movePerformedEvent -= OnMove;
            inputs.jumpStartedEvent -= OnJump;
            inputs.dashStartedEvent -= OnDash;
        }

        private void FixedUpdate()
        {
            if (!isCurrentTarget || !_hook || !_defenseGear) return;

            if (!canBePulled || isBlocked || _defenseGear.isGrounded)
            {
                _hasBeenReleased = true;
            }

            if (_hasBeenReleased) Release();
        }

        private void OnHookInteractStart()
        {
            if (isCurrentTarget) Release();
        }
        
        private void OnMove(Vector2 axis)
        {
            if (!isCurrentTarget) return;
            
            if (Mathf.Abs(axis.x - Mathf.Epsilon) <= Mathf.Epsilon) return;
            _hasBeenReleased = true;
        }

        private void OnJump()
        {
            if (!isCurrentTarget) return;
            _hasBeenReleased = true;
        }

        private void OnDash()
        {
            if (!isCurrentTarget) return;
            _hasBeenReleased = true;
        }

        public override void Release()
        {
            base.Release();
            if (_snatchCoroutine != null) StopCoroutine(_snatchCoroutine);
            _hasBeenReleased = false;
        }

        public override void Interact(HookCapacity hook, GameObject target)
        {
            base.Interact(hook, target);
            if (!isCurrentTarget) return;

            // can't be pull when player is in air
            // can't be pull if not the right input
            if (!canBePulled || !transform.parent || 
                !_hook.TryGetComponent(out PlayerMovement movement) || !movement.IsGrounded)
            {
                Release();
                return;
            }
            
            Debug.Log($"{hook.name} hooking to {name}");
            _snatchCoroutine = StartCoroutine(Snatch());
        }

        private System.Collections.IEnumerator Snatch()
        {
            if (!_hook) yield break;

            float force = _hook.snatchData.force;
            float offset = _hook.snatchData.offset;
            AnimationCurve curve = _hook.snatchData.curve;
            Vector2 startPosition = transform.position;

            float maxDistance = Vector2.Distance(startPosition, _hook.center);
            float timeToTravel = maxDistance / force;
            float currentTime = 0f;

            while (currentTime < timeToTravel)
            {
                nextPosition = Vector2.Lerp(startPosition, _hook.center, curve.Evaluate(currentTime / timeToTravel));
                currentTime = Mathf.MoveTowards(currentTime, timeToTravel, Time.fixedDeltaTime * force);
                Perform();

                yield return _waitForFixedUpdate;
            }

            End();
        }
    }
}