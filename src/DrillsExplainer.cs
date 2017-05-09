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
 
    public class DrillsExplainer : BaseExplainer
    {

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
                harvester,
                part,
                harvester.ResourceName,
                ResourceCache.GetAbundance(harvester.ResourceName, vessel),
                numBays,
                harvester.GetCoreTemperature(),
                harvester.ThermalEfficiency.maxTime,
                harvester.ThermalEfficiency.Evaluate((float)harvester.GetCoreTemperature()),
                harvester.Efficiency,
                specBonus,
                vessel,
                bestCrewSkillLevels);
        }

        private static void ExplainHarvester(
            ModuleResourceHarvester_USI harvester,
            Part part,
            string resourceName,
            float locationResourceAbundance,
            float numBays,
            double partTemperature,
            float maxTemp,
            float thermalEfficiency,
            float extractionAbundanceMultiplier,
            SpecialistBonusExplanation specialistBonus,
            Vessel vessel,
            BestCrewSkillLevels bestCrewSkillLevels)
        {
            var tot = 1d;
            var totFactorsExplanation = new List<string>();


            PrintLine(50, "Resource abundance at location", String.Format("{0}", locationResourceAbundance));
            PrintLine(50, "Harvester abundance multiplier", String.Format("\"{0}% base efficiency\"", extractionAbundanceMultiplier * 100));
            PrintLine(50, " -> Rate", String.Format("\"{0}/s\"", extractionAbundanceMultiplier * locationResourceAbundance));

            PrintLine(50, "\"Core Temperature\"", String.Format("{0:0.00}", partTemperature));
            // PrintLine(50, "Max temperature", String.Format("{0:0.00}", maxTemp));
            PrintLine(50, " -> \"Thermal Efficiency\"", String.Format("{0}%", 100 * thermalEfficiency),  "(from some curves)");
            tot *= thermalEfficiency;
            totFactorsExplanation.Add("ThermalEfficiency");

            PrintLine(50, "Separators", numBays.ToString(), "(Drillheads)");
            tot *= numBays;
            totFactorsExplanation.Add("Drillheads");

            if (specialistBonus != null)
            {
                PrintLine(50, "Specialist bonus", String.Format("{0:0.##}", specialistBonus.GetValue()), specialistBonus.Explain());
                tot *= specialistBonus.GetValue();
                totFactorsExplanation.Add("SpecialistBonus");
            }

            AddMksModuleFactors(harvester, vessel, part, bestCrewSkillLevels, ref tot, totFactorsExplanation);

            var totExplanation = String.Join(" * ", totFactorsExplanation.ToArray());
            PrintLine(50, " -> Total load", String.Format("{0:0.##%}", tot), totExplanation);

            PrintLine(50, " -> Actual obtention rate", String.Format("+{0}/s", FormatResourceRate(tot * extractionAbundanceMultiplier * locationResourceAbundance)), "Rate * load");
            PrintSingleResourceRate(60, resourceName, "+", tot * extractionAbundanceMultiplier * locationResourceAbundance);
        }

    }
}
