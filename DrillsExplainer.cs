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

namespace Explainer
{
 
    public class DrillsExplainer
    {

        private static GUIStyle _labelStyle = new GUIStyle(HighLogic.Skin.label);

        public static void DisplayHarvesterModule(ModuleResourceHarvester_USI harvester, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels)
        {
            var numBays = harvester.BonusList["SwapBay"];
            if (numBays < float.Epsilon)
                return;

            if (!harvester.IsActivated)
            {
                PrintLine(20, harvester.ResourceName + ": Not activated");
                return;
            }
            PrintLine(20, harvester.ResourceName + ": Activated");

            // For debug
            //Line("EfficiencyMultiplier", harvester.GetEfficiencyMultiplier().ToString()); // thermal eff * eff due to specialist
            //Line("res status", harvester.ResourceStatus.ToString()); x/sec displayed by KSP. trash
            //Line("eff bonus", harvester.GetEfficiencyBonus().ToString()); // same a SwapBay ?

            SpecialistBonusExplanation specBonus = harvester.UseSpecialistBonus ? new SpecialistBonusExplanation(
                harvester.SpecialistBonusBase,
                harvester.SpecialistEfficiencyFactor,
                harvester.ExperienceEffect,
                bestCrewSkillLevels) : null;

            ExplainHarvester(
                ResourceCache.GetAbundance(harvester.ResourceName, vessel),
                numBays,
                harvester.GetCoreTemperature(),
                harvester.ThermalEfficiency.maxTime,
                harvester.ThermalEfficiency.Evaluate((float)harvester.GetCoreTemperature()),
                harvester.Efficiency,
                specBonus,
                vessel);
        }

        private static void ExplainHarvester(
            float locationResourceAbundance,
            float numBays,
            double partTemperature,
            float maxTemp,
            float thermalEfficiency,
            float extractionAbundanceMultiplier,
            SpecialistBonusExplanation specialistBonus,
            Vessel vessel)
        {
            PrintLine(50, "Resource abundance at location", String.Format("{0}", locationResourceAbundance));
            PrintLine(50, "Harvester abundance multiplier", String.Format("\"{0}% base efficiency\"", extractionAbundanceMultiplier * 100));
            PrintLine(50, " -> Rate", String.Format("\"{0}/s\"", extractionAbundanceMultiplier * locationResourceAbundance));
            PrintLine(50, "\"Core Temperature\"", String.Format("{0:0.00}", partTemperature));
            // PrintLine(50, "Max temperature", String.Format("{0:0.00}", maxTemp));
            PrintLine(50, " -> \"Thermal Efficiency\"", String.Format("{0}%", 100 * thermalEfficiency),  "(from some curves)");
            PrintLine(50, "Separators", numBays.ToString(), "(Drillheads)");
            float load = thermalEfficiency * numBays;
            var explanation = "ThermalEfficiency * Drillheads";
            if (specialistBonus != null)
            {
                PrintLine(50, "Specialist bonus", String.Format("{0:0.##}", specialistBonus.GetValue()), specialistBonus.Explain());
                load *= specialistBonus.GetValue();
                explanation += " * SpecialistBonus";
            }
            if (Misc.kDrillsUseMksBonuses)
            {
                var geoBonus = KolonizationManager.GetGeologyResearchBonus(vessel.mainBody.flightGlobalsIndex);
                PrintLine(80, "Geology bonus", String.Format("{0:0.##}", geoBonus));
                load *= geoBonus * geoBonus;
                explanation += " * geoBonus * geoBonus";
            }
            PrintLine(50, " -> \"load\"",  String.Format("{0:0.##}%", load * 100), explanation);
            PrintLine(50, " -> Actual obtention rate", String.Format("{0}/s", load * extractionAbundanceMultiplier * locationResourceAbundance), "Rate * load");
        }

        private static void PrintLine(int margin, string content)
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(content, _labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        private static void PrintLine(int margin, string title, string value, string explanation="")
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(title, _labelStyle, GUILayout.Width(200));
            GUILayout.Label(value, _labelStyle, GUILayout.Width(150));
            GUILayout.Label(explanation, _labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }


    }
}
