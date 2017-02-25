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
                PrintLine(20, converter.ConverterName + ": Effiency part (not a real converter)");
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
                PrintLine(40, String.Format("Crew bonus {0:0.##} ({1})", converter.GetCrewBonus(), specBonus.Explain()));
                tot *= converter.GetCrewBonus();
            }
            if (numBays > 1 + float.Epsilon)
            {
                PrintLine(40, String.Format("Bays: {0}", numBays));
                tot *= numBays;
            }

            if (converter.reqList != null)
            {
                foreach (var res in converter.reqList)
                {
                    var amountInPart = part.Resources[res.ResourceName].amount;
                    var bonus = amountInPart / res.Ratio;
                    PrintLine(40, String.Format("{0} {1:0.##} ({2:0.##}/{3:0.##})", res.ResourceName, bonus, amountInPart, res.Ratio));
                    tot *= bonus;
                }
            }

            if (part.FindModuleImplementing<MKSModule>() != null)
            {
                // MKSModule == efficiency parts benefiter + kolonization bonuses benefiter
                var mksModule = part.FindModuleImplementing<MKSModule>();
                var geoBonus = KolonizationManager.GetGeologyResearchBonus(vessel.mainBody.flightGlobalsIndex);
                PrintLine(60, String.Format("Geology bonus {0:0.##}", geoBonus));
                double mksBonus = geoBonus * geoBonus;
                if (mksModule.eTag != "")
                {
                    var effPartsBonus = ExplainEffPartsBonus(converter, mksModule.eTag, vessel, part, bestCrewSkillLevels, geoBonus);
                    mksBonus *= effPartsBonus;
                }
                PrintLine(40, String.Format("MKS bonus (=geo*geo*effpartsbonus): {0:0.##})", mksBonus));
                tot *= mksBonus;
            }
            PrintLine(30, String.Format("Total load {0:0.##}", tot));
            // TODO print resources gain/loss rate

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
                    PrintLine(80, String.Format("Active {0} in {1}", epm.ConverterName, ep.name));
                    var totEff = 1d;
                    PrintLine(100, String.Format("Governor {0:0.##}", epm.Governor));
                    totEff *= epm.Governor;
                    if (epm.UseSpecialistBonus)
                    {
                        SpecialistBonusExplanation specBonus = new SpecialistBonusExplanation(
                                                                   epm.SpecialistBonusBase,
                                                                   epm.SpecialistEfficiencyFactor,
                                                                   epm.ExperienceEffect,
                                                                   bestCrewSkillLevels);
                        PrintLine(100, String.Format("Crew bonus {0:0.##} ({1})", epm.GetCrewBonus(), specBonus.Explain()));
                        totEff *= epm.GetCrewBonus();
                    }
                    if (epm.reqList != null)
                    {
                        foreach (var res in epm.reqList)
                        {
                            var amountInPart = ep.Resources[res.ResourceName].amount;
                            var bonus = amountInPart / res.Ratio;
                            PrintLine(100, String.Format("{0} {1:0.##} ({2:0.##}/{3:0.##})", res.ResourceName, bonus, amountInPart, res.Ratio));
                            totEff *= bonus;
                        }
                    }
                    if (kEffPartsUseMksBonus)
                    {
                        PrintLine(100, String.Format("Geology bonus {0:0.##}", geoBonus));
                        totEff *= geoBonus * geoBonus;
                        PrintLine(100, String.Format("Total efficiency = geo*geo*resource_ratio*gov*skill = {0:0.##}", totEff));
                    }
                    else
                    {
                        PrintLine(100, String.Format("Total efficiency = resource_ratio*gov*skill = {0:0.##}", totEff));
                    }
                    PrintLine(100, String.Format("eMultiplier {0}", epm.eMultiplier)); // 0.83
                    PrintLine(100, String.Format("Total contribution {0:0.##}", epm.eMultiplier * totEff));
                    effPartsContributions.Add(epm.eMultiplier * totEff);
                }
            }

            // TOFIX kolony instead of vessel
            List<float> convertersContribution = new List<float>();
            foreach (var convMks in vessel.FindPartModulesImplementing<MKSModule>())
            {
                var convs = convMks.part.FindModulesImplementing<BaseConverter>();
                foreach (var conv in convs)
                {
                    if (!conv.IsActivated)
                        continue;
                    if (convMks.eTag == converterETag)
                    {
                        PrintLine(80, String.Format("User of [{0}] {1}", converterETag, convMks.name));
                        PrintLine(100, String.Format("eMultiplier {0}", convMks.eMultiplier)); // 13.144
                        convertersContribution.Add(convMks.eMultiplier);
                    }
                }
            }

            var num = effPartsContributions.Sum();
            var numStr = String.Join(" + ", effPartsContributions.Select(x => String.Format("{0:0.##}", x)).ToArray());
            var den = convertersContribution.Sum();
            var denStr = String.Join(" + ", convertersContribution.Select(x => String.Format("{0:0.##}", x)).ToArray());
            if (num < float.Epsilon)
            {
                PrintLine(60, "No active efficiency parts");
                return 1d;
            }
            if (den < float.Epsilon)
            {
                PrintLine(60, "ERROR: no active converter found");
                return 1d;
            }
            var effPartsBonus = 1d + (num / den);
            PrintLine(60, String.Format("Efficiency parts bonus {0:0.##} = ({1}) / ({2})", effPartsBonus, numStr, denStr));
            return effPartsBonus;
        
        }

        // Generic remarks
        // PostProcess result.TimeFactor/deltaTime seems to be the req res ratio ex 0.5 for 15/30 machinery. iow: result.TimeFactor = req_res_ratio * deltaTime
        // geo bonus = sum(researches) -> sqrt -> divide by settings EfficiencyMultiplier (10000) -> add settings starting (1)

        // to give +10% geo: need 1 000 000 research
        // to give +20% geo: need 4 000 000 research
        // to give +50% geo: need 25 000 000 research
        // to give +100% geo: need 100 000 000 research
        // to give +200% geo: need 400 000 000 research
        // to give +500% geo: need 2 500 000 000 research

        // test giving 50% geo, 20% bota, 10% kolo 
        // 2metal bays, no eff: 337.50% load , MKS bonus 2.25   . kerb 1.25 * 2bays  * 60%mach => yes
        // with smelter or crusher 384.39 (each says 225% load) . MKS bonus 2.562613 = geo*geo*(1+sum(eff emul)/sum(conv emul) = 2.25*(1+(x / 13.144)) .. x = 13.144 *( 2.562613/2.25 - 1) = 1,826215676 = 2.25 * 0.83
        //    crusher mks bonus 2.25
        // with smelter+crusher 431.28
        //
        // after fix
        // 384.38 load with both
        // 337.49 with just crusher
        //  with just smelter
        // with just smelter and another converter at 48/2000mach 7.22% load : 360.93% load, MKs bonus 2.406302 =? 2.25*(1+ ( 2.25*0.83 / 2*13.144 )

        // actually
        // skill RepBoost boosts kolonization research
        // skill FundsBoost boosts geology research
        // skill ScienceBoost boosts science research

        private static void PrintResourceList(string label, List<ResourceRatio> list)
        {
            PrintLine(50, label);
            foreach (var rr in list)
            {
                PrintLine(100, String.Format("{0}: {1}/s", rr.ResourceName, rr.Ratio));
            }
        }

        private static void PrintLineM(int margin, string content, params string[] more)
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(content, _labelStyle, GUILayout.Width(400));
            foreach (var item in more)
                GUILayout.Label(item, _labelStyle, GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }

        private static void PrintLine(int margin, string content)
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(content, _labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

    }
}

