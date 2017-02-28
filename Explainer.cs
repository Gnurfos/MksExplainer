using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KSP.UI.Screens;
using UnityEngine;
using USITools;

namespace Explainer
{
    /*
     * TODO color codes:
     *  - term visible in game popups
     *  - due to part settings
     *  - due to crew
     *  - due to environment (planetary abundance, part temp ...)
     *  - due to resource storage
     * 
     * TODO:
     *  - kolony wide efficiency parts
     *  - recyclers explainer
     *  - true kolonization bonuses impact, not always geo*geo
     *  - kolonization bonuses for drills
     */

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ExplainerGui : MonoBehaviour
    {
        private ApplicationLauncherButton launcherButton;
        private Rect _windowPosition = new Rect(300, 60, 820, 400);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _scrollStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        public static bool display = false;

        private BestCrewSkillLevels bestCrewSkillLevels;
        private Part selectedPart;

        void Awake()
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Icon.png");
            texture.LoadImage(File.ReadAllBytes(textureFile));
            launcherButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT, texture);
        }

        private void GuiOn()
        {
            display = true;
        }

        public void Start()
        {
            if (!_hasInitStyles)
                InitStyles();
        }

        private void GuiOff()
        {
            display = false;
            bestCrewSkillLevels = null;
        }

        private void OnGUI()
        {
            if (!display)
                return;
            Ondraw();
        }

        private void Ondraw()
        {
            _windowPosition = GUILayout.Window(42, _windowPosition, OnWindow, "MKS Explainer", _windowStyle);
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        private void GenerateWindow()
        {
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(800), GUILayout.Height(350));
            GUILayout.BeginVertical();
            try
            {
                Display();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.StackTrace);
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        private void Display()
        {
            var vessel = FlightGlobals.ActiveVessel;
            DisplayHeader(vessel);
            if (selectedPart)
            {
                if (GUILayout.Button("Back"))
                {
                    selectedPart = null;
                }
                DisplayPart(vessel, selectedPart);
            }
            else
            {
                DisplayPartList(vessel);
            }
        }

        private void DisplayHeader(Vessel vessel)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vessel: " + vessel.GetName(), _labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        private class PartCounterByName
        {
            Dictionary<string, int> lastPartId = new Dictionary<string, int>();
            public int next(string partName)
            {
                if (!lastPartId.ContainsKey(partName))
                    lastPartId[partName] = 0;
                return ++lastPartId[partName];
            }
        }

        private void DisplayPartList(Vessel vessel)
        {
            var counter = new PartCounterByName();
            foreach (var part in vessel.parts)
            {
                if (part.FindModuleImplementing<ModuleResourceHarvester_USI>()
                    || part.FindModuleImplementing<ModuleResourceConverter_USI>())
                {
                    var label = String.Format("{0} #{1}", part.name, counter.next(part.name));
                    if (GUILayout.Button(label))
                    {
                        selectedPart = part;
                    }
                }
            }
        }

        private void DisplayPart(Vessel vessel, Part part)
        {
            if (part.FindModuleImplementing<ModuleResourceHarvester_USI>())
            {
                PrintLine("Harvesters: ");
                foreach (var mod in part.FindModulesImplementing<ModuleResourceHarvester_USI>())
                {
                    DrillsExplainer.DisplayHarvesterModule(mod, vessel, part, GetBestCrewSkillLevels(vessel));
                }
            }

            if (part.FindModuleImplementing<ModuleResourceConverter_USI>())
            {
                PrintLine("Converters: " + part.name);
                foreach (var mod in part.FindModulesImplementing<ModuleResourceConverter_USI>())
                {
                    ConverterExplainer.DisplayConverterModule(mod, vessel, part, GetBestCrewSkillLevels(vessel));
                }
            }
        }

        private BestCrewSkillLevels GetBestCrewSkillLevels(Vessel vessel)
        {
            if (bestCrewSkillLevels == null)
            {
                bestCrewSkillLevels = new BestCrewSkillLevels();
                foreach (var c in vessel.GetVesselCrew())
                {
                    foreach (var e in c.experienceTrait.Effects)
                    {
                        bestCrewSkillLevels.updateWith(e.Name, c.name, c.experienceLevel);
                    }
                }
            }
            return bestCrewSkillLevels;
        }

        private void PrintLine(string content, int margin=0)
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(content, _labelStyle, GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        internal void OnDestroy()
        {
            if (launcherButton == null)
                return;
            ApplicationLauncher.Instance.RemoveModApplication(launcherButton);
            launcherButton = null;
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 820f;
            _windowStyle.fixedHeight = 400f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _hasInitStyles = true;
        }

    }
}

// Generic remarks
// PostProcess result.TimeFactor/deltaTime seems to be the req res ratio ex 0.5 for 15/30 machinery. iow: result.TimeFactor = req_res_ratio * deltaTime
// geo bonus = sum(researches) -> sqrt -> divide by settings EfficiencyMultiplier (10000) -> add settings starting (1)

// to give +10% geo: need 1 000 000 research
// to give +20% geo: need 4 000 000 research
// to give +50% geo: need 25 000 000 research
// to give +100% geo: need 100 000 000 research
// to give +200% geo: need 400 000 000 research
// to give +500% geo: need 2 500 000 000 research

// test giving 50% geo, 20% bota, 10% kolo 
// 2metal bays, no eff: 337.50% load , MKS bonus 2.25   . kerb 1.25 * 2bays  * 60%mach => yes
// with smelter or crusher 384.39 (each says 225% load) . MKS bonus 2.562613 = geo*geo*(1+sum(eff emul)/sum(conv emul) = 2.25*(1+(x / 13.144)) .. x = 13.144 *( 2.562613/2.25 - 1) = 1,826215676 = 2.25 * 0.83
//    crusher mks bonus 2.25
// with smelter+crusher 431.28
//
// after fix
// 384.38 load with both
// 337.49 with just crusher
//  with just smelter
// with just smelter and another converter at 48/2000mach 7.22% load : 360.93% load, MKs bonus 2.406302 =? 2.25*(1+ ( 2.25*0.83 / 2*13.144 )

// actually
// skill RepBoost boosts kolonization research
// skill FundsBoost boosts geology research
// skill ScienceBoost boosts science research

