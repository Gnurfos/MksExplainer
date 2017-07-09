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
using System.Text.RegularExpressions;

namespace Explainer
{
    class Misc
    {
        // To stop wondering whether v.name or v.GetName() is the correct one
        public static string Name(Vessel vessel)
        {
            return vessel.GetName();
        }

        // To fix root part having the vessel name appended for some reason
        private static Regex _nameDropper = new Regex(" \\(.*\\)$");
        public static string Name(Part part)
        {
            return _nameDropper.Replace(part.name, "");
        }

        public static bool kDrillsUseMksBonuses = true; // Depends on MKS version. Was false in 0.50.14. True in 0.50.17+

    }

    public class BestCrewSkillLevels
    {
        public class BestSkillLevel
        {
            public float level;
            public HashSet<string> kerbals;
            public BestSkillLevel(float l, HashSet<string> ks) { level = l; kerbals = ks; }
        }
        public Dictionary<string, BestSkillLevel> skillLevelNames = new Dictionary<string, BestSkillLevel>();
        public void updateWith(string skill, string kerbal, float level)
        {
            if (!skillLevelNames.ContainsKey(skill) || (skillLevelNames[skill].level < level - float.Epsilon))
            {
                skillLevelNames[skill] = new BestSkillLevel(level, new HashSet<string> { FirstName(kerbal) });
            }
            else if (skillLevelNames.ContainsKey(skill) && (Math.Abs(skillLevelNames[skill].level - level) < float.Epsilon))
            {
                skillLevelNames[skill].kerbals.Add(FirstName(kerbal));
            }

        }
        private static string FirstName(string fullName)
        {
            return fullName.Replace(" Kerman", "");
        }
    }

    public class SpecialistBonusExplanation
    {
        private float SpecialistBonusBase;
        private float SpecialistEfficiencyFactor;
        private string Effect;
        private float BestLevel = 0;
        private string BestKerbal = "";
        public SpecialistBonusExplanation(float bb, float ef, string e, BestCrewSkillLevels bestLevels)
        {
            SpecialistBonusBase = bb;
            SpecialistEfficiencyFactor = ef;
            Effect = e;
            if (bestLevels.skillLevelNames.ContainsKey(e))
            {
                BestLevel = bestLevels.skillLevelNames[e].level;
                BestKerbal = bestLevels.skillLevelNames[e].kerbals.ElementAt(0);
            }
        }
        public string Explain()
        {
            return String.Format("{0} : {1}+{2}/lvl = {3} ({4})", Effect, SpecialistBonusBase, SpecialistEfficiencyFactor, GetValue(), GetBestKerbalDescription());
        }
        public float GetValue()
        {
            var result = SpecialistBonusBase;
            if (BestLevel > float.Epsilon)
            {
                result = SpecialistBonusBase + SpecialistEfficiencyFactor * (BestLevel + 1);
            }
            return result;
        }
        private string GetBestKerbalDescription()
        {
            if (BestLevel > float.Epsilon)
            {
                return String.Format("best={0}, lvl{1}", BestKerbal, BestLevel);
            }
            else
            {
                return "no specialist with skill on board";
            }
        }
    }

}
