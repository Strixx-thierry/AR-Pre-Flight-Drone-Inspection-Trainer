using System;
using System.Collections;
using UnityEngine;
using DroneTrainer.Data;

namespace DroneTrainer.Drone
{
    public enum VerdictResult { CorrectPass, CorrectFault, MissedFault, FalseAlarm }

    /// <summary>Propeller: the trainee must judge it airworthy or damaged; damaged blades get replaced.</summary>
    public class RotorModule : AInspectableComponent
    {
        public Mesh damagedMesh;
        public Material[] damagedMaterials;
        Material[] intactMaterials;
        MeshRenderer meshRenderer;
        [SerializeField] Mesh intactMesh;
        MeshFilter filter;
        Vector3 spinAxis = Vector3.up;
        float spin;          // revolutions per second

        public override PartKind Kind => PartKind.Verdict;
        public override string StatusLabel => State switch
        {
            PartState.Passed => "AIRWORTHY",
            PartState.Faulty => "DAMAGED",
            PartState.Fixed => "REPLACED",
            _ => "NOT CHECKED"
        };

        void Awake()
        {
            filter = GetComponent<MeshFilter>();
            if (filter && !intactMesh) intactMesh = filter.sharedMesh;
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer) intactMaterials = meshRenderer.sharedMaterials;
            spinAxis = transform.InverseTransformDirection(DroneRoot.up).normalized;
        }

        public override void ApplyScenario(FaultConfig faults)
        {
            HasFault = faults != null && Array.IndexOf(faults.crackedProps, partId) >= 0 && damagedMesh;
            if (filter) filter.sharedMesh = HasFault ? damagedMesh : intactMesh;
            ApplyMaterials();
            base.ApplyScenario(faults);
        }

        public VerdictResult SubmitVerdict(bool airworthy)
        {
            if (airworthy && !HasFault) { SetState(PartState.Passed); return VerdictResult.CorrectPass; }
            if (!airworthy && HasFault) { SetState(PartState.Faulty); return VerdictResult.CorrectFault; }
            if (airworthy) { Notify(); return VerdictResult.MissedFault; }   // stays unchecked
            SetState(PartState.Passed);                                       // false alarm: blade is fine
            return VerdictResult.FalseAlarm;
        }

        /// <summary>"Perform Required Fixes" use case: swap in a new propeller.</summary>
        public void Replace()
        {
            if (filter) filter.sharedMesh = intactMesh;
            HasFault = false;
            ApplyMaterials();
            SetState(PartState.Fixed);
        }

        void ApplyMaterials()
        {
            if (!meshRenderer || intactMaterials == null) return;
            meshRenderer.sharedMaterials = HasFault && damagedMaterials != null && damagedMaterials.Length > 0
                ? damagedMaterials : intactMaterials;
        }

        public void SetSpin(float revsPerSecond) => spin = revsPerSecond;

        void Update()
        {
            if (spin > 0.01f) transform.Rotate(spinAxis, spin * 360f * Time.deltaTime, Space.Self);
        }
    }
}

