using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens;

namespace SimpleSurvival
{
    // KSP doesn't support Tuples, soooo...
    public class TwoLabels
    {
        public DialogGUILabel shipLS = new DialogGUILabel("AA", true, true);
        public DialogGUILabel evaLS = new DialogGUILabel("BB", true, true);
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LifeSupportGUI : MonoBehaviour
    {
        private static bool showgui = false;
        private static ApplicationLauncherButton toolbarButton = null;
        private static PopupDialog gui = null;
        private static Vector2 position = new Vector2(0.5f, 0.5f);
        private static Vector2 size = new Vector2(500, 200);
        private static Dictionary<string, TwoLabels> labelMap
            = new Dictionary<string, TwoLabels>();

        // Temp for debugging
        private static DialogGUILabel debugLabel = null;

        public void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(AddToolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveToolbar);
        }

        public void AddToolbar()
        {
            if (toolbarButton == null)
            {
                Texture2D icon = GameDatabase.Instance.databaseTexture.Find(
                    a => a.name.EndsWith("/RD_node_icon_simplesurvivalbasic")).texture;
                toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                    ButtonOnTrue, ButtonOnFalse,
                    null, null, null, null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    icon);
            }
            toolbarButton.SetFalse();
            showgui = false;
        }

        public void RemoveToolbar()
        {
            if (toolbarButton != null)
            {
                ApplicationLauncher.Instance.RemoveApplication(toolbarButton);
                toolbarButton = null;
            }
            showgui = false;
        }

        private void ButtonOnTrue()
        {
            Util.PostUpperMessage("CALL: ontrue");
            showgui = true;

            // Store labels in separate data structure for easy access when updating
            labelMap.Clear();
            List<DialogGUILabel> entries = new List<DialogGUILabel>();
            var kerbals = FlightGlobals.ActiveVessel.GetVesselCrew();
            kerbals.Sort(CompareCrewNames);

            foreach (var kerbal in kerbals)
            {
                TwoLabels labels = new TwoLabels();
                labelMap.Add(kerbal.name, labels);
                entries.Add(new DialogGUILabel(kerbal.name, true, true));
                entries.Add(labels.shipLS);
                entries.Add(labels.evaLS);
            }

            gui = PopupDialog.SpawnPopupDialog(
                    new MultiOptionDialog(
                    "lifesupport_readout",
                    "",
                    "LifeSupport Readout",
                    UISkinManager.defaultSkin,
                    new Rect(position, size),
                        new DialogGUIContentSizer(UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize,
                        UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize, true),
                        new DialogGUIScrollList(Vector2.one, false, true,
                            new DialogGUIGridLayout(new RectOffset(),
                                new Vector2(100,20),
                                Vector2.zero,
                                UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                                UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                                TextAnchor.MiddleLeft,
                                UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 3,
                                entries.ToArray()))),
                false,
                UISkinManager.defaultSkin,
                false,
                "ExtraTitle");
            // position = new Vector2(position.x, position.y);

            /*if (fdebugLabel == null)
            {
                debugLabel = new DialogGUILabel("");
                PopupDialog.SpawnPopupDialog(
                    new MultiOptionDialog(
                        "Hello", "Hi", "Title",
                        UISkinManager.defaultSkin,
                        debugLabel),
                    false,
                    UISkinManager.defaultSkin,
                    false);
            }*/
        }

        private void ButtonOnFalse()
        {
            Util.PostUpperMessage("CALL: onfalse");
            position = gui.GetComponent<RectTransform>().position;
            position.x = position.x / Screen.width + 0.5f;
            position.y = position.y / Screen.height + 0.5f;
            showgui = false;
            gui.Dismiss();
        }

        private void UpdateGUI()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            var parts = vessel.FindPartModulesImplementing<LifeSupportModule>();
            List<string> deadKerbals = new List<string>();

            foreach (var module in parts)
            {
                Part part = module.part;
                List<ProtoCrewMember> crew = part.protoModuleCrew;
                crew.Sort(CompareCrewNames);

                double perhead = part.Resources[C.NAME_LIFESUPPORT].amount / crew.Count;
                string timestr = DaysToString(perhead / C.LS_PER_DAY_PER_KERBAL);

                foreach (string kerbal in labelMap.Keys)
                {
                    if (!crew.Exists(a => a.name == kerbal))
                    {
                        labelMap[kerbal].shipLS.SetOptionText("DEAD");
                        labelMap[kerbal].evaLS.SetOptionText("DEAD");
                        deadKerbals.Add(kerbal);
                        continue;
                    }

                    double evaLS = EVALifeSupportTracker.GetEVALSInfo(kerbal).ls_current;
                    string evastr = DaysToString(evaLS / C.EVA_LS_DRAIN_PER_DAY);
                    labelMap[kerbal].shipLS.SetOptionText(timestr);
                    labelMap[kerbal].evaLS.SetOptionText(evastr);
                }
            }

            foreach (string kerbal in deadKerbals)
                labelMap.Remove(kerbal);
        }

        public void OnGUI()
        {
            //debugLabel.SetOptionText($"x: {position.x}\ny: {position.y}\nUI_SCALE: {GameSettings.UI_SCALE}");
            if (showgui)
                UpdateGUI();
        }

        private static int CompareCrewNames(ProtoCrewMember c1, ProtoCrewMember c2)
        {
            return c1.name.CompareTo(c2.name);
        }

        /// <summary>
        /// Converts Kerbal days (6 hrs) to string.
        /// </summary>
        /// <returns></returns>
        private string DaysToString(double value)
        {
            int days = (int)value;
            // Convert to hours
            value = (value - days) * 6.0;
            int hours = ((int)value);
            // Convert to minutes
            value = (value - hours) * 60.0;
            int mins = (int)value;
            // Convert to seconds
            value = (value - mins) * 60.0;
            int secs = (int)value;

            return $"{days}d, {PadInt(hours)}:{PadInt(mins)}:{PadInt(secs)}";
        }

        /// <summary>
        /// Pad single-digit numbers with a leading zero.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string PadInt(int value)
        {
            return value.ToString().PadLeft(2, '0');
        }
    }
}
