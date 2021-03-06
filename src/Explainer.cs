﻿using System;
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
using KolonyTools;

namespace Explainer
{
    /*
     * TODO:
     *  - recyclers explainer
     *  - sifter: explain abundance values (sum for biomes with min)
     */

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class ExplainerScenario : ScenarioModule
    {
        private static string CONFIG_NODE = "MKS_EXPLAINER";
        public float windowX = 300;
        public float windowY = 60;
        public float windowW = 820;
        public float windowH = 400;
        public static ExplainerScenario Instance { get; private set; }
        public ExplainerScenario()
        {
            Instance = this;
        }
        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            if (gameNode.HasNode(CONFIG_NODE))
            {
                var node = gameNode.GetNode(CONFIG_NODE);
                node.TryGetValue("GUI.x", ref windowX);
                node.TryGetValue("GUI.y", ref windowY);
                node.TryGetValue("GUI.w", ref windowW);
                node.TryGetValue("GUI.h", ref windowH);
            }
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);
            var node = gameNode.HasNode(CONFIG_NODE) ? gameNode.GetNode(CONFIG_NODE) : gameNode.AddNode(CONFIG_NODE);
            node.AddValue("GUI.x", windowX);
            node.AddValue("GUI.y", windowY);
            node.AddValue("GUI.w", windowW);
            node.AddValue("GUI.h", windowH);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class ExplainerGui : MonoBehaviour
    {
        private ApplicationLauncherButton launcherButton;
        private static Texture2D resizeTexture, closeTexture;
        private static GUIContent resizeContent;
        private Rect _resizePosition, _closePosition;
        private GUIStyle _buttonsStyle;
        private bool _resizePushed;

        private Rect _windowPosition = new Rect(ExplainerScenario.Instance.windowX, ExplainerScenario.Instance.windowY, ExplainerScenario.Instance.windowW, ExplainerScenario.Instance.windowH);

        private GUIStyle _labelStyle;
        private GUIStyle _scrollStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        public static bool display = false;


        private BestCrewSkillLevels bestCrewSkillLevels;
        private Part selectedPart;
        private bool selectedHab = false;

        private static ExplainerGui lastInstance;
        public ExplainerGui()
        {
            lastInstance = this;
        }

        void Awake()
        {
            var resizeIconFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resize.png");
            resizeTexture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
            resizeTexture.LoadImage(File.ReadAllBytes(resizeIconFile));
            resizeContent = new GUIContent(resizeTexture, "Resize");

            var closeIconFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Close.png");
            closeTexture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
            closeTexture.LoadImage(File.ReadAllBytes(closeIconFile));

            var launcherIconTexture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var launcherIconFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Icon.png");
            launcherIconTexture.LoadImage(File.ReadAllBytes(launcherIconFile));
            launcherButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT, launcherIconTexture);
        }

        public void Start()
        {
            DontDestroyOnLoad(this);
            if (!_hasInitStyles)
                InitStyles();
        }

        private void GuiOn()
        {
            ToggleDisplay();
        }

        private void GuiOff()
        {
            ToggleDisplay();
        }

        private void ToggleDisplay()
        {
            if (display)
            {
                display = false;
                bestCrewSkillLevels = null;
            }
            else
            {
                display = true;
            }
        }

        private void OnGUI()
        {
            if (!display)
                return;
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            Ondraw();
        }

