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
                specBonus);
        }

        private static void ExplainHarvester(
            float locationResourceAbundance,
            float numBays,
            double partTemperature,
            float maxTemp,
            float thermalEfficiency,
            float extractionAbundanceMultiplier,
            SpecialistBonusExplanation specialistBonus)
        {
            PrintLine(0, "");
            PrintLine(50, "Resource abundance at location", locationResourceAbundance.ToString());
            PrintLine(50, "Harvester bundance multiplier", String.Format("\"{0}% base efficiency\"", extractionAbundanceMultiplier * 100));
            PrintLine(50, "Rate", String.Format("\"{0}/s\"", extractionAbundanceMultiplier * locationResourceAbundance));
            PrintLine(0, "");
            PrintLine(50, "\"Core Temperature\"", String.Format("{0:0.00}", partTemperature));
                      PrintLine(50, "Max temperature", String.Format("{0:0.00}", maxTemp));
            PrintLine(50, "\"Thermal Efficiency\" (from some curves)", String.Format("{0}%", 100 * thermalEfficiency));
            PrintLine(0, "");
            PrintLine(50, "Bays", numBays.ToString());
            PrintLine(0, "");
            float load;
            if (specialistBonus != null)
            {
                PrintLine(50, "Specialist bonus", specialistBonus.Explain());
                PrintLine(0, "");
                load = thermalEfficiency * specialistBonus.GetValue() * numBays;
                PrintLine(50, "\"load\" = ThermalEfficiency * SpecialistBonus * NumBays", String.Format("{0}%", load * 100));
            }
            else
            {
                load = thermalEfficiency * numBays;
                PrintLine(50, "\"load\" = ThermalEfficiency * NumBays", String.Format("{0}%", load * 100));
            }
            PrintLine(0, "");
            PrintLine(50, "Actual obtention rate = Rate * load", String.Format("{0}/s", load * extractionAbundanceMultiplier * locationResourceAbundance));

            PrintLine(50, "----------------", "");
        }

        private static void PrintLine(int margin, string content, params string[] more)
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(content, _labelStyle, GUILayout.Width(400));
            foreach (var item in more)
                GUILayout.Label(item, _labelStyle, GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }


    }
}
