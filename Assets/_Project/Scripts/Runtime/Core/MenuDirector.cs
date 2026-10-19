using System.Collections;
using System.Linq;
using UnityEngine;
using DroneTrainer.Data;
using DroneTrainer.UI;
#if DT_ARFOUNDATION
using DroneTrainer.AR;
#endif

namespace DroneTrainer.Core
{
    /// <summary>Main menu flow: S01 splash → S02 home → S03 hangar → S04 briefing → AR scene; S16 records.</summary>
    public class MenuDirector : MonoBehaviour
    {
        public UIManager ui;
        public SplashView splash;
        public HomeView home;
        public HangarView hangar;
        public BriefingView briefing;
        public RecordsView records;
        public SettingsView settings;
        public HelpView help;
        public HeroStage stage;

        DroneCatalog catalog;
        DroneDefinition current;
        ScenarioConfig scenario;
        bool arReady;
        static bool splashShown;

        IEnumerator Start()
        {
            ui.HideAll();
            catalog = DroneCatalog.Load();
            if (!catalog || catalog.FirstUnlocked() == null)
            {
                Debug.LogError("[Menu] No DroneCatalog in Resources. Run Tools ▸ Drone Trainer ▸ Import Drones.");
                yield break;
            }
            current = catalog.Find(GameSession.DroneId);
            GameSession.DroneId = current.id;
            Bind();

            if (!splashShown)
            {
                splash.Show();
                splash.SetProgress(0.15f, "Checking AR support…");
#if DT_ARFOUNDATION
                yield return ARPlacementManager.CheckSupport(ok => arReady = ok);
#endif
                splash.SetProgress(0.55f, arReady ? "ARCore OK · Camera OK" :"AR not available. Desk simulator enabled.");
                yield return LoadScenario();
                splash.SetProgress(0.85f, "Loading checklist…");
                yield return new WaitForSeconds(0.6f);
                splash.SetProgress(1f, "Ready");
                yield return new WaitForSeconds(0.3f);
                splashShown = true;
            }
            else
            {
#if DT_ARFOUNDATION
                yield return ARPlacementManager.CheckSupport(ok => arReady = ok);
#endif
                yield return LoadScenario();
            }

            ShowHome();
            if (GameSession.OpenRecordsOnMenu)
            {
                GameSession.OpenRecordsOnMenu = false;
                ShowRecords();
            }
        }

        IEnumerator LoadScenario()
        {
            yield return ScenarioLoader.Load(current, s => scenario = s);
            if (scenario == null) Debug.LogError("[Menu] Scenario failed to load for " + current.id);
        }

        void Bind()
        {
            home.Settings += () => settings.Show();
            home.Help += () => help.Show();
            home.Records += ShowRecords;
            home.Hangar += () => { ui.ShowOnly(hangar); hangar.Open(catalog, current.id); };
            home.Training += ShowBriefing;
            home.QuickStart += () => SceneNames.Load(SceneNames.ARInspection);
            home.Prev += () => Cycle(-1);
            home.Next += () => Cycle(1);

            hangar.Back += ShowHome;
            // menus use the 2D picture; the 3D model only appears in the AR training
            hangar.Selected += d => StartCoroutine(SelectDrone(d));

            briefing.Back += ShowHome;
            briefing.StartRequested += mode =>
            {
                GameSession.Mode = mode;
                SceneNames.Load(SceneNames.ARInspection);
            };

            records.Back += ShowHome;
            settings.Close += () => settings.Hide();
            settings.ProgressReset += RefreshHome;
            help.Close += () => help.Hide();
        }

        IEnumerator SelectDrone(DroneDefinition d)
        {
            current = d;
            GameSession.DroneId = d.id;
            yield return LoadScenario();
            ShowBriefing();
        }

        void Cycle(int dir)
        {
            var unlocked = catalog.drones.Where(d => d && !d.locked && d.prefab).ToList();
            if (unlocked.Count < 2) return;
            int i = (unlocked.IndexOf(current) + dir + unlocked.Count) % unlocked.Count;
            StartCoroutine(SelectAndStay(unlocked[i]));
        }

        IEnumerator SelectAndStay(DroneDefinition d)
        {
            current = d;
            GameSession.DroneId = d.id;
            yield return LoadScenario();
            RefreshHome();
        }

        void ShowHome()
        {
            ui.ShowOnly(home);
            RefreshHome();
        }

        void RefreshHome()
        {
            var sessions = ProgressStore.GetSessions(current.id);
            home.Set(current, sessions.Count, ProgressStore.BestTime(current.id),
                     sessions.Count > 0 ? sessions[0].score : -1, arReady);
        }

        void ShowBriefing()
        {
            if (scenario == null) return;
            ui.ShowOnly(briefing);
            briefing.Open(current, scenario, GameSession.Mode);
        }

        void ShowRecords()
        {
            ui.ShowOnly(records);
            records.Open(ProgressStore.GetSessions(current.id), ProgressStore.BestTime(current.id));
        }
    }
}
