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
    public class ConverterExplainer
    {
        private static GUIStyle _labelStyle = new GUIStyle(HighLogic.Skin.label);

        private static bool kEffPartsUseMksBonus = false; // Depends on MKS version

        public static void DisplayConverterModule(ModuleResourceConverter_USI converter, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels)
        {
            var numBays = converter.BonusList["SwapBay"];
            if (numBays < float.Epsilon)
                return;

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
            PrintLine(20, converter.ConverterName + ": Activated");

            var tot = 1d;

            if (converter.UseSpecialistBonus)
            {
                SpecialistBonusExplanation specBonus = new SpecialistBonusExplanation(
                                                           converter.SpecialistBonusBase,
                                                           converter.SpecialistEfficiencyFactor,
                                                           converter.ExperienceEffect,
                                                           bestCrewSkillLevels);
                PrintLine(40, "Specialist bonus", String.Format("{0:0.##}", converter.GetCrewBonus()), specBonus.Explain());
                tot *= converter.GetCrewBonus();
            }
            if (numBays > 1 + float.Epsilon)
            {
                PrintLine(40, "Bays", String.Format("{0}", numBays));
                tot *= numBays;
            }

            if (converter.reqList != null)
            {
                foreach (var res in converter.reqList)
                {
                    var amountInPart = part.Resources[res.ResourceName].amount;
                    var bonus = amountInPart / res.Ratio;
                    PrintLine(40, res.ResourceName, String.Format("{0:0.##}", bonus), String.Format("{0:0.##}/{1:0.##}", amountInPart, res.Ratio));
                    tot *= bonus;
                }
            }

            if (part.FindModuleImplementing<MKSModule>() != null)
            {
                // MKSModule == efficiency parts benefiter + kolonization bonuses benefiter
                var mksModule = part.FindModuleImplementing<MKSModule>();
                var geoBonus = KolonizationManager.GetGeologyResearchBonus(vessel.mainBody.flightGlobalsIndex);
                PrintLine(40, "Geology bonus", String.Format("{0:0.##}", geoBonus));
                tot *= geoBonus * geoBonus;
                if (mksModule.eTag != "")
                {
                    var effPartsBonus = ExplainEffPartsBonus(converter, mksModule.eTag, vessel, part, bestCrewSkillLevels, geoBonus);
                    tot *= effPartsBonus;
                }
            }
            PrintLine(40, " -> Total load", String.Format("{0:0.##}", tot));
            PrintResourceRates(60, tot, converter);
        }


        private static double ExplainEffPartsBonus(ModuleResourceConverter_USI converter, string converterETag, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels, float geoBonus)
        {    

            // MKSModule == efficiency parts benefiter + kolonization bonuses benefiter
            // refreshed every 5 seconds
            // set the MKS bonus (of all IEfficiencyBonusConsumer modules) to : GetEfficiencyBonus()
            //           GetEfficiencyBonus() = totBonus = geoBonus * GetPlanetaryBonus() * GetEfficiencyRate()
            //      GetPlanetaryBonus is either geo (again!) or bota or kolo depending on BonusEffect (seems to always be FundsBoost)
            //      GetEfficiencyRate is 
            /* curConverters
             *    all vessels in 500 range who have a MKSModule with same eTag
             *    sum of all MKSModule's (with same eTag) eMultiplier
             * _colonyConverterEff 
             *    records the best curConverters ever seen
             * curEParts
             *    all vessels in 500 range who have a ModuleEfficiencyPart with same eTag
             *    sum of all ModuleEfficiencyPart's (even with different eTag, even inactive?) eMultiplier * EfficiencyMultiplier(=load)
             * _effPartTotal
             *    records the best curEParts seen (although 1 "best seen" will update the 2)
             * ret 1 + _effPartTotal / _colonyConverterEff
             * */

            PrintLine(40, "Efficiency parts");


            // TOFIX kolony instead of vessel
            List<double> effPartsContributions = new List<double>();

            foreach (var epm in vessel.FindPartModulesImplementing<ModuleEfficiencyPart>())
            {
                var ep = epm.part;
                if (epm.eTag == converterETag)
                {
                    if (epm.EfficiencyBonus < float.Epsilon)
                    {
                        continue;
                    }
                    if (!epm.IsActivated)
                    {
                        continue;
                    }
                    PrintLine(60, String.Format("Active {0} in {1}", epm.ConverterName, ep.name));
                    var totEff = 1d;
                    PrintLine(80, "Governor", String.Format("{0:0.##}", epm.Governor));
                    totEff *= epm.Governor;
                    if (epm.UseSpecialistBonus)
                    {
                        SpecialistBonusExplanation specBonus = new SpecialistBonusExplanation(
                                                                   epm.SpecialistBonusBase,
                                                                   epm.SpecialistEfficiencyFactor,
                                                                   epm.ExperienceEffect,
                                                                   bestCrewSkillLevels);
                        PrintLine(80, "Crew bonus", String.Format("{0:0.##}", epm.GetCrewBonus()), specBonus.Explain());
                        totEff *= epm.GetCrewBonus();
                    }
                    if (epm.reqList != null)
                    {
                        foreach (var res in epm.reqList)
                        {
                            var amountInPart = ep.Resources[res.ResourceName].amount;
                            var bonus = amountInPart / res.Ratio;
                            PrintLine(80, res.ResourceName, String.Format("{0:0.##}", bonus), String.Format("{0:0.##}/{1:0.##}", amountInPart, res.Ratio));
                            totEff *= bonus;
                        }
                    }
                    if (kEffPartsUseMksBonus)
                    {
                        PrintLine(80, "Geology bonus", String.Format("{0:0.##}", geoBonus));
                        totEff *= geoBonus * geoBonus;
                    }
                    PrintLine(80, "eMultiplier", String.Format("{0}", epm.eMultiplier)); // 0.83
                    PrintLine(80, " -> Total contribution", String.Format("{0:0.##}", epm.eMultiplier * totEff));
                    effPartsContributions.Add(epm.eMultiplier * totEff);
                }
            }
            var numerator = effPartsContributions.Sum();
            var numeratorStr = PrintSum(effPartsContributions);
            if (numerator < float.Epsilon)
            {
                PrintLine(60, "No active efficiency parts");
                return 1d;
            }

            // TOFIX kolony instead of vessel
            List<double> convertersContribution = new List<double>();
            foreach (var convMks in vessel.FindPartModulesImplementing<MKSModule>())
            {
                var convs = convMks.part.FindModulesImplementing<BaseConverter>();
                foreach (var conv in convs)
                {
                    if (!conv.IsActivated)
                        continue;
                    if (convMks.eTag == converterETag)
                    {
                        PrintLine(60, String.Format("User of [{0}] {1}", converterETag, convMks.name));
                        PrintLine(80, "eMultiplier", String.Format("{0}", convMks.eMultiplier)); // 13.144
                        convertersContribution.Add(convMks.eMultiplier);
                    }
                }
            }
            var denominator = convertersContribution.Sum();
            var denominatorStr = PrintSum(convertersContribution);
            if (denominator < float.Epsilon)
            {
                PrintLine(60, "ERROR: no active converter found");
                return 1d;
            }
            var effPartsBonus = 1d + (numerator / denominator);
            PrintLine(40, "Efficiency parts bonus", String.Format("{0:0.##}", effPartsBonus), String.Format("1 + {0} / {1}", numeratorStr, denominatorStr));
            return effPartsBonus;
        
        }

        private static string PrintSum(List<double> values)
        {
            var sum = String.Join(" + ", values.Select(x => String.Format("{0:0.##}", x)).ToArray());
            if (values.Count > 1)
                return "(" + sum + ")";
            else
                return sum;
        }

        private static void PrintResourceList(string label, List<ResourceRatio> list)
        {
            PrintLine(50, label);
            foreach (var rr in list)
            {
                PrintLine(100, String.Format("{0}: {1}/s", rr.ResourceName, rr.Ratio));
            }
        }

        private static void PrintResourceRates(int margin, double load, ModuleResourceConverter_USI converter)
        {
            foreach (var rr in converter.inputList)
            {
                PrintLine(margin, rr.ResourceName, String.Format("-{0:0.####}/s", rr.Ratio * load));
            }
            foreach (var rr in converter.outputList)
            {
                PrintLine(margin, rr.ResourceName, String.Format("+{0:0.####}/s", rr.Ratio * load));
            }
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
            GUILayout.Label(value, _labelStyle, GUILayout.Width(100));
            GUILayout.Label(explanation, _labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        // " -> \"Thermal Efficiency\" (from" = 200 width

    }
}

