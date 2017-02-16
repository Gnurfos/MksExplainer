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

        private static void Line(string a, string b)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", _labelStyle, GUILayout.Width(50));
            GUILayout.Label(a, _labelStyle, GUILayout.Width(255));
            GUILayout.Label(b, _labelStyle, GUILayout.Width(255));
            GUILayout.EndHorizontal();
        }

    }
}
