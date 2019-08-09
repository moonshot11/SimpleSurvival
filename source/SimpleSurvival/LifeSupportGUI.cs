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
    public class GUIElements
    {
        public DialogGUILabel shipLS = new DialogGUILabel("AA", true, true);
        public DialogGUILabel evaLS = new DialogGUILabel("BB", true, true);
        public DialogGUIButton fillEVAButton;

        // -- Debug values --
        public DialogGUILabel evaLS_Value = new DialogGUILabel("CC", true, true);
        public DialogGUILabel evaProp = new DialogGUILabel("DD", true, true);

        private ProtoCrewMember kerbal;
        /// <summary>
        /// Is this NOT EVA, and do we have a converter?
        /// </summary>
        private bool buttonEnable;

        public GUIElements(ProtoCrewMember kerbal, bool buttonEnable)
        {
            this.kerbal = kerbal;
            this.buttonEnable = buttonEnable;
            fillEVAButton = new DialogGUIButton<string>(
                "Fill EVA", LifeSupportGUI.PressFillEva, kerbal.name,
                EnabledCondition: EnableFillButton,
                dismissOnSelect: false);
        }

        /// <summary>
        /// Manages the true condition which enables the "Fill EVA" button.
        /// </summary>
        /// <returns></returns>
        private bool EnableFillButton()
        {
            var info = EVALifeSupportTracker.GetEVALSInfo(kerbal.name);
            return info.ls_current < info.ls_max &&
                !FlightGlobals.ActiveVessel.isEVA &&
                buttonEnable;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LifeSupportGUI : MonoBehaviour
    {

        private static bool showgui = false;

        // This is necessary to track when the GUI is refreshed 2+ times
        // without being drawn to the screen. Otherwise, after the second
        // cycle, a neutral position is assumed, and the saved position
        // is lost.
        /// <summary>
        /// Has the GUI been draw in the current cycle?
        /// </summary>
        private static bool drewgui = false;

        private static ApplicationLauncherButton toolbarButton = null;
        private static PopupDialog gui = null;
        private static Vector2 position = new Vector2(0.5f, 0.5f);
        private static Vector2 size = new Vector2(470, 200);
        private static Dictionary<string, GUIElements> labelMap
            = new Dictionary<string, GUIElements>();
        private static bool allowBadTransfer = false;

        private static DialogGUIToggle riskButton = new DialogGUIToggle(
            !allowBadTransfer,
            "Prevent unsafe crew transfer",
            RiskButtonSelected);

        private static DialogGUIHorizontalLayout riskLayout = new DialogGUIHorizontalLayout(
            false, false, 0f, new RectOffset(20, 0, 0, 0), TextAnchor.MiddleLeft, riskButton);

        private DialogGUILabel statusLabel = new DialogGUILabel("Status", true, true);
        private DialogGUILabel consLabel = new DialogGUILabel("Consumables: x/x", 200, 0);

        private int consID = PartResourceLibrary.Instance.GetDefinition(C.NAME_CONSUMABLES).id;
        private int lsID = PartResourceLibrary.Instance.GetDefinition(C.NAME_LIFESUPPORT).id;
        private int evapropID = PartResourceLibrary.Instance.GetDefinition(C.NAME_EVA_PROPELLANT).id;

        public void Awake()
        {
            Util.Log("LifeSupportGUI Awake");
            GameEvents.onGUIApplicationLauncherReady.Add(AddToolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveToolbar);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onCrewTransferred.Add(OnCrewTransferred);
            GameEvents.onGamePause.Add(OnGamePause);
        }

        public void OnDisable()
        {
            Util.Log("LifeSupportGUI OnDisable");
            GameEvents.onGUIApplicationLauncherReady.Remove(AddToolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveToolbar);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onCrewTransferred.Remove(OnCrewTransferred);
            GameEvents.onGamePause.Remove(OnGamePause);
            // Ensure it's removed from the MainMenu scene
            RemoveToolbar();
        }

        public void OnVesselChange(Vessel vessel)
        {
            RefreshGUI();
        }

        public void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> ev)
        {
            RefreshGUI();
        }

        public void OnGamePause()
        {
            if (!showgui)
                return;
            toolbarButton.SetFalse(makeCall: true);
        }

        /// <summary>
        /// Update GUI components that cannot be changed live,
        /// by "turning if off then on again."
        /// </summary>
        public void RefreshGUI()
        {
            if (!showgui)
                return;
            ButtonOnFalse();
            ButtonOnTrue();
        }

        public void AddToolbar()
        {
            // Not sure why this is necessary in addition to the visibleInScenes
            // arg below...
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            Util.Log("LifeSupportGUI AddToolbar: " + HighLogic.LoadedScene.Description());
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
            toolbarButton.SetFalse(false);
            showgui = false;
            drewgui = false;
        }

        public void RemoveToolbar()
        {
            Util.Log("LifeSupportGUI RemoveToolbar: " + HighLogic.LoadedScene.Description());
            if (toolbarButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
                toolbarButton = null;
            }
            showgui = false;
        }

        private static void RiskButtonSelected(bool arg)
        {
            Util.PostUpperMessage("Button: " + arg.ToString());
        }

        private void ButtonOnTrue()
        {
            Util.PostUpperMessage("CALL: ontrue");

            // Store labels in separate data structure for easy access when updating
            labelMap.Clear();
            List<LifeSupportReportable> modules =
                FlightGlobals.ActiveVessel.FindPartModulesImplementing<LifeSupportReportable>();
            // Calculate once on window generation, instead of each frame
            bool buttonEnable = !FlightGlobals.ActiveVessel.isEVA &&
                FlightGlobals.ActiveVessel.HasModule<Cons2LSModule>();
            // Cell padding
            RectOffset offset = new RectOffset(20, 0, 10, 0);
            int cellWidth = 100;
            DialogGUIVerticalLayout vert = new DialogGUIVerticalLayout(
                false, false, 1f,
                new RectOffset(), TextAnchor.UpperLeft);

            vert.AddChild(
                new DialogGUIGridLayout(new RectOffset(),
                    new Vector2(cellWidth, 20),
                    Vector2.zero,
                    UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                    UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                    TextAnchor.MiddleLeft,
                    UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 4,
                    new DialogGUILabel("<b>Kerbal</b>", true, true),
                    new DialogGUILabel("<b>Ship life support</b>", true, true),
                    new DialogGUILabel("<b>Suit life support</b>", true, true),
                    new DialogGUILabel("", true, true)
                    ));

            foreach (LifeSupportReportable module in modules)
            {
                if (module.part.protoModuleCrew.Count == 0)
                    continue;

                List<DialogGUIBase> kerbalCells = new List<DialogGUIBase>();
                List<ProtoCrewMember> crew = new List<ProtoCrewMember>(module.part.protoModuleCrew);
                crew.Sort(CompareCrewNames);
                vert.AddChild(new DialogGUILabel($"<color=#f4b00c><b>{module.part.partInfo.title}</b></color>"));

                foreach (ProtoCrewMember kerbal in crew)
                {
                    GUIElements elems = new GUIElements(kerbal, buttonEnable);
                    labelMap.Add(kerbal.name, elems);
                    kerbalCells.Add(new DialogGUILabel(kerbal.name, true, true));
                    kerbalCells.Add(elems.shipLS);
                    kerbalCells.Add(elems.evaLS);
                    kerbalCells.Add(elems.fillEVAButton);

                    // Add raw EVA tracking values
                    if (Config.DEBUG_SHOW_EVA)
                    {
                        DialogGUILabel emptyLabel = new DialogGUILabel("", true, true);
                        kerbalCells.Add(emptyLabel);
                        kerbalCells.Add(elems.evaLS_Value);
                        kerbalCells.Add(elems.evaProp);
                        kerbalCells.Add(emptyLabel);
                    }
                }

                vert.AddChild(
                    new DialogGUIGridLayout(new RectOffset(),
                        new Vector2(cellWidth, 20),
                        Vector2.zero,
                        UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                        UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                        TextAnchor.MiddleLeft,
                        UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 4,
                        kerbalCells.ToArray()));
            }

            // Define the header which contains additional info
            // (status, Consumables)
            DialogGUIGridLayout statusGrid =
                new DialogGUIGridLayout(new RectOffset(),
                    new Vector2(cellWidth * 2, 20),
                    Vector2.zero,
                    UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                    UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                    TextAnchor.MiddleLeft,
                    UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 2,
                    statusLabel, consLabel, riskLayout);

            // Set up the pop window
            MultiOptionDialog multi = new MultiOptionDialog(
                "lifesupport_readout",
                "",
                "LifeSupport Readout",
                UISkinManager.defaultSkin,
                new Rect(position, size),
                statusGrid,
                new DialogGUIScrollList(Vector2.zero, false, true,
                    new DialogGUIVerticalLayout(false, false, 1f,
                        offset, TextAnchor.UpperLeft,
                        new DialogGUIContentSizer(
                            UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained,
                            UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize,
                            true),
                        vert)));

            gui = PopupDialog.SpawnPopupDialog(
                multi,
                false,
                UISkinManager.defaultSkin,
                false,
                "");

            showgui = true;
            drewgui = false;
        }

        public static void PressFillEva(string name)
        {
            Util.PostUpperMessage("PRESS: " + name);
            FillEVAResource(name);
        }

        private void ButtonOnFalse()
        {
            Util.PostUpperMessage("CALL: onfalse");
            if (showgui && drewgui)
            {
                position = gui.GetComponent<RectTransform>().position;
                position.x = position.x / Screen.width + 0.5f;
                position.y = position.y / Screen.height + 0.5f;
                Util.Log("Save GUI pos: " + position.ToString());
            }
            showgui = false;
            gui.Dismiss();
        }

        private void UpdateGUI()
        {
            drewgui = true;

            Vessel vessel = FlightGlobals.ActiveVessel;
            int crewCount = vessel.GetCrewCount();
            var parts = vessel.FindPartModulesImplementing<LifeSupportReportable>();

            // Update live Kerbal numbers
            foreach (LifeSupportReportable module in parts)
            {
                string timestr = module.ReportLifeSupport();

                foreach (ProtoCrewMember kerbal in module.part.protoModuleCrew)
                {
                    double evaLS = EVALifeSupportTracker.GetEVALSInfo(kerbal.name).ls_current;
                    string evastr = Util.DaysToString(evaLS / C.EVA_LS_DRAIN_PER_DAY);
                    labelMap[kerbal.name].shipLS.SetOptionText(timestr);
                    labelMap[kerbal.name].evaLS.SetOptionText(evastr);

                    if (Config.DEBUG_SHOW_EVA)
                    {
                        var info = EVALifeSupportTracker.GetEVALSInfo(kerbal.name);
                        labelMap[kerbal.name].evaLS_Value.SetOptionText(info.ls_current.ToString());
                        labelMap[kerbal.name].evaProp.SetOptionText(info.prop_current.ToString());

                    }
                }
            }

            // Any Kerbals not found above are assumed KIA
            // Would be great to move this to a GameEvent
            // such as GameEvents.onKerbalRemoved()
            if (crewCount < labelMap.Count)
            {
                List<string> fullcrew = vessel.GetVesselCrew().ConvertAll(a => a.name);
                foreach (string name in labelMap.Keys.ToArray())
                {
                    if (!fullcrew.Contains(name))
                    {
                        labelMap[name].shipLS.SetOptionText("KIA");
                        labelMap[name].evaLS.SetOptionText("KIA");
                        labelMap.Remove(name);
                    }
                }
            }

            // Update status
            double curr, max;
            vessel.GetConnectedResourceTotals(lsID, out curr, out max);
            string status;
            if (crewCount == 0)
                status = "No crew";
            else if (Util.BreathableAir(vessel))
                status = "Breathable air";
            else
                status = "Life support active";
            statusLabel.SetOptionText($"       Status:  {status}");

            if (FlightGlobals.ActiveVessel.isEVA)
            {
                vessel.GetConnectedResourceTotals(evapropID, out curr, out max);
                string prefix = "";
                string suffix = "</color>";
                if (curr < 0.5)
                    prefix = C.GUI_HARDWARN_COLOR;
                else if (curr < 1.0)
                    prefix = C.GUI_LITEWARN_COLOR;
                else
                    suffix = "";
                consLabel.SetOptionText($"Propellant:  {prefix}{string.Format("{0:0.00}", curr)}{suffix}");
            }
            else
            {
                vessel.GetConnectedResourceTotals(consID, out curr, out max);
                double consDays = curr / C.CONS_PER_LS;
                consLabel.SetOptionText($"Consumables:  {Util.DaysToString(consDays)}");
            }
        }

        public void OnGUI()
        {
            if (showgui)
                UpdateGUI();
        }

        private static int CompareCrewNames(ProtoCrewMember c1, ProtoCrewMember c2)
        {
            return c1.name.CompareTo(c2.name);
        }

        private static int FillEVAResource(string kerbalName)
        {
            Vessel active = FlightGlobals.ActiveVessel;

            double conversion_rate;
            string resource_name;
            conversion_rate = C.CONS_TO_EVA_LS;
            resource_name = C.NAME_EVA_LIFESUPPORT;

            Util.Log("Processing FillEVA resource request for " + resource_name);

            // Player is controlling ship
            if (false)
            {
                Util.Log("FillEVA pressed for active vessel " + active.name);

                double eva_request_total = 0;

                // Map of kerbals in tracking, and how much they're requesting
                Dictionary<string, double> kerbal_requests = new Dictionary<string, double>();

                foreach (ProtoCrewMember kerbal in active.GetVesselCrew())
                {
                    // Previously had a check here if Kerbal was in EVA tracking.
                    // This should now be covered by LifeSupportModule adding
                    // all missing Kerbals to tracking in OnStart.

                    var info = EVALifeSupportTracker.GetEVALSInfo(kerbal.name);
                    double request = 0;

                    request = info.ls_max - info.ls_current;

                    eva_request_total += request;
                    kerbal_requests.Add(kerbal.name, request);

                    Util.Log("    Kerbal " + kerbal.name + " has EVA need for " + request);
                }

                // If no EVA request, exit early
                if (eva_request_total < C.DOUBLE_MARGIN)
                {
                    Util.Log("All crewmembers full! Skipping EVA refill");
                    Util.PostUpperMessage("EVA resources already full!");
                    return -1;
                }

                // Have to update this!
                Cons2LSModule module = new Cons2LSModule();
                // Deduct Consumables
                double obtained = module.part.RequestResource(C.NAME_CONSUMABLES, conversion_rate * eva_request_total);
                double frac = obtained / eva_request_total;

                Util.Log("    EVA request total  = " + eva_request_total);
                Util.Log("    Request * factor   = " + conversion_rate * eva_request_total);
                Util.Log("    Obtained           = " + obtained);
                Util.Log("    Fraction available = " + frac);

                // Distribute EVA LS proportionally
                foreach (string name in kerbal_requests.Keys)
                {
                    double add = kerbal_requests[name] * frac;
                    EVALifeSupportTracker.AddEVAAmount(name, add, EVA_Resource.LifeSupport);

                    Util.Log("    Adding " + add + " to " + name);
                }

                if (frac > C.DOUBLE_ALMOST_ONE)
                    return 0;
                else if (frac < C.DOUBLE_MARGIN)
                    return 2;
                else
                    return 1;
            }
            // Player is controlling EVA
            else
            {
                Util.Log("FillEVA pressed for: " + kerbalName);

                // This works right now because the tracker updates live.
                // May break in the future.
                var info = EVALifeSupportTracker.GetEVALSInfo(kerbalName);
                double eva_request = 0;
                eva_request = info.ls_max - info.ls_current;

                Cons2LSModule module = active.FindPartModuleImplementing<Cons2LSModule>();
                double obtained = module.part.RequestResource(C.NAME_CONSUMABLES, conversion_rate * eva_request);
                double add = obtained / conversion_rate;
                EVALifeSupportTracker.AddEVAAmount(kerbalName, add, EVA_Resource.LifeSupport);

                Util.Log("    EVA Request  = " + eva_request);
                Util.Log("    Amt Obtained = " + obtained);

                // If enough resources were added
                if (add > eva_request - C.DOUBLE_MARGIN)
                    return 0;
                // If Consumables are empty
                else if (add < C.DOUBLE_MARGIN)
                    return 2;
                // If Consumables are almost empty, partial refill
                else
                    return 1;
            }
        }
    }
}
