using System;
using System.Collections.Generic;
using UnityEngine;


namespace Explainer
{
    public class GuiTools
    {

        protected static GUIStyle _labelStyle = new GUIStyle(HighLogic.Skin.label);

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
            if (rate > 1000)
                return String.Format("{0}", (int)rate);
            if (rate > 1)
                return String.Format("{0:0.#}", rate);
            else if (rate > 0.001)
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
                PrintSingleResourceRate(margin, rr.ResourceName, "-", rr.Ratio * load);
            }
            foreach (var rr in outputList)
            {
                PrintSingleResourceRate(margin, rr.ResourceName, "+", rr.Ratio * load);
            }
        }

        protected static void PrintSingleResourceRate(int margin, string name, string sign, double rate)
        {
            GUILayout.BeginHorizontal();
            if (margin != 0)
                GUILayout.Label("", _labelStyle, GUILayout.Width(margin));
            GUILayout.Label(name, _labelStyle, GUILayout.Width(200));
            PrintRateLabel(sign, rate, "s");
            PrintRateLabel(sign, rate * 3600, "h");
            PrintRateLabel(sign, rate * 3600 * HoursPerDay(), "d");
            PrintRateLabel(sign, rate * 3600 * HoursPerDay() * DaysPerYear(), "y");
            GUILayout.EndHorizontal();
        }
        private static void PrintRateLabel(string sign, double rate, string timeScale)
        {
            GUILayout.Label(String.Format("{0}{1}/{2}", sign, FormatResourceRate(rate), timeScale), _labelStyle, GUILayout.Width(100));
        }
        private static int HoursPerDay()
        {
            return GameSettings.KERBIN_TIME ? 6 : 24;
        }
        private static int DaysPerYear()
        {
            return GameSettings.KERBIN_TIME ? 425 : 365;
        }
    }
}
