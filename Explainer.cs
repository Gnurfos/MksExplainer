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
     *  - term visible in game
     *  - due to part settings
     *  - due to crew
     *  - due to environment (planetary abundance, part temp ...)
     *  - due to resource storage
     * 
     * TODO:
     *  - converters explainer (with efficiency parts)
     *  - recyclers explainer
     *  - kolonization bonuses impact ?
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
            _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "MKS Explainer", _windowStyle);
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
            foreach (var p in vessel.parts)
            {
                if (p.FindModuleImplementing<ModuleResourceHarvester_USI>())
                {
                    PrintLine("Harvester: " + p.name);
                    foreach (var m in p.FindModulesImplementing<ModuleResourceHarvester_USI>())
                    {
                        DrillsExplainer.DisplayHarvesterModule(m, vessel, p, GetBestCrewSkillLevels(vessel));
                    }
                }

                if (p.FindModuleImplementing<ModuleResourceConverter_USI>())
                {
                    PrintLine("Converter: " + p.name);
                    foreach (var m in p.FindModulesImplementing<ModuleResourceConverter_USI>())
                    {
                        ConverterExplainer.DisplayConverterModule(m, vessel, p, GetBestCrewSkillLevels(vessel));
                    }
                }
            }

            PrintLine("Best skills: ");
            foreach (var item in GetBestCrewSkillLevels(vessel).skillLevelNames)
            {
                var skill = item.Key;
                var best = item.Value;
                var knames = string.Join(", ", best.kerbals.ToArray());
                PrintLine(skill + ": Level " + best.level.ToString() + " (" + knames + ")", 50);
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
