using System.Collections.Generic;
using USITools;
using KolonyTools;
using System;
using System.Linq;
using LifeSupport;

namespace Explainer {
    
    public class BaseExplainer : GuiTools
    {

        private const float EFF_RANGE = 500f;

        protected static void AddMksModuleFactors(PartModule module, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels, ref double tot, List<string> totFactorsExplanation)
        {
            if (!BenefitsFromMksModuleBonuses(module))
                return;
            if (part.FindModuleImplementing<MKSModule>() != null)
            {
                // MKSModule == efficiency parts benefiter + kolonization bonuses benefiter
                // 1. Kolonization bonuses
                var mksModule = part.FindModuleImplementing<MKSModule>();
                var geoBonus = KolonizationManager.GetGeologyResearchBonus(vessel.mainBody.flightGlobalsIndex);
                var koloBonus = KolonizationManager.GetKolonizationResearchBonus(vessel.mainBody.flightGlobalsIndex);
                var botaBonus = KolonizationManager.GetBotanyResearchBonus(vessel.mainBody.flightGlobalsIndex);
                PrintLine(40, "Geology bonus", String.Format("{0:0.##}", geoBonus));
                tot *= geoBonus;
                totFactorsExplanation.Add("geo_bonus");

                if (mksModule.BonusEffect == "RepBoost")
                {
                    PrintLine(40, "Kolonization bonus", String.Format("{0:0.##}", koloBonus));
                    tot *= koloBonus;
                    totFactorsExplanation.Add("kolo_bonus");
                }
                else if (mksModule.BonusEffect == "ScienceBoost")
                {
                    PrintLine(40, "Botany bonus", String.Format("{0:0.##}", botaBonus));
                    tot *= botaBonus;
                    totFactorsExplanation.Add("bota_bonus");
                }
                else
                {
                    tot *= geoBonus;
                    totFactorsExplanation.Add("geo_bonus");
                }
                // 2. Efficiency parts
                if (mksModule.eTag != "")
                {
                    var effPartsBonus = ExplainEffPartsBonus(mksModule.eTag, vessel, part, bestCrewSkillLevels, geoBonus);
                    tot *= effPartsBonus;
                    if (Math.Abs(effPartsBonus - 1d) > double.Epsilon)
                        totFactorsExplanation.Add("eff_parts_bonus");
                }
                if (typeof(MKSModule).GetField("Governor") != null)
                {
                    var gov = (float) typeof(MKSModule).GetField("Governor").GetValue(mksModule);
                    PrintLine(40, "Governor", String.Format("{0:0.##}", gov));
                    tot *= gov;
                    totFactorsExplanation.Add("governor");
                }
            }
        }

        protected static void AddRequiredResourcesFactors(List<ResourceRatio> reqList, Part part, ref double tot, List<string> totFactorsExplanation)
        {
            if (reqList != null)
            {
                foreach (var res in reqList)
                {
                    var amountInPart = part.Resources[res.ResourceName].amount;
                    var bonus = amountInPart / res.Ratio;
                    PrintLine(40, res.ResourceName, String.Format("{0:0.##}", bonus), String.Format("{0:0.##}/{1:0.##}", amountInPart, res.Ratio));
                    tot *= bonus;
                }
                totFactorsExplanation.Add("req_resource");
            }
        }

        private static double ExplainEffPartsBonus(string converterETag, Vessel vessel, Part part, BestCrewSkillLevels bestCrewSkillLevels, float geoBonus)
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

            List<double> effPartsContributions = new List<double>();

