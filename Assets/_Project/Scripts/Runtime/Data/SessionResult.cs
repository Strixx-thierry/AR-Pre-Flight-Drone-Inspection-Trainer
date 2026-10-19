using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DroneTrainer.Data
{
    public enum ErrorSeverity { Safety, Minor }

    [Serializable]
    public class ErrorEntry
    {
        public float time;
        public string stepId;
        public string partId;
        public string message;
        public ErrorSeverity severity;
    }

    [Serializable]
    public class StepResult
    {
        public string id;
        public string title;
        public float seconds;
        public int errors;
        public bool completed;
        public string note;
    }

    [Serializable]
    public class SessionResult
    {
        public string droneId;
        public string droneName;
        public string mode;
        public float seconds;
        public int score;
        public bool passed;
        public int safetyErrors;
        public int minorErrors;
        public long utcTicks;
        public List<StepResult> steps = new List<StepResult>();
        public List<ErrorEntry> errors = new List<ErrorEntry>();

        public DateTime LocalTime => new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();
        public int TotalErrors => safetyErrors + minorErrors;
    }

    /// <summary>Offline progress history in PlayerPrefs (DMP), capped to the latest sessions.</summary>
    public static class ProgressStore
    {
        const string Key = "dt.sessions.v1";
        const int Cap = 20;

        [Serializable] class Wrapper { public List<SessionResult> items = new List<SessionResult>(); }

        static Wrapper Read()
        {
            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new Wrapper();
            try { return JsonUtility.FromJson<Wrapper>(json) ?? new Wrapper(); }
            catch { return new Wrapper(); }
        }

        public static void Add(SessionResult r)
        {
            var w = Read();
            w.items.Insert(0, r);
            if (w.items.Count > Cap) w.items.RemoveRange(Cap, w.items.Count - Cap);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(w));
            PlayerPrefs.Save();
        }

        /// <summary>Newest first.</summary>
        public static List<SessionResult> GetSessions(string droneId = null) =>
            Read().items.Where(s => droneId == null || s.droneId == droneId).ToList();

        public static float BestTime(string droneId)
        {
            var passed = GetSessions(droneId).Where(s => s.passed).ToList();
            return passed.Count == 0 ? -1 : passed.Min(s => s.seconds);
        }

        public static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}
