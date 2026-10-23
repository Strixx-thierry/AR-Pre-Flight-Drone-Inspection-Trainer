using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DroneTrainer.Data;
using DroneTrainer.Drone;

namespace DroneTrainer.EditorTools
{
    /// <summary>
    /// Step 4: turns every FBX in Assets/Models/Drones into a ready prefab.
    /// Part names follow the convention from the Blender pipeline (Prop_FL, Battery_Latch, Gimbal_Clamp …).
    /// </summary>
    public static class DroneImporter
    {
        struct Look
        {
            public Color color; public float smooth, metal; public bool transparent; public Color? emission;
            public Look(float r, float g, float b, float smooth, float metal = 0, float alpha = 1, Color? emission = null)
            {
                color = new Color(r, g, b).gamma;       // Blender values are linear
                color.a = alpha;
                this.smooth = smooth; this.metal = metal; transparent = alpha < 1; this.emission = emission;
            }
        }

        static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
        {
            ["M_Body_Plastic"] = new Look(0.78f, 0.79f, 0.81f, 0.58f),
            ["M_Body_PlasticMid"] = new Look(0.60f, 0.62f, 0.65f, 0.5f),
            ["M_Body_Plastic_TALON"] = new Look(0.07f, 0.075f, 0.085f, 0.55f),
            ["M_Body_PlasticMid_TALON"] = new Look(0.85f, 0.30f, 0.04f, 0.5f),
            ["M_Dark_Plastic"] = new Look(0.06f, 0.065f, 0.07f, 0.45f),
            ["M_Propeller"] = new Look(0.035f, 0.038f, 0.042f, 0.62f),
            ["M_Propeller_Tip"] = new Look(0.92f, 0.92f, 0.92f, 0.6f),
            ["M_Propeller_Damage"] = new Look(0.72f, 0.68f, 0.6f, 0.15f),
            ["M_Motor_Metal"] = new Look(0.80f, 0.81f, 0.83f, 0.78f, 1f),
            ["M_Metal_Dark"] = new Look(0.18f, 0.19f, 0.2f, 0.7f, 1f),
            ["M_Lens_Glass"] = new Look(0.005f, 0.006f, 0.01f, 0.97f),
            ["M_Lens_Coating"] = new Look(0.05f, 0.12f, 0.35f, 0.95f, 0.6f),
            ["M_LED_White"] = new Look(1f, 0.97f, 0.88f, 0.8f, 0, 1, new Color(1f, 0.95f, 0.85f) * 3f),
            ["M_LED_Green"] = new Look(0.2f, 0.9f, 0.45f, 0.8f, 0, 1, new Color(0.25f, 1f, 0.5f) * 2.5f),
            ["M_LED_Off"] = new Look(0.25f, 0.27f, 0.28f, 0.7f),
            ["M_Rubber"] = new Look(0.02f, 0.02f, 0.022f, 0.15f),
            ["M_Clamp_Clear"] = new Look(0.85f, 0.92f, 1f, 0.92f, 0, 0.32f),
        };

        static readonly Dictionary<string, string> NiceRotor = new Dictionary<string, string>
        { ["FL"] = "front left", ["FR"] = "front right", ["RL"] = "rear left", ["RR"] = "rear right" };

        public static void Run()
        {
            Paths.Ensure(Paths.Prefabs + "/Drones");
            Paths.Ensure(Paths.Data + "/Drones");
            Paths.Ensure(Paths.Data + "/Scenarios");
            Paths.Ensure(Paths.Materials + "/Drone");
            if (!ThemeBuilder.Mat("M_Ghost")) ThemeBuilder.Run();

            var defs = new List<DroneDefinition>();
            if (Directory.Exists(Paths.Models))
            {
                foreach (var file in Directory.GetFiles(Paths.Models, "*.fbx"))
                {
                    var path = file.Replace('\\', '/');
                    if (Path.GetFileNameWithoutExtension(path).Contains("_Prop_Damaged")) continue;
                    var def = ImportDrone(path);
                    if (def) defs.Add(def);
                }
            }
            if (defs.Count == 0) Debug.LogError("[DroneTrainer] No drone FBX found in " + Paths.Models);

            defs.Add(Placeholder("HEXA_H6", "HEXA H6", "Heavy-lift hexacopter", "Bonus · locked", 3, "ADVANCED"));

            var catalog = Paths.LoadOrCreate<DroneCatalog>(Paths.Resources + "/DroneCatalog.asset");
            catalog.drones = defs;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DroneTrainer] Drone catalog: {string.Join(", ", defs.Select(d => d.id + (d.locked ? " (locked)" : "")))}");
        }

