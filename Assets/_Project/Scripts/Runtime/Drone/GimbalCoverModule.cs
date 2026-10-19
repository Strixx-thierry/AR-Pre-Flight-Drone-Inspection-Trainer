using System;
using System.Collections;
using UnityEngine;
using DroneTrainer.Data;

namespace DroneTrainer.Drone
{
    /// <summary>Clear gimbal clamp that must be removed before power-on.</summary>
    public class GimbalCoverModule : AInspectableComponent
    {
        public float slideDistance = 0.07f;
        public bool IsFitted { get; private set; } = true;
        public override PartKind Kind => PartKind.Remove;
        public override bool IsSafe => !IsFitted;
        public override string StatusLabel => IsFitted ? "FITTED" : "REMOVED";

        Vector3 homePos;
        Renderer[] renderers;
        Collider[] colliders;
        Coroutine anim;

        void Awake()
        {
            homePos = transform.localPosition;
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
        }

        public override void ApplyScenario(FaultConfig faults)
        {
            HasFault = faults == null || faults.clampFitted;
            if (anim != null) StopCoroutine(anim);
            transform.localPosition = homePos;
            SetVisible(HasFault);
            IsFitted = HasFault;
            base.ApplyScenario(faults);
        }

        public void Remove()
        {
            if (!IsFitted) return;
            IsFitted = false;
            foreach (var c in colliders) c.enabled = false;
            if (isActiveAndEnabled) anim = StartCoroutine(SlideOff());
            else SetVisible(false);
            SetState(PartState.Passed);
        }

        IEnumerator SlideOff()
        {
            var parent = transform.parent;
            Vector3 worldDelta = DroneRoot.forward * slideDistance * DroneRoot.lossyScale.x;
            Vector3 end = homePos + (parent ? parent.InverseTransformVector(worldDelta) : worldDelta);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.45f)
            {
                transform.localPosition = Vector3.Lerp(homePos, end, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            SetVisible(false);
        }

        void SetVisible(bool v)
        {
            if (renderers == null) Awake();
            foreach (var r in renderers) if (r) r.enabled = v;
            foreach (var c in colliders) if (c) c.enabled = v;
        }
    }
}

