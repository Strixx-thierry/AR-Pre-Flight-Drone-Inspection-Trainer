using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DroneTrainer.UI
{
    /// <summary>Raises <see cref="Swiped"/> when the user drags upward/forward across this rect.</summary>
    public class SwipeArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public float minDistance = 120f;
        public event Action Swiped;
        public event Action Tapped;
        Vector2 start;

        public void OnBeginDrag(PointerEventData e) => start = e.position;
        public void OnDrag(PointerEventData e) { }

        public void OnEndDrag(PointerEventData e)
        {
            var d = e.position - start;
            if (d.y > minDistance && d.y > Mathf.Abs(d.x) * 0.8f) Swiped?.Invoke();
        }

        public void OnPointerClick(PointerEventData e) { if (!e.dragging) Tapped?.Invoke(); }
    }
}
