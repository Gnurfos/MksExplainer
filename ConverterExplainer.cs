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
            PrintLine(20, converter.ConverterName + ": Activated");

            // For debug
            //Line("EfficiencyMultiplier", harvester.GetEfficiencyMultiplier().ToString()); // thermal eff * eff due to specialist
            //Line("res status", harvester.ResourceStatus.ToString()); x/sec displayed by KSP. trash
            //Line("eff bonus", harvester.GetEfficiencyBonus().ToString()); // same a SwapBay ?

            SpecialistBonusExplanation specBonus = converter.UseSpecialistBonus ? new SpecialistBonusExplanation(
                converter.SpecialistBonusBase,
                converter.SpecialistEfficiencyFactor,
                converter.ExperienceEffect,
                bestCrewSkillLevels) : null;



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
            }

            // MKSModule == efficiency parts benefiter
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
            if (part.FindModuleImplementing<MKSModule>() != null)
            {
                var mksModule = part.FindModuleImplementing<MKSModule>();
                PrintLine(50, "eTag", mksModule.eTag);
                PrintLine(50, "eMultiplier", mksModule.eMultiplier.ToString()); // 13.441
                PrintLine(50, "BonusEffect", mksModule.BonusEffect);
                // ModuleEfficiencyPart == efficiency part (provider)
                foreach (var epm in vessel.FindPartModulesImplementing<ModuleEfficiencyPart>())
                {
                    var ep = epm.part;
                    if (epm.eTag == mksModule.eTag)
                    {
                        PrintLine(50, "Efficiency part on vessel: " + ep.name);
                        PrintLine(80, epm.ConverterName + " activated: " + epm.IsActivated);
                        PrintLine(80, "eMultiplier", epm.eMultiplier.ToString()); // 0.83
                        PrintLine(80, "EfficiencyBonus", epm.EfficiencyBonus.ToString()); // 1 if part if configured as smelter else 0 (=numbays)
                        PrintLine(80, "EfficiencyMultiplier (=gov * _curMult)", epm.EfficiencyMultiplier.ToString()); // 0 when inactive . 0.5 when active  and 15/30 machinery. display is "50% load". 1/100% when full
                        PrintLine(100, "Governor", epm.Governor.ToString());
                        PrintResourceList("reqList", epm.reqList);
                        PrintLine(80, "Resources");
                        foreach (var res in ep.Resources)
                        {
                            PrintLine(100, String.Format("{0}: {1}/{2}", res.resourceName, res.amount, res.maxAmount));
                        }
                    }
                }
                // TODO dont display efficiency modules  as converters

                // Generic remarks
                // PostProcess result.TimeFactor/deltaTime seems to be the req res ratio ex 0.5 for 15/30 machinery. iow: result.TimeFactor = req_res_ratio * deltaTime
                // geo bonus = sum(researches) -> sqrt -> divide by settings EfficiencyMultiplier (10000) -> add settings starting (1)

                // to give +10% geo: need 1 000 000 research
                // to give +20% geo: need 4 000 000 research
                // to give +50% geo: need 25 000 000 research
                // to give +100% geo: need 100 000 000 research
                // to give +200% geo: need 400 000 000 research
                // to give +500% geo: need 2 500 000 000 research



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
            }


            /*ExplainHarvester(
                ResourceCache.GetAbundance(harvester.ResourceName, vessel),
                numBays,
                harvester.GetCoreTemperature(),
                harvester.ThermalEfficiency.maxTime,
                harvester.ThermalEfficiency.Evaluate((float)harvester.GetCoreTemperature()),
                harvester.Efficiency,
                specBonus);*/
        }

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

