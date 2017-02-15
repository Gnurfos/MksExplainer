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
     * TOFIX:
     *  - cache resource abundance
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

        private class BestCrewSkillLevels
        {
            public class BestSkillLevel
            {
                public float level;
                public HashSet<string> kerbals;
                public BestSkillLevel(float l, HashSet<string> ks) { level = l; kerbals = ks; }
            }
            public Dictionary<string, BestSkillLevel> skillLevelNames = new Dictionary<string, BestSkillLevel>();
            public void updateWith(string skill, string kerbal, float level)
            {
                if (!skillLevelNames.ContainsKey(skill) || (skillLevelNames[skill].level < level - float.Epsilon))
                {
                    skillLevelNames[skill] = new BestSkillLevel(level, new HashSet<string> { FirstName(kerbal) });
                }
                else if (skillLevelNames.ContainsKey(skill) && (Math.Abs(skillLevelNames[skill].level - level) < float.Epsilon))
                {
                    skillLevelNames[skill].kerbals.Add(FirstName(kerbal));
                }

            }
            private static string FirstName(string fullName)
            {
                return fullName.Replace(" Kerman", "");
            }
        }

        private void Display()
        {
            var vessel = FlightGlobals.ActiveVessel;
            foreach (var p in vessel.parts)
            {
                if (p.FindModuleImplementing<ModuleResourceHarvester_USI>())
                {
                    DisplayHeader("Harvester: " + p.name);
                    foreach (var m in p.FindModulesImplementing<ModuleResourceHarvester_USI>())
                    {
                        DisplayHarvesterModule(m, vessel, p);
                    }
                }
            }

            /*
            DisplayHeader("Best skills: ");
            foreach (var item in GetBestCrewSkillLevels(vessel).skillLevelNames)
            {
                var skill = item.Key;
                var best = item.Value;
                var knames = string.Join(", ", best.kerbals.ToArray());
                Line(skill, "Level " + best.level.ToString() + " (" + knames + ")");
            }*/
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

        private void DisplayHarvesterModule(ModuleResourceHarvester_USI harvester, Vessel vessel, Part part)
        {
            var numBays = harvester.BonusList["SwapBay"];
            if (numBays < float.Epsilon)
                return;

            if (!harvester.IsActivated)
            {
                Line(harvester.ResourceName, "Not activated");
                return;
            }
            Line(harvester.ResourceName, "Activated");

            // For debug
            //Line("EfficiencyMultiplier", harvester.GetEfficiencyMultiplier().ToString()); // thermal eff * eff due to specialist
            //Line("res status", harvester.ResourceStatus.ToString()); x/sec displayed by KSP. trash
            //Line("eff bonus", harvester.GetEfficiencyBonus().ToString()); // same a SwapBay ?

            SpecialistBonusExplanation specBonus = harvester.UseSpecialistBonus ? new SpecialistBonusExplanation(
                harvester.SpecialistBonusBase,
                harvester.SpecialistEfficiencyFactor,
                harvester.ExperienceEffect,
                GetBestCrewSkillLevels(vessel)) : null;

            ExplainHarvester(
                ResourceCache.GetAbundance(harvester.ResourceName, vessel),
                numBays,
                harvester.GetCoreTemperature(),
                harvester.ThermalEfficiency.maxTime,
                harvester.ThermalEfficiency.Evaluate((float)harvester.GetCoreTemperature()),
                harvester.Efficiency,
                specBonus);

            /*
            Line("Planetary abundance", GetAbundance(harvester.ResourceName, vessel).ToString());

            Line("Bays", numBays.ToString());

            Line("Temperature", part.temperature.ToString());
            Line("ThermalEfficiency", harvester.ThermalEfficiency.Evaluate(part.temperature).ToString());

            Line("Efficiency (abundance multiplier)", harvester.Efficiency.ToString());


            Line("UseSpecialistBonus", harvester.UseSpecialistBonus.ToString());
            Line("SpecialistBonusBase", harvester.SpecialistBonusBase.ToString());
            Line("ExperienceEffect", harvester.ExperienceEffect);*/

        }

        private class SpecialistBonusExplanation
        {
            private float SpecialistBonusBase;
            private float SpecialistEfficiencyFactor;
            private string Effect;
            private float BestLevel = 0;
            private string BestKerbal = "";
            public SpecialistBonusExplanation(float bb, float ef, string e, BestCrewSkillLevels bestLevels)
            {
                SpecialistBonusBase = bb;
                SpecialistEfficiencyFactor = ef;
                Effect = e;
                if (bestLevels.skillLevelNames.ContainsKey(e))
                {
                    BestLevel = bestLevels.skillLevelNames[e].level;
                    BestKerbal = bestLevels.skillLevelNames[e].kerbals.ElementAt(0);
                }
            }
            public string Explain()
            {
                return String.Format("Skill: {0} : {1}+{2}/lvl = {3} ({4})", Effect, SpecialistBonusBase, SpecialistEfficiencyFactor, GetValue(), GetBestKerbalDescription());
            }
            public float GetValue()
            {
                var result = SpecialistBonusBase;
                if (BestLevel > float.Epsilon)
                {
                    result = SpecialistBonusBase + SpecialistEfficiencyFactor * (BestLevel + 1);
                }
                return result;
            }
            private string GetBestKerbalDescription()
            {
                if (BestLevel > float.Epsilon)
                {
                    return String.Format("best={0}, lvl{1}", BestKerbal, BestLevel);
                }
                else
                {
                    return "no specialist with skill on board";
                }
            }
        }

        private void ExplainHarvester(
            float locationResourceAbundance,
            float numBays,
            double partTemperature,
            float maxTemp,
            float thermalEfficiency,
            float extractionAbundanceMultiplier,
            SpecialistBonusExplanation specialistBonus)
        {
            Line("", "");
            Line("Resource abundance at location", locationResourceAbundance.ToString());
            Line("Harvester bundance multiplier", String.Format("\"{0}% base efficiency\"", extractionAbundanceMultiplier * 100));
            Line("Rate", String.Format("\"{0}/s\"", extractionAbundanceMultiplier * locationResourceAbundance));
            Line("", "");
            Line("\"Core Temperature\"", partTemperature.ToString());
            Line("Max temperature", maxTemp.ToString());
            Line("\"Thermal Efficiency\" (from some curves)", String.Format("{0}%", 100 * thermalEfficiency));
            Line("", "");
            Line("Bays", numBays.ToString());
            Line("", "");
            float load;
            if (specialistBonus != null)
            {
                Line("Specialist bonus", specialistBonus.Explain());
                Line("", "");
                load = thermalEfficiency * specialistBonus.GetValue() * numBays;
                Line("\"load\" = ThermalEfficiency * SpecialistBonus * NumBays", String.Format("{0}%", load * 100));
            }
            else
            {
                load = thermalEfficiency * numBays;
                Line("\"load\" = ThermalEfficiency * NumBays", String.Format("{0}%", load * 100));
            }
            Line("", "");
            Line("Actual obtention rate = Rate * load", String.Format("{0}/s", load * extractionAbundanceMultiplier * locationResourceAbundance));

            Line("----------------", "");
        }


        private void DisplayHeader(string h)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(h, _labelStyle, GUILayout.Width(200));
            GUILayout.Label("", _labelStyle, GUILayout.Width(255));
            GUILayout.Label("", _labelStyle, GUILayout.Width(255));
            GUILayout.EndHorizontal();
        }

        private void Line(string a, string b)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", _labelStyle, GUILayout.Width(200));
            GUILayout.Label(a, _labelStyle, GUILayout.Width(255));
            GUILayout.Label(b, _labelStyle, GUILayout.Width(255));
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
