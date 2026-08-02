using System.Reflection;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

namespace Jam6
{
    public class Jam6 : ModBehaviour
    {
        public static Jam6 Instance;
        public INewHorizons NewHorizons;
        public GameObject FuturePlanet;
        public GameObject PastPlanet;

        const float FUTURE_PLANET_APPEAR_TIME = 5 * 60;
        const float PAST_PLANET_DISAPPEAR_TIME = 15 * 60;

        public void Awake()
        {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        public void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(Jam6)} is loaded!", MessageType.Success);

            // Get the New Horizons API and load configs
            NewHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            NewHorizons.LoadConfigs(this);

            new Harmony("LeeSpork.Jam6").PatchAll(Assembly.GetExecutingAssembly());

            // Example of accessing game code.
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            NewHorizons.GetBodyLoadedEvent().AddListener(OnPlanetLoaded);
        }

        public void Update()
        {
            if (FuturePlanet != null)
            {
                if (TimeLoop.GetSecondsElapsed() >= FUTURE_PLANET_APPEAR_TIME)
                {
                    ModHelper.Console.WriteLine("Recieving planet from the future!", MessageType.Info);

                    SetFuturePlanetActive(true);

                    FuturePlanet = null; // Forget about the game object
                }
            }

            //if (PastPlanet != null)
            //{
            //    if (TimeLoop.GetSecondsElapsed() >= PAST_PLANET_DISAPPEAR_TIME)
            //    {
            //        PastPlanet.SetActive(false);
            //
            //        ModHelper.Console.WriteLine("Sent planet back in time!", MessageType.Success);
            //        PastPlanet = null; // Forget about the game object
            //    }
            //}
        }

        public void OnPlanetLoaded(string name)
        {
            //ModHelper.Console.WriteLine($"Body {name} loaded!", MessageType.Info);

            switch (name)
            {
                case "LeeSpork.Jam6.Planet.Future":
                    FuturePlanet = NewHorizons.GetPlanet(name);
                    ModHelper.Console.WriteLine("Got future planet!", MessageType.Success);
                    SetFuturePlanetActive(false);
                    
                    break;
            
                //case "LeeSpork.Jam6.Planet.Past":
                //    PastPlanet = NewHorizons.GetPlanet(name);
                //    ModHelper.Console.WriteLine("Got past planet!", MessageType.Success);
                //    break;
                
                default:
                    break;
            }
        }

        public void SetFuturePlanetActive(bool state)
        {
            FuturePlanet.transform.Find("Sector").gameObject.SetActive(state);
            FuturePlanet.transform.Find("RFVolume").gameObject.SetActive(state);
            FuturePlanet.transform.Find("Volumes").gameObject.SetActive(state);
            FuturePlanet.transform.Find("Orbit").gameObject.SetActive(state);
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            if (newScene != OWScene.SolarSystem) return;
            //ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);

            FuturePlanet = null;
            PastPlanet = null;
        }
    }

}
