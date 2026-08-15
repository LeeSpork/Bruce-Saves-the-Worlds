using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Jam6
{
    public class Jam6 : ModBehaviour
    {
        public static Jam6 Instance;
        public INewHorizons NewHorizons;
        public GameObject FuturePlanet;
        public GameObject PastPlanet;
        public GameObject TemporalBlackHoleBody;

        const float FUTURE_PLANET_APPEAR_TIME = 5 * 60;
        const float PAST_PLANET_DISAPPEAR_TIME = 15 * 60;

        int pastPlanetWarning;
        bool worldsSaved;

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
            ModHelper.Console.WriteLine($"Thank you for playing LeeSpork's Mod Jam 6 entry!", MessageType.Success);

            // Get the New Horizons API and load configs
            NewHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            NewHorizons.LoadConfigs(this);

            new Harmony("LeeSpork.Jam6").PatchAll(Assembly.GetExecutingAssembly());

            // Example of accessing game code.
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            NewHorizons.GetBodyLoadedEvent().AddListener(OnPlanetLoaded);
            
            // Add extention to New Horizons planet config
            NewHorizons.GetBodyLoadedEvent().AddListener((name) =>
            {
                //ModHelper.Console.WriteLine($"Body {name} loaded!", MessageType.Info);
                var infos = NewHorizons.QueryBody<PropReskinnerInfo[]>(name, "$.extras.LeeSpork_Jam6_Reskins");

                if (infos == null) return;

                var planet = NewHorizons.GetPlanet(name);
                ModHelper.Console.WriteLine($"Reskinning stuff on {name}", MessageType.Info);

                foreach (PropReskinnerInfo info in infos)
                {
                    foreach (string path in info.props)
                    {
                        var prop = planet.transform.Find(path).gameObject;
                        ReskinObject(prop, info);
                    }
                }
            });
        }

        public void Update()
        {
            if (FuturePlanet != null)
            {
                if (TimeLoop.GetSecondsElapsed() >= FUTURE_PLANET_APPEAR_TIME)
                {
                    ModHelper.Console.WriteLine("Recieving planet from the future!", MessageType.Info);

                    SetFuturePlanetActive(true);
                    DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_FUTURE_PLANET_ARRIVED", true);

                    FuturePlanet = null; // Forget about the game object I no longer care about it
                }
            }

            switch (pastPlanetWarning)
            {
                case 0:
                    if (TimeLoop.GetSecondsElapsed() >= (PAST_PLANET_DISAPPEAR_TIME - 60 * 8))
                    {
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_1", true);
                        // play AudioType.NomaiHologramDeactivate maybe
                        pastPlanetWarning += 1;
                    }
                    break;
                case 1:
                    if (TimeLoop.GetSecondsElapsed() >= (PAST_PLANET_DISAPPEAR_TIME - 60 * 4))
                    {
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_1", false);
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_2", true);
                        pastPlanetWarning += 1;
                    }
                    break;
                case 2:
                    if (TimeLoop.GetSecondsElapsed() >= (PAST_PLANET_DISAPPEAR_TIME - 60 * 2))
                    {
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_2", false);
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_3", true);
                        pastPlanetWarning += 1;
                    }
                    break;
                case 3:
                    if (TimeLoop.GetSecondsElapsed() >= (PAST_PLANET_DISAPPEAR_TIME - 60 * 1))
                    {
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_3", false);
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_4", true);
                        pastPlanetWarning += 1;
                    }
                    break;
                case 4:
                    if (TimeLoop.GetSecondsElapsed() >= (PAST_PLANET_DISAPPEAR_TIME - 30))
                    {
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_4", false);
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_5", true);
                        pastPlanetWarning += 1;
                    }
                    break;
                case 5:
                    if (TimeLoop.GetSecondsElapsed() >= (PAST_PLANET_DISAPPEAR_TIME))
                    {
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_5", false);
                        DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_DEPARTED", true);
                        pastPlanetWarning += 1;

                        // Play audio one-shot of planet falling into a black hole
                        // TODO play at location of planet instead of at player?
                        Locator.GetPlayerAudioController().PlayOneShotInternal(AudioType.BH_BlackHoleEmission);
                    }
                    break;
                case 6:
                    // Clean up black hole
                    if (TemporalBlackHoleBody == null)
                    {
                        pastPlanetWarning += 1;
                    }
                    else if (TimeLoop.GetSecondsElapsed() >= (PAST_PLANET_DISAPPEAR_TIME + 60 * 2))
                    {
                        DeleteBlackHole();
                        pastPlanetWarning += 1;
                    }
                    break;
            }

            if (worldsSaved == false)
            {
                if (DialogueConditionManager.SharedInstance.GetConditionState("LEESPORK_JAM6_SAVED_THE_WORLDS"))
                {
                    OnWorldsSaved();
                }
            }
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
                //    ReskinObject(PastPlanet);
                //    break;

                case "LeeSpork.Jam6.TemporalBlackHole":
                    TemporalBlackHoleBody = NewHorizons.GetPlanet(name);
                    ModHelper.Console.WriteLine("Got planet-destroying black hole!", MessageType.Success);
                    break;

                case "LeeSpork.Jam6.Sun":
                    // TODO = NewHorizons.GetPlanet(name);
                    ModHelper.Console.WriteLine("Got the local sun!", MessageType.Success);
                    break;

                default:
                    break;
            }
        }

        public void OnWorldsSaved()
        {
            worldsSaved = true;
            ModHelper.Console.WriteLine("Saved the worlds!", MessageType.Success);

            // Prevent past planet from disapearing
            DeleteBlackHole();

            // Prevent sun from blowing up
            // TODO

            // Move sun stations to low solar orbit
            // Actually no I can do that in NH
        }

        public void SetFuturePlanetActive(bool active)
        {
            FuturePlanet.transform.Find("Sector").gameObject.SetActive(active);
            FuturePlanet.transform.Find("RFVolume").gameObject.SetActive(active);
            FuturePlanet.transform.Find("Volumes").gameObject.SetActive(active);
            FuturePlanet.transform.Find("Orbit").gameObject.SetActive(active);

            if (active)
            {
                // Play audio one-shot
                // TODO play at location of planet instead of at player?
                Locator.GetPlayerAudioController().PlayOneShotInternal(AudioType.VesselSingularityCollapse);
            }
        }

        public void DeleteBlackHole()
        {
            // Delete black hole
            TemporalBlackHoleBody.SetActive(false);

            // Clear warnings about planet being consumed
            pastPlanetWarning = -1;
            DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_1", false);
            DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_2", false);
            DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_3", false);
            DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_4", false);
            DialogueConditionManager.SharedInstance.SetConditionState("LEESPORK_JAM6_PAST_PLANET_WARNING_5", false);
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            if (newScene != OWScene.SolarSystem) return;
            //ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);

            FuturePlanet = null;
            PastPlanet = null;
            pastPlanetWarning = 0;
            worldsSaved = false;
        }



    
        public void ReskinObject(GameObject prop, PropReskinnerInfo info)
        {
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
            {
                renderer.materials = [.. renderer.materials.Select(material => GetReplacementMaterial(ref material, info))];
            }
        }

        public Material OWMat(string name) // Get a material that is already in the game
        {
            return Resources.FindObjectsOfTypeAll<Material>().First(x => x.name.Contains(name));
        }
        public Texture OWTex(string name) // Get a material that is already in the game
        {
            return Resources.FindObjectsOfTypeAll<Texture>().First(x => x.name.Contains(name));
        }

    private Material GetReplacementMaterial(ref Material material, PropReskinnerInfo info)
        {

            //PaintedDetailsMode details = PaintedDetailsMode.Clean;
            PaintedDetailsMode details = info.details;

            string baseMat, metalMat, detailedMat,
                baseTex;

            baseMat = "Structure_NOM_PorcelainClean_mat";
            baseTex = "Structure_NOM_PorcelainClean_d";
            metalMat = "Structure_NOM_Silver_mat";
            detailedMat = "Structure_NOM_SilverPorcelain_mat";

            static void ReplaceTexturesFrom(Material dest, Material source, string subtexture = "")
            {
                dest.SetTexture($"_{subtexture}MainTex", source.mainTexture);
                dest.SetTexture($"_{subtexture}MetallicGlossMap", source.GetTexture("_MetallicGlossMap"));
                dest.SetTexture($"_{subtexture}BumpMap", source.GetTexture("_BumpMap"));
            }

            string materialName = material.name;
            if (materialName.EndsWith(" (Instance)"))
            {
                materialName = materialName.Remove(materialName.Length - 11);
            }

            // TODO fix shit having LOD textures

            if (material.name.Contains("Structure_NOM_SandStone_mat")
                || material.name.Contains("Structure_NOM_SandStone_Dark_mat")
                || material.name.Contains("Structure_NOM_WallInside_mat") // Detail: stained red, very worn.
                || material.name.Contains("Structure_NOM_Ceiling_mat")
                || material.name.Contains("Structure_NOM_Shuttle_mat") // TODO
                || material.name.Contains("Props_NOM_SmallTractorBeam_mat") // Detail1: TrimPattern. Detail2: Grooves_Red. Detail3: also Grooves_Red.
                || material.name.Contains("Props_NOM_LargeTractorBeam_mat") // TODO
                || material.name.Contains("Structure_NOM_PorcelainBroken_mat") // Masks of Nomai Grave guys
                || material.name.Contains("Structure_NOM_Spiral_Red_mat") // Toy ship
                || material.name.Contains("Structure_NOM_Spiral_Green_mat") // Seen on Ash Twin
                || material.name.Contains("Structure_NOM_Spiral_Yellow_mat") // Seen in Sun Station. Actually more of a tangerine orange.
                || material.name.Contains("ObservatoryInterior_HEA_VillagePlanks_mat")
                || material.name.Contains("Terrain_GD_IslandCliff_mat")
                || material.name.Contains("Terrain_GD_RockPrefab_mat")
                || material.name.Contains("Terrain_GD_Moss_mat")
                )
            {
                return OWMat(baseMat);
            }
            else if (material.name.Contains("Structure_NOM_TrimPatternLines_mat")
                || material.name.Contains("Structure_NOM_Grooves_mat") // Detail: zigzag/diamond. Seen on stairs.
                || material.name.Contains("Props_NOM_Computer_mat")
                || material.name.Contains("Structure_NOM_WhiteBoardTile_mat") // Nomai staff keypad
                || material.name.Contains("Structure_NOM_Zigzag_mat") // sandstone but with glass texture as detail ? Rare, seen on Ash Twin, e.g. Ember Twin tower's tractor beam.
                || material.name.Contains("Structure_NOM_Spiral_mat") // 
                )
            {
                // Replace main texture only
                material.SetTexture("_MainTex", OWTex(baseTex));
            }
            if (material.name.Contains("Props_NOM_MaskPainted_mat") // Texture has quarters red, white, turquoise-green, yellow
                )
            {
                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        return material;
                    case PaintedDetailsMode.Clean:
                        return OWMat(baseMat);
                    case PaintedDetailsMode.AltMaterial:
                        return OWMat(detailedMat);
                }
            }
            else if (material.name.Contains("Structure_NOM_PropTile_Color_mat") // Diamonds pattern, yellow and blueish. Used for: SimpleChair (aka bench); Container (aka box with spout).
                || material.name.Contains("Structure_NOM_HexagonTile_mat") // teal and yellow diamonds with space. Used on bed.
                || material.name.Contains("Structure_NOM_WallOutside_mat") // Detail: diagonal square carvings, worn.
                || material.name.Contains("Structure_NOM_Zigzag_Color_mat")
                || material.name.Contains("Structure_NOM_RotatingDoor_mat") // _DetailAlbedoMap _DetailNormalMap
                )
            {
                //_DetailMainTex _DetailMetallicGlossMap _DetailBumpMap

                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        // Replace main texture only
                        material.SetTexture("_MainTex", OWTex(baseTex));
                        return material;
                    case PaintedDetailsMode.Clean:
                        return OWMat(baseMat);
                    case PaintedDetailsMode.AltMaterial:
                        return OWMat(detailedMat);
                }
            }
            else if (material.name.Contains("Structure_NOM_TrimPattern_mat") // Atlas of horizontal strips. Details and colour on furnature, Solanum's mask, small box, detailed warp reciever...
                || material.name.Contains("Structure_NOM_Whiteboard_mat") // Whiteboard (very helpful comment I know)
                || material.name.Contains("Structure_NOM_WhiteboardSmall_mat") // Not sure?
                )
            {
                //_DetailMainTex _DetailMetallicGlossMap _DetailBumpMap

                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        // Replace main texture only
                        material.SetTexture("_MainTex", OWTex(baseTex));
                        return material;
                    case PaintedDetailsMode.Clean:
                        return OWMat(baseMat);
                    case PaintedDetailsMode.AltMaterial:
                        return OWMat(metalMat); // Keep it simple
                }
            }
            else if (material.name.Contains("Props_NOM_Scroll_mat"))
            {
                ReplaceTexturesFrom(material, OWMat(baseMat));
                material.SetTexture("_Detail2MainTex", OWMat(metalMat).mainTexture);
            }
            else if (material.name.Contains("Structure_NOM_Airlock_mat"))
            {
                ReplaceTexturesFrom(material, OWMat(baseMat));
                ReplaceTexturesFrom(material, OWMat(metalMat), "Detail4");
                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        return material;
                    case PaintedDetailsMode.Clean:
                        return OWMat(baseMat); // TODO
                    case PaintedDetailsMode.AltMaterial:
                        return OWMat(detailedMat); // TODO
                }
            }
            else if (material.name.Contains("Structure_NOM_WarpReceiver_mat"))
            {
                ReplaceTexturesFrom(material, OWMat(baseMat));

                // Structure_NOM_Spiral_Red
                material.SetTexture("_Detail1MainTex", null);
                material.SetTexture("_Detail1MetallicGlossMap", null);
                material.SetTexture("_Detail1BumpMap", null);

                // Structure_NOM_Grooves
                //material.SetTexture("_Detail2MainTex", PropReskinner.Instance.replacementMaterialManager.silverPorcelain_albedo);
                //material.SetTexture("_Detail2MetallicGlossMap", PropReskinner.Instance.replacementMaterialManager.silverPorcelain_metallicGloss);
                //material.SetTexture("_Detail2BumpMap", PropReskinner.Instance.replacementMaterialManager.silverPorcelain_bump);

                ReplaceTexturesFrom(material, OWMat(metalMat), "Detail4");

                material.shader = UnityEngine.Shader.Find("Standard"); // Otherwise it will still be inexplicably sandstone-coloured
            }
            else if (material.name.Contains("Structure_NOM_GravityCannon_mat"))
            {
                ReplaceTexturesFrom(material, OWMat(baseMat));

                // Structure_NOM_Spiral_Red_d - used on inside of gravity cannon tube
                material.SetTexture("_Detail1MainTex", null);
                material.SetTexture("_Detail1MetallicGlossMap", null);
                material.SetTexture("_Detail1BumpMap", null);

                // Structure_NOM_Spiral_Yellow_d - used on inside of gravity cannon tube
                material.SetTexture("_Detail2MainTex", null);
                material.SetTexture("_Detail2MetallicGlossMap", null);
                material.SetTexture("_Detail2BumpMap", null);

                // Structure_NOM_WovenGrooves_d - floor tiles where some are painted (used for gravity cannon's path bit)
                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        break;
                    case PaintedDetailsMode.Clean:
                        ReplaceTexturesFrom(material, OWMat(baseMat), "Detail3");
                        break;
                    case PaintedDetailsMode.AltMaterial:
                        ReplaceTexturesFrom(material, OWMat(detailedMat), "Detail3");
                        break;
                }

                // _Detail4MainTex _Detail4MetallicGlossMap _Detail4BumpMap : OrbitalProbeCannon_NOM_Diamonds_d // TODO
            }
            else if (material.name.Contains("Structure_NOM_Floor_mat")  // floor tiles where some are painted.
                || material.name.Contains("Structure_NOM_WovenGrooves_mat") // Version Seen on Big bridges (BH, TT, ATP)
                )
            {
                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        // Replace main texture only
                        material.SetTexture("_MainTex", OWTex(baseTex));
                        return material;
                    case PaintedDetailsMode.Clean:
                        return OWMat(baseMat);
                    case PaintedDetailsMode.AltMaterial:
                        ReplaceTexturesFrom(material, OWMat(baseMat));
                        ReplaceTexturesFrom(material, OWMat(detailedMat), "Detail");
                        return material;
                }
            }
            else if (material.name.Contains("Structure_NOM_StarHexagon_Glow_mat") // Gravity floors ON
                || material.name.Contains("IntactModule_NOM_RemoteViewerFloor_mat") // very similar if not the same as above
                || material.name.Contains("IntactModule_NOM_HologramFloor_mat") // Gravity floor but different
                )
            {
                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        // Replace main texture only
                        material.SetTexture("_MainTex", OWTex(baseTex));
                        return material;
                    case PaintedDetailsMode.Clean:
                        return OWMat(baseMat);
                    case PaintedDetailsMode.AltMaterial:
                        return OWMat("Structure_NOM_SilverPorcelainGlow_mat");
                }
                //material.SetTexture("_DetailMainTex", null);
                //material.SetTexture("_DetailMetallicGlossMap", null);
                //material.SetTexture("_DetailBumpMap", null);
            }
            else if (material.name.Contains("Structure_NOM_StarHexagon_mat") // gravity floors OFF
                || material.name.Contains("IntactModule_NOM_HologramFloorBroken_mat") // gravity floor but different off
                )
            {
                switch (details)
                {
                    case PaintedDetailsMode.Keep:
                        // Replace main texture only
                        material.SetTexture("_MainTex", OWTex(baseTex));
                        return material;
                    case PaintedDetailsMode.AltMaterial:
                        return OWMat(detailedMat); // Assuming it looks like Structure_NOM_SilverPorcelainGlow_mat but not glowing
                    case PaintedDetailsMode.Clean:
                        return OWMat(baseMat);
                }
            }
            else if (material.name.Contains("Structure_NOM_OrbTrack_mat")
                || material.name.Contains("Structure_NOM_ProbeWindow_mat")
                )
            {
                // TODO isn't there a circle version of the orb track material?
                material.SetTexture("_DetailAlbedoMap", OWTex(baseTex));
            }
            else if (material.name.Contains("Structure_NOM_Copper_mat")
                || material.name.Contains("Structure_NOM_CopperOld_mat")
                || material.name.Contains("Structure_NOM_CopperOld_Dark_mat")
                || material.name.Contains("ObservatoryInterior_HEA_VillageMetal_mat")
                )
            {
                return OWMat(metalMat);
            }
            else if (material.name.Contains("Structure_NOM_SandStone_Darker_mat") // ???? TODO
                || material.name.Contains("Structure_NOM_Grooves_Red_mat") // Stairs found on StatueIsland, SmallBowl
                || material.name.Contains("Props_NOM_Mask_Trim_mat") // Post-crash guys have lines connected with circles. Pre-crash guys just have SilverPorcelain material.
                )
            {
                return OWMat(detailedMat);
            }
            else if (material.name.Contains("Props_NOM_WarpCore_mat")) // Black & White Warp Cores
            {
                var mat = OWMat(baseMat);
                ReplaceTexturesFrom(material, mat);
                ReplaceTexturesFrom(material, OWMat(metalMat), "Detail1");

                // _Detail2 = Structure_NOM_Zigzag (which looks like glass's texture)

                // _Detail3 = Structure_NOM_Grooves_Green
                material.SetTexture("_Detail3MainTex", mat.mainTexture);
                //material.SetTexture("_Detail3MetallicGlossMap", base_metallicGloss);
                //material.SetTexture("_Detail3BumpMap", base_bump);

                // _Detail4 = Structure_NOM_Grooves
            }
            else if (material.name.Contains("Props_NOM_Lamp_mat"))
            {
                return OWMat("Props_NOM_VesselLamp_mat");
            }
            else if (material.name.Contains("Character_NOM_NomaiDirty_v2_mat"))
            {
                return OWMat("Character_NOM_NomaiDirty_Advanced_mat");
            }
            else if (material.name.Contains("Character_NOM_NomaiDirty_R_v2_mat"))
            {
                return OWMat("Character_NOM_NomaiDirty_Advanced_R_mat");
            }
            //else if (material.name.Contains("Structure_NOM_OrbTrack_mat"))
            //{
            //    material.color = new Color(999f, 999f, 999f, 1f); // Turns anything white
            //}
            else if (material.name.Contains("Props_HEA_Lightbulb_mat"))
            {
                material.SetColor("_EmissionColor", new Color(0.6f, 0.7f, 0.8f));
            }

            return material;
        }
    }
}
