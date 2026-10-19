using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DroneTrainer.Input
{
    /// <summary>
    /// Turns touches (and mouse in the editor) into world-interaction gestures.
    /// Gestures that start on UI are ignored so HUD buttons never leak into the AR view.
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        public float tapMaxMove = 24f;
        public float tapMaxTime = 0.35f;

        public event Action<Vector2> Tapped;
        public event Action<Vector2> DragBegan;
        public event Action<Vector2, Vector2> Dragging;     // position, delta
        public event Action<Vector2, Vector2> DragEnded;    // start, end
        public event Action<Vector2> SecondaryDrag;         // two-finger pan / right mouse
        public event Action<float> Twisted;                 // degrees
        public event Action<float> Pinched;                 // ratio (>1 = spread)

        public bool Enabled { get; set; } = true;

        bool pressing, dragging, startedOnUI;
        Vector2 startPos, lastPos;
        float startTime;
        float lastTwistAngle, lastPinchDist;
        bool multi;

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            if (!Enabled) { pressing = dragging = false; return; }
            var touches = ETouch.activeTouches;
            if (touches.Count >= 2) { HandleTwoFingers(touches[0], touches[1]); return; }
            if (multi && touches.Count == 0) multi = false;
            if (multi) return;

            if (touches.Count == 1)
            {
                var t = touches[0];
                Primary(t.screenPosition,
                        t.phase == UnityEngine.InputSystem.TouchPhase.Began,
                        t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled);
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.isPressed || mouse.leftButton.wasReleasedThisFrame)
                Primary(mp, mouse.leftButton.wasPressedThisFrame, mouse.leftButton.wasReleasedThisFrame);
            if (mouse.rightButton.isPressed && !IsOverUI(mp))
                SecondaryDrag?.Invoke(mouse.delta.ReadValue());
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !IsOverUI(mp))
                Pinched?.Invoke(scroll > 0 ? 1.08f : 1f / 1.08f);
        }

        void Primary(Vector2 pos, bool began, bool ended)
        {
            if (began)
            {
                pressing = true;
                dragging = false;
                startedOnUI = IsOverUI(pos);
                startPos = lastPos = pos;
                startTime = Time.unscaledTime;
                return;
            }
            if (!pressing) return;

            if (!startedOnUI)
            {
                if (!dragging && (pos - startPos).magnitude > tapMaxMove)
                {
                    dragging = true;
                    DragBegan?.Invoke(startPos);
                }
                if (dragging && pos != lastPos) Dragging?.Invoke(pos, pos - lastPos);
            }
            lastPos = pos;

            if (ended)
            {
                pressing = false;
                if (startedOnUI) return;
                if (dragging) DragEnded?.Invoke(startPos, pos);
                else if (Time.unscaledTime - startTime <= tapMaxTime) Tapped?.Invoke(pos);
                dragging = false;
            }
        }

        void HandleTwoFingers(ETouch a, ETouch b)
        {
            Vector2 pa = a.screenPosition, pb = b.screenPosition;
            float angle = Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg;
            float dist = Vector2.Distance(pa, pb);
            Vector2 mid = (pa + pb) * 0.5f;
            if (!multi || b.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                multi = true;
                pressing = dragging = false;
                lastTwistAngle = angle;
                lastPinchDist = dist;
                lastPos = mid;
                startedOnUI = IsOverUI(pa) || IsOverUI(pb);
                return;
            }
            if (startedOnUI) return;
            Twisted?.Invoke(Mathf.DeltaAngle(lastTwistAngle, angle));
            if (lastPinchDist > 1f) Pinched?.Invoke(dist / lastPinchDist);
            SecondaryDrag?.Invoke(mid - lastPos);
            lastTwistAngle = angle;
            lastPinchDist = dist;
            lastPos = mid;
        }

        static readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        public static bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (!es) return false;
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPos }, uiHits);
            return uiHits.Count > 0;
        }
    }
}
