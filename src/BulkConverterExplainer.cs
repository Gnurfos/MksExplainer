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
    public class BulkConverterExplainer : BaseExplainer
    {

        public static void DisplayConverterModule(ModuleBulkConverter converter, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels)
        {
            if (!converter.IsActivated)
            {
                PrintLine(20, converter.ConverterName + ": Not activated");
                return;
            }
            PrintLine(20, converter.ConverterName + ": Activated");

            var tot = 1d;
            var totFactorsExplanation = new List<string>();

            if (converter.UseSpecialistBonus)
            {
                SpecialistBonusExplanation specBonus = new SpecialistBonusExplanation(
                                                           converter.SpecialistBonusBase,
                                                           converter.SpecialistEfficiencyFactor,
                                                           converter.ExperienceEffect,
                                                           bestCrewSkillLevels);
                PrintLine(40, "Specialist bonus", String.Format("{0:0.##}", specBonus.GetValue()), specBonus.Explain());
                tot *= specBonus.GetValue();
                totFactorsExplanation.Add("spec_bonus");
            }

            AddRequiredResourcesFactors(converter.reqList, part, ref tot, totFactorsExplanation);

            AddMksModuleFactors(converter, vessel, part, bestCrewSkillLevels, ref tot, totFactorsExplanation);

            var totExplanation = String.Join(" * ", totFactorsExplanation.ToArray());
            PrintLine(40, " -> Total load", String.Format("{0:0.##%}", tot), totExplanation);

            Dictionary<string, double> resourceYields = GetResourceYields(vessel.mainBody.flightGlobalsIndex, converter.Yield, converter.MinAbundance, converter.inputList);
            List<ResourceRatio> actualOutputs = GetOutputs(resourceYields, converter.outputList);
            PrintResourceRates(60, tot, converter.inputList, actualOutputs);
        }

        private static Dictionary<string, double> GetResourceYields(int bodyId, float converterYield, float converterMin, List<ResourceRatio> inputs)
        {
            Dictionary<string, double> resourceYields = new Dictionary<string, double>();
            foreach (var res in global::ResourceCache.Instance.AbundanceCache)
            {
                if (res.BodyId != bodyId)
                    continue;
                if (inputs.Any(rr => rr.ResourceName == res.ResourceName))
                    continue;
                if (!resourceYields.ContainsKey(res.ResourceName))
                    resourceYields.Add(res.ResourceName, 0d);
                resourceYields[res.ResourceName] += res.Abundance;
            }
            var sum = resourceYields.Sum(r => r.Value);
            foreach (var resource in resourceYields.Keys.ToArray())
            {
                if (resourceYields[resource] < converterMin || resourceYields[resource] < double.Epsilon)
                    resourceYields.Remove(resource);
                else
                    resourceYields[resource] = converterYield * resourceYields[resource] / sum;
            }
            return resourceYields;
        }

        private static List<ResourceRatio> GetOutputs(Dictionary<string, double> resourceYields, List<ResourceRatio> converterStaticOutput)
        {
            List<ResourceRatio> res = new List<ResourceRatio>(converterStaticOutput);
            foreach (var ry in resourceYields)
            {
                res.Add(new ResourceRatio { FlowMode = ResourceFlowMode.ALL_VESSEL, Ratio = ry.Value, ResourceName = ry.Key, DumpExcess = true });
            }
            return res;
        }

    }
}

