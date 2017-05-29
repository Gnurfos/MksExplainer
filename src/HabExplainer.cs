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
using KolonyTools;
using LifeSupport;

namespace Explainer
{
    public class HabExplainer : BaseExplainer
    {

        private static List<System.Object> _expandedSections = new List<System.Object>();
        private static bool IsExpanded(System.Object p)
        {
            return _expandedSections.Any(x => System.Object.ReferenceEquals(x, p));
        }
        private static void Expand(System.Object p)
        {
            _expandedSections.Add(p);
        }
        private static void Collapse(System.Object p)
        {
            _expandedSections.Remove(p);
        }
        private static void Toggle(System.Object p)
        {
            if (IsExpanded(p))
                Collapse(p);
            else
                Expand(p);
        }
        private static System.Object _baseSection = new Int64();

        public static void Display(Vessel vessel)
        {
            var HAB_RANGE = (float) LifeSupportScenario.Instance.settings.GetSettings().HabRange;
            var vessels = GetKolonyVessels(vessel, HAB_RANGE, true, false).OrderBy(v => v.thisVessel);
            var kolonyCrew = vessels.Sum(v => v.vessel.GetCrewCount());
            var kolonyCrewCapacity = vessels.Sum(v => v.vessel.GetCrewCapacity());
            PrintLine(40, "kolonyCrew", String.Format("{0}/{1}", kolonyCrew, kolonyCrewCapacity));

            double partsHabTime = 0d;
            double partsMultiplierBoost = 0d;
            foreach (var v in vessels)
            {
                var vesselLabel = v.thisVessel ? v.name : String.Format("{0} ({1}m away)", v.name, (int) v.distance);
                PrintLine(40, "In " + vesselLabel);
                if (v.vessel.GetCrewCapacity() == 0)
                {
                    PrintLine(50, "No crew capacity, not contributing to hab");
                    continue;
                }
                PrintLine(50, String.Format("Crew: {0}/{1}", v.vessel.GetCrewCount(), v.vessel.GetCrewCapacity()));
                foreach (var hab in v.vessel.FindPartModulesImplementing<ModuleHabitation>())
                {
                    if (hab.BonusList.ContainsKey("SwapBay") && (hab.BonusList["SwapBay"] < float.Epsilon))
                    {
                        continue; // Not configured
                    }
                    double partHabTime;
                    double partMultiplierBoost;
                    ExplainHabModule(hab, kolonyCrew, out partHabTime, out partMultiplierBoost);
                    partsHabTime += partHabTime;
                    partsMultiplierBoost += partMultiplierBoost;
                }
            }

            var buttonLabel = IsExpanded(_baseSection) ? "<<" : ">>";
            GUILayout.BeginHorizontal();
            GUILayout.Label("", _labelStyle, GUILayout.Width(60));
            GUILayout.Label("Base (non parts)", _labelStyle);
            if (GUILayout.Button(buttonLabel, GUILayout.ExpandWidth(false)))
            {
                Toggle(_baseSection);
            }
            GUILayout.EndHorizontal();
            var settingsBaseHabTime = LifeSupportScenario.Instance.settings.GetSettings().BaseHabTime;
            var capacityHabTime = settingsBaseHabTime * kolonyCrewCapacity;
            var bonusMultiplierBoost = USI_GlobalBonuses.Instance.GetHabBonus(vessel.mainBody.flightGlobalsIndex);
            if (IsExpanded(_baseSection))
            {
                PrintLine(80, "settingsBaseHabTime", String.Format("{0:0.##}", settingsBaseHabTime), "from settings");
                PrintLine(80, "capacityHabTime", String.Format("{0:0.##}", capacityHabTime), "settingsBaseHabTime * kolonyCrewCapacity");
                PrintLine(80, "bonus", String.Format("{0:0.###}", bonusMultiplierBoost), "kolonization research bonus progress");
            }
            PrintLine(70, "-->", String.Format("+{0:0.##}K-month", capacityHabTime), String.Format("x{0:0.###}", bonusMultiplierBoost));

            var multiplierBoost = partsMultiplierBoost + bonusMultiplierBoost;
            PrintLine(40, "multiplierBoost", String.Format("x{0:0.##}", multiplierBoost), "sum of(parts + base)");
            var multiplier = 1 + multiplierBoost;
            PrintLine(40, "multiplier", String.Format("x{0:0.##}", multiplier), "1 + multiplierBoost");
            var rawHabTime = partsHabTime + capacityHabTime;
            PrintLine(40, "rawHabTime", String.Format("{0:0.##}", rawHabTime), "sum of(parts + base)");

            var settingsMultiplier = LifeSupportScenario.Instance.settings.GetSettings().HabMultiplier;
            var settingsMultiplierAddendum = "";
            if (settingsMultiplier != 1)
            {
                PrintLine(40, "settingsMultiplier", String.Format("{0:0.##}", settingsMultiplier), "from settings");
                settingsMultiplierAddendum = " * settingsMultiplier";
            }

            var result = (rawHabTime / kolonyCrew) * multiplier * settingsMultiplier;
            var formattedResult = LifeSupportUtilities.DurationDisplay(result * LifeSupportUtilities.SecondsPerMonth(), LifeSupportUtilities.TimeFormatLength.Short);
            PrintLine(40, " -> Vessel hab time", String.Format("{0:0.##} months = {1}", result, formattedResult), "(rawHabTime / kolonyCrew) * multiplier" + settingsMultiplierAddendum);
        }