            foreach (var effPartVessel in GetKolonyVessels(vessel, EFF_RANGE))
                foreach (var epm in effPartVessel.vessel.FindPartModulesImplementing<ModuleEfficiencyPart>())
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
                        effPartsContributions.Add(GetEffPartContribution(ep, epm, bestCrewSkillLevels, geoBonus, effPartVessel));
                    }
                }
            var numerator = effPartsContributions.Sum();
            var numeratorStr = PrintSum(effPartsContributions);
            if (numerator < float.Epsilon)
            {
                PrintLine(60, "No active efficiency parts");
                return 1d;
            }

            List<double> convertersContribution = new List<double>();
            foreach (var effPartVessel in GetKolonyVessels(vessel, EFF_RANGE))
                foreach (var convMks in effPartVessel.vessel.FindPartModulesImplementing<MKSModule>())
                {
                    var convs = convMks.part.FindModulesImplementing<BaseConverter>();
                    foreach (var conv in convs)
                    {
                        if (!conv.IsActivated)
                            continue;
                        if (convMks.eTag == converterETag)
                        {
                            PrintLine(60, String.Format("User of [{0}] {1}/{2}{3}", converterETag, Misc.Name(convMks.part), conv.ConverterName, effPartVessel.ExplainOther()));
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

        private static double GetEffPartContribution(Part ep, ModuleEfficiencyPart epm, BestCrewSkillLevels bestCrewSkillLevels, float geoBonus, KolonyVessel effPartVessel)
        {
            var otherVesselExplanation = effPartVessel.ExplainOther();
            PrintLine(60, String.Format("Active {0} in {1}{2}", epm.ConverterName, Misc.Name(ep), otherVesselExplanation));
            var totEff = 1d;
            if (typeof(ModuleEfficiencyPart).GetField("Governor") != null)
            {
                var gov = (float) typeof(ModuleEfficiencyPart).GetField("Governor").GetValue(epm);
                PrintLine(80, "Governor", String.Format("{0:0.##}", gov));
                totEff *= gov;
            }
            if (epm.UseSpecialistBonus)
            {
                SpecialistBonusExplanation specBonus = new SpecialistBonusExplanation(
                    epm.SpecialistBonusBase,
                    epm.SpecialistEfficiencyFactor,
                    epm.ExperienceEffect,
                    bestCrewSkillLevels);
                PrintLine(80, "Crew bonus", String.Format("{0:0.##}", specBonus.GetValue()), specBonus.Explain());
                totEff *= specBonus.GetValue();
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
            if (Misc.kEffPartsUseMksBonus)
            {
                PrintLine(80, "Geology bonus", String.Format("{0:0.##}", geoBonus));
                totEff *= geoBonus * geoBonus;
            }
            PrintLine(80, "eMultiplier", String.Format("{0}", epm.eMultiplier)); // 0.83
            PrintLine(80, " -> Total contribution", String.Format("{0:0.##}", epm.eMultiplier * totEff));
            return epm.eMultiplier * totEff;
        }

        protected struct KolonyVessel
        {
            public string name;
            public Vessel vessel;
            public bool thisVessel;
            public double distance;
            public string ExplainOther()
            {
                if (thisVessel) return "";
                else return String.Format(" (in {0}, {1}m away)", name, (int) distance);
            }
        }

        protected static List<KolonyVessel> GetKolonyVessels(Vessel thisVessel, float range, bool includeThis=true, bool landedOnly=true)
        {
            List<KolonyVessel> res = new List<KolonyVessel>();
            foreach (var v in LogisticsTools.GetNearbyVessels(range, includeThis, thisVessel, landedOnly))
            {
                KolonyVessel item = new KolonyVessel();
                item.name = Misc.Name(v);
                item.vessel = v;
                if (v == thisVessel)
                {
                    item.thisVessel = true;
                }
                else
                {
                    item.thisVessel = false;
                    item.distance = LogisticsTools.GetRange(v, thisVessel);
                }

                res.Add(item);
            }
            return res;
        }

        private static string PrintSum(List<double> values)
        {
            var sum = String.Join(" + ", values.Select(x => String.Format("{0:0.##}", x)).ToArray());
            if (values.Count > 1)
                return "(" + sum + ")";
            else
                return sum;
        }

        private static bool BenefitsFromMksModuleBonuses(PartModule module)
        {
            if (!typeof(IEfficiencyBonusConsumer).IsInstanceOfType(module))
                return false;
            if (typeof(ModuleEfficiencyPart).IsInstanceOfType(module))
                return false;
            return true;
        }

    }
}