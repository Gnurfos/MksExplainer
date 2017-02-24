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
                var geoBonus = KolonizationManager.GetGeologyResearchBonus(vessel.mainBody.flightGlobalsIndex);
                var effPartsBonus = ExplainEffPartsBonus(converter, vessel, part, bestCrewSkillLevels, geoBonus);
                PrintLine(60, String.Format("Geology bonus {0:0.##}", geoBonus));
                var mksBonus = geoBonus * geoBonus * effPartsBonus;
                PrintLine(40, String.Format("MKS bonus (=geo*geo*effpartsbonus): {0:0.##} (game reports {1})", mksBonus, converter.BonusList["MKS"]));
                tot *= mksBonus;
            }
            PrintLine(30, String.Format("Total load {0:0.##}", tot));
            PrintLine(40, "BonusList");
            foreach (var b in converter.BonusList) // SwapBay 2, MKS 
            {
                PrintLine(50, b.Key + ": " + b.Value.ToString());
            }


            // For debug
            //Line("EfficiencyMultiplier", harvester.GetEfficiencyMultiplier().ToString()); // thermal eff * eff due to specialist
            //Line("res status", harvester.ResourceStatus.ToString()); x/sec displayed by KSP. trash
            //Line("eff bonus", harvester.GetEfficiencyBonus().ToString()); // same a SwapBay ?


            /*


            PrintLine(50, "GetEfficiencyBonus", converter.GetEfficiencyBonus().ToString()); // 2 for 2 bays           . 2.123502 with 1 full smelter
            PrintLine(80, "BonusList");
            foreach (var b in converter.BonusList) // SwapBay 2, MKS 1.061751 =  1 + (0.83 / 13.441)
            {
                PrintLine(100, b.Key + ": " + b.Value.ToString());
            }
            PrintLine(50, "GetEfficiencyMultiplier", converter.GetEfficiencyMultiplier().ToString()); // 2.5 ?        . 2.6543781  with 1 full smelter
            PrintLine(50, "GetCrewBonus", converter.GetCrewBonus().ToString()); // 1.25 with max specialist
            PrintResourceList("reqList", converter.reqList);
            PrintResourceList("inputList", converter.inputList);
            PrintResourceList("outputList", converter.outputList);
            PrintLine(50, "Resources");
            foreach (var res in part.Resources)
            {
                PrintLine(100, String.Format("{0}: {1}/{2}", res.resourceName, res.amount, res.maxAmount));
            }*/

        }


        private static double ExplainEffPartsBonus(ModuleResourceConverter_USI converter, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels, float geoBonus)
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

            var mksModule = part.FindModuleImplementing<MKSModule>();


            //PrintLine(50, "eTag", mksModule.eTag);
            //PrintLine(50, "eMultiplier", mksModule.eMultiplier.ToString()); // 13.441
            //PrintLine(50, "BonusEffect", mksModule.BonusEffect);

            // ModuleEfficiencyPart == efficiency part (provider)

            // TOFIX kolony instead of vessel
            var effPartsContributions = 0d;
            foreach (var epm in vessel.FindPartModulesImplementing<ModuleEfficiencyPart>())
            {
                var ep = epm.part;
                //PrintLine(120, String.Format("ep {0}, epm.name {1}, epm.EfficiencyBonus {2}, epm.eTag {3}, epm.IsActivated {4}, epm.EfficiencyMultiplier {5}", ep.name, epm.ConverterName, epm.EfficiencyBonus, epm.eTag, epm.IsActivated, epm.EfficiencyMultiplier));
                if (epm.eTag == mksModule.eTag)
                {
                    //PrintLine(80, "EfficiencyBonus", epm.EfficiencyBonus.ToString()); // 1 if part if configured as smelter else 0 (=numbays)
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
                    PrintLine(100, String.Format("Geology bonus {0:0.##}", geoBonus));
                    totEff *= geoBonus * geoBonus;
                    PrintLine(100, String.Format("Total efficiency = geo*geo*resource_ratio*gov*skill= {0:0.##} (game reports {1:0.##})", totEff, epm.EfficiencyMultiplier));
                    PrintLine(100, "eMultiplier", epm.eMultiplier.ToString()); // 0.83
                    PrintLine(100, String.Format("Total contribution {0:0.##}", epm.eMultiplier * totEff));
                    effPartsContributions += epm.eMultiplier * totEff;
                    /*
                    PrintLine(110, "epm BonusList");
                    foreach (var b in epm.BonusList) // 
                    {
                        PrintLine(120, b.Key + ": " + b.Value.ToString());
                    }*/
                }
            }

            // TOFIX kolony instead of vessel
            var convertersContribution = 0f;
            foreach (var convMks in vessel.FindPartModulesImplementing<MKSModule>())
            {
                var convs = convMks.part.FindModulesImplementing<BaseConverter>();
                foreach (var conv in convs)
                {
                    if (!conv.IsActivated)
                        continue;
                    // TODO fix in MKS : weigh by "EfficiencyMultiplier" ? (or just num bays)
                    if (convMks.eTag == mksModule.eTag)
                    {
                        PrintLine(80, String.Format("User of [{0}] {1}", mksModule.eTag, convMks.name));
                        PrintLine(100, "eMultiplier", convMks.eMultiplier.ToString()); // 13.144
                        convertersContribution += convMks.eMultiplier;
                    }
                }
            }

            var effPartsBonus = 1d + (effPartsContributions / convertersContribution);
            PrintLine(60, String.Format("Efficiency parts bonus {0:0.##}", effPartsBonus));

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


                // Metals 2 bays. mach 1200/2000
                // When smelter active 15/30 => 154.63%load 
                // (governor is for transition 0 to 1)
                // When inactive: 150% load
                // 1.5 + 0.0463

                // 0.0464 =? 

                // full mach 159.26%  1200 mach /2000 = 60%
                // 159 = 60% * GetEfficiencyMultiplier

                // 0.83 / 13.144 = 0.063146683

        private static void PrintResourceList(string label, List<ResourceRatio> list)
        {
            PrintLine(50, label);
            foreach (var rr in list)
            {
                PrintLine(100, String.Format("{0}: {1}/s", rr.ResourceName, rr.Ratio));
            }
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

