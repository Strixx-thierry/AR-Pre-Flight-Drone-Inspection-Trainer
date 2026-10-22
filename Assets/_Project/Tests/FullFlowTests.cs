using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DroneTrainer.Core;
using DroneTrainer.Data;
using DroneTrainer.Drone;
using DroneTrainer.Tasks;
using DroneTrainer.UI;

namespace DroneTrainer.Tests
{
    /// <summary>
    /// End-to-end run in the desk simulator: menu → place → blocked launch (FR-08) → all four
    /// checks → launch → report saved (FR-09/10). Any logged error fails the test.
    /// </summary>
    public class FullFlowTests
    {
        [UnityTest]
        public IEnumerator Menu_Loads_Home()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
            yield return new WaitForSeconds(3.5f);
            var menu = Object.FindAnyObjectByType<MenuDirector>();
            Assert.NotNull(menu, "MenuDirector missing");
            Assert.IsTrue(menu.home.IsVisible, "Home should be visible after the splash");
            Assert.NotNull(menu.home.heroImage.sprite, "Home should show the 2D drone picture");
            Assert.IsNull(Object.FindAnyObjectByType<DroneController>(), "No 3D drone in the menu");
        }

        [UnityTest]
        public IEnumerator Guided_Inspection_EndToEnd()
        {
            ProgressStore.Clear();
            GameSession.DroneId = "SKYFOLD_F4";
            GameSession.Mode = TrainingMode.Guided;
            GameSession.RandomizeFaults = false;   // fixed faults from the JSON
            SceneManager.LoadScene(SceneNames.ARInspection);
            yield return new WaitForSeconds(2.5f);

            var d = Object.FindAnyObjectByType<InspectionDirector>();
            Assert.NotNull(d, "InspectionDirector missing");
            Assert.IsFalse(d.Placement.IsAR, "Editor should use the desk simulator");
            Assert.IsTrue(d.hud.IsVisible, "3D bay should open straight into the inspection HUD");

            var tm = d.tasks;
            var drone = d.Drone;
            Assert.AreEqual(4, tm.StepCount);
            Assert.AreEqual(0, tm.CurrentStepIndex);

            // Launch with the battery unlatched must be refused (FR-08) and cost a safety error.
            Assert.IsFalse(tm.RequestLaunch());
            Assert.AreEqual(1, tm.SafetyErrors);
            yield return null;
            Assert.IsTrue(d.gate.IsVisible, "Launch-blocked modal should appear");
            d.gate.Hide();

            // Guided order: propellers are locked until the battery step is done.
            var rotor = drone.PartsOf<RotorModule>().First();
            Assert.IsFalse(tm.CanInteract(rotor, out _));

            var latch = drone.PartsOf<BatteryLatchModule>().Single();
            Assert.IsFalse(latch.IsLocked);
            tm.ToggleLatch(latch);
            Assert.IsTrue(tm.IsStepDone(0));
            Assert.AreEqual(1, tm.CurrentStepIndex);

            foreach (var r in drone.PartsOf<RotorModule>())
            {
                if (r.HasFault)
                {
                    Assert.AreEqual(VerdictResult.CorrectFault, tm.SubmitVerdict(r, airworthy: false));
                    tm.ReplaceProp(r);
                }
                else Assert.AreEqual(VerdictResult.CorrectPass, tm.SubmitVerdict(r, airworthy: true));
            }
            Assert.IsTrue(tm.IsStepDone(1), "All propellers should be safe");

            var clamp = drone.PartsOf<GimbalCoverModule>().Single();
            tm.RemoveClamp(clamp);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(tm.IsStepDone(2));

            d.hud.calibrateButton.button.onClick.Invoke();
            yield return null;
            Assert.IsTrue(d.calibration.IsVisible, "Calibration view should open");
            RaiseEvent(d.calibration, "Completed", true);
            yield return null;
            Assert.IsTrue(tm.AllDone);

            d.hud.launchButton.button.onClick.Invoke();
            yield return null;
            Assert.IsTrue(d.ready.IsVisible, "Launch-ready view should appear when all checks pass");
            RaiseEvent(d.ready.slide, "Confirmed");
            yield return new WaitForSeconds(5.5f);

            Assert.IsTrue(d.report.IsVisible, "Report should show after the launch animation");
            var result = GameSession.LastResult;
            Assert.NotNull(result);
            Assert.IsTrue(result.passed, "Run with one blocked launch should still pass");
            Assert.AreEqual(1, result.safetyErrors);
            Assert.AreEqual(90, result.score);
            Assert.AreEqual(1, ProgressStore.GetSessions("SKYFOLD_F4").Count, "Session should be saved to PlayerPrefs");
        }

        static void RaiseEvent(object target, string eventName, params object[] args)
        {
            var field = target.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            var del = field?.GetValue(target) as System.Delegate;
            Assert.NotNull(del, $"No subscribers on {target.GetType().Name}.{eventName}");
            del.DynamicInvoke(args);
        }
    }
}
