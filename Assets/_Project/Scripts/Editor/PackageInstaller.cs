using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace DroneTrainer.EditorTools
{
    /// <summary>Step 1: AR Foundation + ARCore + XR Plugin Management (versions picked by the Package Manager for this editor).</summary>
    public static class PackageInstaller
    {
        static readonly string[] Packages =
        {
            "com.unity.xr.arfoundation",
            "com.unity.xr.arcore",
            "com.unity.xr.management",
        };

        static AddAndRemoveRequest request;
        static Action<bool> onDone;

        public static void Install(Action<bool> done)
        {
            onDone = done;
            Debug.Log("[DroneTrainer] Installing packages: " + string.Join(", ", Packages));
            request = Client.AddAndRemove(Packages, null);
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (request == null || !request.IsCompleted) return;
            EditorApplication.update -= Poll;
            bool ok = request.Status == StatusCode.Success;
            if (ok)
            {
                foreach (var p in request.Result) Debug.Log($"[DroneTrainer] Package ready: {p.name} {p.version}");
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("Drone Trainer", "Packages installed. When Unity finishes compiling, run step 2 · Configure Project.", "OK");
            }
            else Debug.LogError("[DroneTrainer] Package install failed: " + request.Error?.message);
            request = null;
            onDone?.Invoke(ok);
        }
    }
}
