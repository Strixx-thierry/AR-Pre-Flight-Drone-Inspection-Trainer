using UnityEngine;
using UnityEngine.SceneManagement;
using DroneTrainer.Data;

namespace DroneTrainer.Core
{
    public enum TrainingMode { Guided, Assessment }

    /// <summary>Carries choices from the menu scene into the AR scene and the result back.</summary>
    public static class GameSession
    {
        public static string DroneId = "SKYFOLD_F4";
        public static TrainingMode Mode = TrainingMode.Guided;
        public static SessionResult LastResult;
        public static bool OpenRecordsOnMenu;
    }

    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string ARInspection = "ARInspection";

        public static void Load(string scene) => SceneManager.LoadScene(scene);
    }

    /// <summary>State pattern (SRS §5): PlacingState → InspectionState → EvaluationState.</summary>
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }

    public class StateMachine
    {
        public IState Current { get; private set; }

        public void Change(IState next)
        {
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick() => Current?.Tick();
    }

    /// <summary>Player settings persisted in PlayerPrefs (DMP).</summary>
    public static class AppSettings
    {
        static bool Get(string k, bool d) => PlayerPrefs.GetInt("dt.set." + k, d ? 1 : 0) == 1;
        static void Set(string k, bool v) { PlayerPrefs.SetInt("dt.set." + k, v ? 1 : 0); PlayerPrefs.Save(); }

        public static bool Sound { get => Get("sound", true); set => Set("sound", value); }
        public static bool Haptics { get => Get("haptics", true); set => Set("haptics", value); }
        public static bool Hints { get => Get("hints", true); set => Set("hints", value); }
        public static bool LeftHanded { get => Get("lefthand", false); set => Set("lefthand", value); }
        public static bool ShowPlanes { get => Get("planes", true); set => Set("planes", value); }

        public static string PilotName
        {
            get => PlayerPrefs.GetString("dt.set.pilot", "TRAINEE PILOT");
            set { PlayerPrefs.SetString("dt.set.pilot", value); PlayerPrefs.Save(); }
        }
    }

    public static class TimeFormat
    {
        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