        static void ExplainHabModule(ModuleHabitation hab, int kolonyCrew, out double partHabTime, out double partMultiplierBoost)
        {
            double load = hab.HabAdjustment;
            var crewRatio = Math.Min(1, hab.CrewCapacity / kolonyCrew);
            var contributionToMultiplierBoost = hab.HabMultiplier * crewRatio;
            partHabTime = hab.KerbalMonths;
            partMultiplierBoost = contributionToMultiplierBoost;
            var buttonLabel = IsExpanded(hab) ? "<<" : ">>";
            GUILayout.BeginHorizontal();
            GUILayout.Label("", _labelStyle, GUILayout.Width(60));
            GUILayout.Label(Misc.Name(hab.part), _labelStyle);
            if (GUILayout.Button(buttonLabel, GUILayout.ExpandWidth(false)))
            {
                Toggle(hab);
            }
            GUILayout.EndHorizontal();
            if (IsExpanded(hab))
            {
                if (load < 1d - double.Epsilon)
                {
                    PrintLine(80, "Base", String.Format("+{0:0.#}K-month", hab.BaseKerbalMonths), String.Format("x{0:0.##}", hab.BaseHabMultiplier));
                    PrintLine(80, String.Format("Converter running at {0:0.##%} load", load));
                    PrintLine(80, "Effective", String.Format("+{0:0.#}K-month", hab.KerbalMonths), String.Format("x{0:0.##}", hab.HabMultiplier));
                }
                else
                {
                    PrintLine(80, "Base", String.Format("+{0:0.#}K-month", hab.BaseKerbalMonths), String.Format("x{0:0.##}", hab.BaseHabMultiplier));
                }
                PrintLine(80, "Crew capacity", String.Format("{0}", hab.CrewCapacity));
                PrintLine(80, "Crew ratio", String.Format("{0:0.##}", crewRatio), "Min(1, partCrewCapacity / kolonyCrew)");
                PrintLine(80, "Multiplier contribution", String.Format("x{0:0.##}", contributionToMultiplierBoost), "Multiplier * Crew Ratio");
            }
            PrintLine(70, "-->", String.Format("+{0:0.#}K-month", hab.KerbalMonths), String.Format("x{0:0.##}", contributionToMultiplierBoost));
        }

    }
}

