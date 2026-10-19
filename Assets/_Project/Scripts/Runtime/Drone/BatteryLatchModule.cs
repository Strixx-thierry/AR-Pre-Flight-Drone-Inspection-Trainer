using System;
using System.Collections;
using UnityEngine;
using DroneTrainer.Data;

namespace DroneTrainer.Drone
{
    /// <summary>Battery latch: Locked / Unlocked toggle (FR-05). Critical for launch (FR-08).</summary>
    public class BatteryLatchModule : AInspectableComponent
    {
        public Renderer[] chargeLeds = Array.Empty<Renderer>();
        public Material ledOn;
        public Material ledOff;
        public float openAngle = -28f;

        public bool IsLocked { get; private set; }
        public int Charge { get; private set; } = 4;
        public override PartKind Kind => PartKind.Toggle;
        public override bool IsSafe => IsLocked;
        public override string StatusLabel => IsLocked ? "LOCKED" : "UNLOCKED";

        Quaternion closedRot;
        Vector3 hingeAxis = Vector3.right;
        Coroutine anim;

        void Awake()
        {
            closedRot = transform.localRotation;
            var parent = transform.parent ? transform.parent : transform;
            hingeAxis = parent.InverseTransformDirection(DroneRoot.right).normalized;
        }

        public override void ApplyScenario(FaultConfig faults)
        {
            HasFault = faults == null || faults.batteryStartsUnlocked;
            Charge = faults != null ? Mathf.Clamp(faults.batteryCharge, 0, 4) : 4;
            for (int i = 0; i < chargeLeds.Length; i++)
                if (chargeLeds[i]) chargeLeds[i].sharedMaterial = i < Charge ? ledOn : ledOff;
            SetLocked(!HasFault, instant: true);
            base.ApplyScenario(faults);
        }

        public void Toggle() => SetLocked(!IsLocked, instant: false);

        void SetLocked(bool locked, bool instant)
        {
            IsLocked = locked;
            var target = locked ? closedRot : Quaternion.AngleAxis(openAngle, hingeAxis) * closedRot;
            if (anim != null) StopCoroutine(anim);
            if (instant || !isActiveAndEnabled) transform.localRotation = target;
            else anim = StartCoroutine(Rotate(target));
            if (!instant) SetState(locked ? PartState.Passed : PartState.Unchecked);
        }

        IEnumerator Rotate(Quaternion target)
        {
            var start = transform.localRotation;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.18f)
            {
                transform.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            transform.localRotation = target;
        }
    }
}