        // ------------------------------------------------------------------ per drone
        static DroneDefinition ImportDrone(string path)
        {
            string id = Path.GetFileNameWithoutExtension(path);
            ConfigureModel(path);
            var damagedPath = $"{Paths.Models}/{id}_Prop_Damaged.fbx";
            Mesh damaged = null;
            Material[] damagedMats = null;
            if (File.Exists(damagedPath))
            {
                ConfigureModel(damagedPath);
                damaged = AssetDatabase.LoadAllAssetsAtPath(damagedPath).OfType<Mesh>().FirstOrDefault();
                var dmgModel = AssetDatabase.LoadAssetAtPath<GameObject>(damagedPath);
                damagedMats = dmgModel ? dmgModel.GetComponentInChildren<MeshRenderer>()?.sharedMaterials : null;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var root = new GameObject(id);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "Model";
            inst.transform.SetParent(root.transform, false);
            Normalize(root, inst);

            var dc = root.AddComponent<DroneController>();
            dc.droneId = id;
            dc.ghostMaterial = ThemeBuilder.Mat("M_Ghost");
            var hl = ThemeBuilder.Mat("M_Highlight");
            int parts = 0;

            foreach (var key in NiceRotor.Keys)
            {
                var t = Find(root.transform, "Prop_" + key);
                if (!t) continue;
                var r = t.gameObject.AddComponent<RotorModule>();
                r.Configure("Prop_" + key, "Rotor · " + NiceRotor[key], true);
                r.damagedMesh = damaged;
                r.damagedMaterials = damagedMats;
                AddCollider(t, 1.25f, 0.035f);
                r.highlighter = AddHighlighter(t, hl);
                parts++;
            }

            var latchT = Find(root.transform, "Battery_Latch");
            var batteryT = Find(root.transform, "Battery");
            if (latchT)
            {
                var latch = latchT.gameObject.AddComponent<BatteryLatchModule>();
                latch.Configure("Battery_Latch", "Battery latch", true);
                latch.chargeLeds = Enumerable.Range(1, 4).Select(i => Find(root.transform, "Battery_LED_" + i))
                                             .Where(x => x).Select(x => x.GetComponent<Renderer>()).Where(x => x).ToArray();
                latch.ledOn = DroneMaterial("M_LED_Green");
                latch.ledOff = DroneMaterial("M_LED_Off");
                AddCollider(latchT, 1.6f, 0.03f);
                if (batteryT)
                {
                    AddCollider(batteryT, 1.1f, 0.03f);
                    batteryT.gameObject.AddComponent<InspectableProxy>().target = latch;
                    latch.highlighter = AddHighlighter(batteryT, hl);
                }
                else latch.highlighter = AddHighlighter(latchT, hl);
                parts++;
            }

            var clampT = Find(root.transform, "Gimbal_Clamp");
            if (clampT)
            {
                var clamp = clampT.gameObject.AddComponent<GimbalCoverModule>();
                clamp.Configure("Gimbal_Clamp", "Gimbal clamp", true);
                AddCollider(clampT, 1.3f, 0.03f);
                clamp.highlighter = AddHighlighter(clampT, hl);
                parts++;
            }

            var prefabPath = $"{Paths.Prefabs}/Drones/{id}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[DroneTrainer] {id}: prefab with {parts} inspectable parts → {prefabPath}");

            var def = Paths.LoadOrCreate<DroneDefinition>($"{Paths.Data}/Drones/{id}.asset");
            def.id = id;
            def.prefab = prefab;
#if DT_ADDRESSABLES
            // Addressables: the model is loaded on demand, so the catalog must NOT hold a hard reference
            def.address = RegisterAddressable(prefabPath, id);
            def.prefab = null;
#endif
            def.locked = false;
            FillMetadata(def);
            def.fallbackScenario = CopyScenario(def.scenarioFile);
            def.heroImage = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIArtPostprocessor.Folder}Drones/{id.ToLowerInvariant()}_render.png");
            EditorUtility.SetDirty(def);
            return def;
        }

#if DT_ADDRESSABLES
        static string RegisterAddressable(string assetPath, string address)
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup("Drones") ?? settings.CreateGroup("Drones", false, false, true, null,
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
            entry.address = address;
            entry.SetLabel("drone", true, true);
            EditorUtility.SetDirty(settings);
            return address;
        }
