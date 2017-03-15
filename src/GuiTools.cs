using System;
using System.Collections.Generic;
using UnityEngine;


namespace Explainer
{
    public class GuiTools
    {

        private static GUIStyle _labelStyle = new GUIStyle(HighLogic.Skin.label);

        public static void PrintLine(int margin, string content)
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(content, _labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        public static void PrintLine(int margin, string title, string value, string explanation="")
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(title, _labelStyle, GUILayout.Width(200));
            GUILayout.Label(value, _labelStyle, GUILayout.Width(100));
            GUILayout.Label(explanation, _labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        protected static string FormatResourceRate(double rate)
        {
            if (rate < double.Epsilon)
                return "0";
            if (rate > 0.001)
                return String.Format("{0:0.####}", rate);
            else if (rate > 0.000001)
                return String.Format("{0:0.#######}", rate);
            else if (rate > 0.000000001)
                return String.Format("{0:0.##########}", rate);
            else
                return String.Format("{0}", rate);
        }

        protected static void PrintResourceRates(int margin, double load, ModuleResourceConverter converter)
        {
            PrintResourceRates(margin, load, converter.inputList, converter.outputList);
        }
        protected static void PrintResourceRates(int margin, double load, IEnumerable<ResourceRatio> inputList, IEnumerable<ResourceRatio> outputList)
        {
            foreach (var rr in inputList)
            {
                PrintLine(margin, rr.ResourceName, String.Format("-{0}/s", FormatResourceRate(rr.Ratio * load)));
            }
            foreach (var rr in outputList)
            {
                PrintLine(margin, rr.ResourceName, String.Format("+{0}/s", FormatResourceRate(rr.Ratio * load)));
            }
        }

    }
}