        private void Ondraw()
        {
            _windowPosition = GUILayout.Window(42, _windowPosition, OnWindow, "MKS Explainer",
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinWidth(820), GUILayout.MinHeight(400));
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        private void GenerateWindow()
        {
            _closePosition = new Rect(_windowPosition.width - 21, 1, 16, 16);
            if (GUI.Button(_closePosition, closeTexture, _buttonsStyle))
            {
                ToggleDisplay();
            }
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle);
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
                DrawAndHandleResize();
                GUI.DragWindow();
                ExplainerScenario.Instance.windowX = _windowPosition.x;
                ExplainerScenario.Instance.windowY = _windowPosition.y;
                ExplainerScenario.Instance.windowW = _windowPosition.width;
                ExplainerScenario.Instance.windowH = _windowPosition.height;
            }
        }

        private void Display()
        {
            var vessel = FlightGlobals.ActiveVessel;
            DisplayHeader(vessel);
            if (selectedPart && selectedPart.vessel == vessel)
            {
                if (GUILayout.Button("Back"))
                {
                    selectedPart = null;
                }
                else
                {
                    DisplayPart(vessel, selectedPart);
                }
            }
            else if (selectedHab)
            {
                if (GUILayout.Button("Back"))
                {
                    selectedHab = false;
                }
                else
                {
                    DisplayHab(vessel);
                }
            }
            else
            {
                DisplayPartList(vessel);
                if (GUILayout.Button("Hab"))
                {
                    selectedHab = true;
                }
            }
        }

        private void DrawAndHandleResize()
        {
            GUILayout.Space(24);
            _resizePosition = new Rect(_windowPosition.width - 21, _windowPosition.height - 22, 16, 16);
            GUI.Label(_resizePosition, resizeContent, _buttonsStyle);
            if (Event.current == null || Event.current.type == EventType.Layout)
                return;
            if (!_resizePushed)
            {
                if (Event.current.type == EventType.MouseDown
                    && Event.current.button == 0
                    && _resizePosition.Contains(Event.current.mousePosition))
                {
                    _resizePushed = true;
                    Event.current.Use();
                }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    ResizeTo(Input.mousePosition);
                }
                else
                {
                    _resizePushed = false;
                }
            }
        }

        private void ResizeTo(Vector3 screenMousePos)
        {
            _windowPosition.width = Mathf.Clamp(screenMousePos.x - _windowPosition.x + _resizePosition.width / 2, 50, Screen.width - _windowPosition.x);
            _windowPosition.height = Mathf.Clamp(Screen.height - screenMousePos.y - _windowPosition.y + _resizePosition.height / 2, 50, Screen.height - _windowPosition.y);
        }

        private void DisplayHeader(Vessel vessel)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vessel: " + Misc.Name(vessel), _labelStyle, GUILayout.ExpandWidth(true));
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

        private struct HighlightedPart
        {
            public Part part;
            public bool wasActive;
            public bool wasRecursive;
            public Color previousColor;
        }
        private HighlightedPart highlightedPart = new HighlightedPart();
        private static Color highlightColor = new Color(0f, 0.8f, 0.6f);

        private void DisplayPartList(Vessel vessel)
        {
            var counter = new PartCounterByName();
            Part newHighligtedPart = null;
            foreach (var part in vessel.parts)
            {
                if (part.FindModuleImplementing<ModuleResourceHarvester_USI>()
                    || part.FindModuleImplementing<ModuleResourceConverter_USI>()
                    || part.FindModuleImplementing<ModuleBulkConverter>())
                {
                    var pName = Misc.Name(part);
                    var label = String.Format("{0} #{1}", pName, counter.next(pName));
                    if (GUILayout.Button(label))
                    {
                        selectedPart = part;
                    }
                    else
                    {
                        if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                        {
                            newHighligtedPart = part;
                        }
                    }
                }
            }
            highlightPartIfNeeded(newHighligtedPart);
        }

        private void highlightPartIfNeeded(Part newHighligtedPart)
        {
            if (highlightedPart.part != null && highlightedPart.part != newHighligtedPart)
            {
                // Restore previous part highlight
                highlightedPart.part.SetHighlightColor(highlightedPart.previousColor);
                highlightedPart.part.HighlightActive = highlightedPart.wasActive;
                highlightedPart.part.RecurseHighlight = highlightedPart.wasRecursive;
            }
            if (newHighligtedPart != highlightedPart.part)
            {
                // Highlight new part
                highlightedPart.part = newHighligtedPart;
                if (newHighligtedPart != null)
                {
                    highlightedPart.previousColor = newHighligtedPart.highlightColor;
                    highlightedPart.wasActive = newHighligtedPart.HighlightActive;
                    highlightedPart.wasRecursive = newHighligtedPart.RecurseHighlight;
                    newHighligtedPart.SetHighlightColor(highlightColor);
                    newHighligtedPart.SetHighlight(true, false);
                }
            }
        }

        public static void SelectPart(Part part)
        {
            if (lastInstance != null)
            {
                lastInstance.selectedPart = part;
                if (!display)
                    display = true;
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
                PrintLine("Converters: " + Misc.Name(part));
                foreach (var mod in part.FindModulesImplementing<ModuleResourceConverter_USI>())
                {
                    ConverterExplainer.DisplayConverterModule(mod, vessel, part, GetBestCrewSkillLevels(vessel));
                }
            }

            if (part.FindModuleImplementing<ModuleBulkConverter>())
            {
                PrintLine("Bulk Converters: " + Misc.Name(part));
                foreach (var mod in part.FindModulesImplementing<ModuleBulkConverter>())
                {
                    BulkConverterExplainer.DisplayConverterModule(mod, vessel, part, GetBestCrewSkillLevels(vessel));
                }
            }
        }

        private void DisplayHab(Vessel vessel)
        {
            HabExplainer.Display(vessel);
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
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _buttonsStyle = new GUIStyle(HighLogic.Skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 20,
                fixedHeight = 20,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset() { left = 3, right = 3, top = 3, bottom = 3 } 
            };
            _resizePosition = new Rect(_windowPosition.width - 21, _windowPosition.height - 22, 16, 16);
            _closePosition = new Rect(_windowPosition.width - 21, 1, 16, 16);
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

