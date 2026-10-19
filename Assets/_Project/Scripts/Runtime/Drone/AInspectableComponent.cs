using System;
using UnityEngine;
using DroneTrainer.Data;

namespace DroneTrainer.Drone
{
    public enum PartKind { Verdict, Toggle, Remove }
    public enum PartState { Unchecked, Passed, Faulty, Fixed }

    /// <summary>
    /// Abstract base for every inspectable drone part (SRS §3 class model).
    /// Internal flags stay private; the inspection logic only uses the public surface,
    /// and the TaskManager observes <see cref="StateChanged"/> (Observer pattern).
    /// </summary>
    public abstract class AInspectableComponent : MonoBehaviour
    {
        [SerializeField] protected string partId;
        [SerializeField] protected string displayName;
        [SerializeField] protected bool isCritical = true;
        public PartHighlighter highlighter;

        public string PartId => partId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? partId : displayName;
        public bool IsCritical => isCritical;
        public PartState State { get; private set; } = PartState.Unchecked;
        /// <summary>Ground truth for this run — the trainee has to discover it.</summary>
        public bool HasFault { get; protected set; }
        public abstract PartKind Kind { get; }
        /// <summary>True when the part is fit to fly.</summary>
        public virtual bool IsSafe => State == PartState.Passed || State == PartState.Fixed;
        public virtual string StatusLabel => State.ToString().ToUpperInvariant();

        public event Action<AInspectableComponent> StateChanged;

        public void Configure(string id, string niceName, bool critical)
        {
            partId = id;
            displayName = niceName;
            isCritical = critical;
        }

        protected Transform DroneRoot
        {
            get
            {
                var c = GetComponentInParent<DroneController>();
                return c ? c.transform : transform.root;
            }
        }

        /// <summary>Resets the part to the scenario's starting condition.</summary>
        public virtual void ApplyScenario(FaultConfig faults) => SetState(PartState.Unchecked, false);

        protected void SetState(PartState s, bool notify = true)
        {
            State = s;
            if (notify) StateChanged?.Invoke(this);
        }

        protected void Notify() => StateChanged?.Invoke(this);
    }
}
