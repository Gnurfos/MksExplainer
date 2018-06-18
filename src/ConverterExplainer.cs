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
    public class ConverterExplainer : BaseExplainer
    {
        public static void DisplayConverterModule(ModuleResourceConverter_USI converter, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels)
        {
            //var numBays = converter.BonusList["SwapBay"];
            //if (numBays < float.Epsilon)
            //    return;

            if (!converter.IsActivated)
            {
                PrintLine(20, converter.ConverterName + ": Not activated");
                return;
            }
            if (typeof(ModuleEfficiencyPart).IsInstanceOfType(converter))
            {
                PrintLine(20, converter.ConverterName + ": Active effiency part (not a real converter)");
                return;
            }
            if (typeof(ModuleHabitation).IsInstanceOfType(converter))
            {
                PrintLine(20, converter.ConverterName + ": Active hab part (not a real converter)");
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
            //if (numBays > 1 + float.Epsilon)
            //{
            //    PrintLine(40, "Bays", String.Format("{0}", numBays));
            //    tot *= numBays;
            //    totFactorsExplanation.Add("num_bays");
            //}
                
            AddRequiredResourcesFactors(converter.reqList, part, ref tot, totFactorsExplanation);

            AddMksModuleFactors(converter, vessel, part, bestCrewSkillLevels, ref tot, totFactorsExplanation);

            var totExplanation = String.Join(" * ", totFactorsExplanation.ToArray());
            PrintLine(40, " -> Total load", String.Format("{0:0.##%}", tot), totExplanation);
            PrintResourceRates(60, tot, converter);
        }

    }
}