#endif

        static void ConfigureModel(string path)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.isReadable = false;
            mi.useFileScale = true;
            mi.globalScale = 1f;
            mi.bakeAxisConversion = true;      // Blender Z-up → Unity Y-up without a root rotation
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.SaveAndReimport();
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(m), DroneMaterial(m.name));
            mi.SaveAndReimport();
        }

        /// <summary>Real scale (≈32 cm span), nose +Z, landing feet on the ground at y=0.</summary>
        static void Normalize(GameObject root, GameObject model)
        {
            var cam = Find(model.transform, "Gimbal_Camera");
            var b = Bounds(model);
            if (b.size.y > Mathf.Max(b.size.x, b.size.z) * 1.2f) model.transform.Rotate(-90, 0, 0, Space.World); // came in Z-up
            if (cam && cam.position.z < Bounds(model).center.z) model.transform.Rotate(0, 180, 0, Space.World);
            b = Bounds(model);
            float span = Mathf.Max(b.size.x, b.size.z);
            if (span < 0.15f || span > 0.6f) model.transform.localScale *= 0.32f / Mathf.Max(span, 1e-4f);
            b = Bounds(model);
            model.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var f = Find(c, name);
                if (f) return f;
            }
            return null;
        }

        /// <summary>Box collider sized to the mesh, enlarged for comfortable touch (NFR usability).</summary>
        static void AddCollider(Transform t, float factor, float minMeters)
        {
            var mf = t.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) return;
            var bc = t.gameObject.AddComponent<BoxCollider>();
            var mb = mf.sharedMesh.bounds;
            float minLocal = minMeters / Mathf.Max(1e-4f, t.lossyScale.x);
            var size = mb.size * factor;
            bc.center = mb.center;
            bc.size = new Vector3(Mathf.Max(size.x, minLocal), Mathf.Max(size.y, minLocal), Mathf.Max(size.z, minLocal));
        }

        static PartHighlighter AddHighlighter(Transform t, Material mat)
        {
            var h = t.gameObject.AddComponent<PartHighlighter>();
            h.highlightMaterial = mat;
            return h;
        }

        static Material DroneMaterial(string name)
        {
            var path = $"{Paths.Materials}/Drone/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            var look = Looks.TryGetValue(name, out var l) ? l : new Look(0.6f, 0.6f, 0.62f, 0.5f);
            m.SetColor("_BaseColor", look.color);
            m.SetFloat("_Smoothness", look.smooth);
            m.SetFloat("_Metallic", look.metal);
            if (look.transparent) ThemeBuilder.MakeTransparent(m);
            if (look.emission.HasValue)
            {
                m.SetColor("_EmissionColor", look.emission.Value);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void FillMetadata(DroneDefinition d)
        {
            if (d.id == "SKYFOLD_F4")
            {
                d.displayName = "SKYFOLD F4";
                d.subtitle = "Foldable camera quadcopter";
                d.difficulty = 1;
                d.difficultyLabel = "BEGINNER";
                d.scenarioFile = "skyfold_f4_standard.json";
                d.specs = new List<SpecEntry>
                {
                    new SpecEntry("Span (unfolded)", "32 × 32 cm"),
                    new SpecEntry("Folded", "14 × 16.5 cm"),
                    new SpecEntry("Rotors", "4 · 2-blade"),
                    new SpecEntry("Camera", "Gimbal, front"),
                    new SpecEntry("Inspection points", "4 critical"),
                };
            }
            else if (d.id == "TALON_X4")
            {
                d.displayName = "TALON X4";
                d.subtitle = "Heavy foldable survey quad";
                d.difficulty = 2;
                d.difficultyLabel = "INTERMEDIATE";
                d.scenarioFile = "talon_x4_standard.json";
                d.specs = new List<SpecEntry>
                {
                    new SpecEntry("Span (unfolded)", "43 × 43 cm"),
                    new SpecEntry("Rotors", "4 · 2-blade"),
                    new SpecEntry("Battery", "Low charge risk"),
                    new SpecEntry("Inspection points", "4 critical"),
                };
            }
            else
            {
                d.displayName = string.IsNullOrEmpty(d.displayName) ? d.id.Replace('_', ' ') : d.displayName;
                d.subtitle = string.IsNullOrEmpty(d.subtitle) ? "Imported aircraft" : d.subtitle;
                if (string.IsNullOrEmpty(d.scenarioFile)) d.scenarioFile = d.id.ToLowerInvariant() + "_standard.json";
            }
        }

        static TextAsset CopyScenario(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            var src = $"{Paths.Scenarios}/{file}";
            if (!File.Exists(src)) { Debug.LogWarning("[DroneTrainer] Missing scenario " + src); return null; }
            var dst = $"{Paths.Data}/Scenarios/{file}";
            File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dst);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(dst);
        }

        static DroneDefinition Placeholder(string id, string name, string subtitle, string note, int difficulty, string label)
        {
            var d = Paths.LoadOrCreate<DroneDefinition>($"{Paths.Data}/Drones/{id}.asset");
            d.id = id;
            d.displayName = name;
            d.subtitle = subtitle;
            d.locked = true;
            d.lockedNote = note;
            d.difficulty = difficulty;
            d.difficultyLabel = label;
            d.prefab = null;
            d.specs = new List<SpecEntry> { new SpecEntry("Status", note) };
            EditorUtility.SetDirty(d);
            return d;
        }
    }
}
