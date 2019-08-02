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

        private ProtoCrewMember kerbal;

        public GUIElements(ProtoCrewMember kerbal)
        {
            this.kerbal = kerbal;
            fillEVAButton = new DialogGUIButton<string>(
                "Fill EVA", LifeSupportGUI.PressFillEva, kerbal.name,
                EnabledCondition: LifeSupportGUI.EnableFillButton,
                dismissOnSelect: false);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LifeSupportGUI : MonoBehaviour
    {

        private static bool showgui = false;
        private static ApplicationLauncherButton toolbarButton = null;
        private static PopupDialog gui = null;
        private static Vector2 position = new Vector2(0.5f, 0.5f);
        private static Vector2 size = new Vector2(500, 200);
        private static Dictionary<string, GUIElements> labelMap
            = new Dictionary<string, GUIElements>();

        // Temp for debugging
        private static DialogGUILabel debugLabel = null;

        public void Awake()
        {
            Util.Log("LifeSupportGUI Awake");
            GameEvents.onGUIApplicationLauncherReady.Add(AddToolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveToolbar);
            GameEvents.onVesselChange.Add(ToggleButton);
        }

        public void OnDisable()
        {
            Util.Log("LifeSupportGUI OnDisable");
            GameEvents.onGUIApplicationLauncherReady.Remove(AddToolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveToolbar);
            GameEvents.onVesselChange.Remove(ToggleButton);
            // Ensure it's removed from the MainMenu scene
            RemoveToolbar();
        }

        public void ToggleButton(Vessel vessel)
        {
            if (toolbarButton?.isActiveAndEnabled ?? false)
            {
                ButtonOnFalse();
                ButtonOnTrue();
            }
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

        private void ButtonOnTrue()
        {
            Util.PostUpperMessage("CALL: ontrue");
            showgui = true;

            // Store labels in separate data structure for easy access when updating
            labelMap.Clear();
            List<DialogGUIBase> entries = new List<DialogGUIBase>();
            var kerbals = FlightGlobals.ActiveVessel.GetVesselCrew();
            kerbals.Sort(CompareCrewNames);

            foreach (ProtoCrewMember kerbal in kerbals)
            {
                GUIElements elems = new GUIElements(kerbal);
                labelMap.Add(kerbal.name, elems);
                entries.Add(new DialogGUILabel(kerbal.name, true, true));
                entries.Add(elems.shipLS);
                entries.Add(elems.evaLS);
                entries.Add(new DialogGUIButton<string>("Fill EVA", PressFillEva, kerbal.name,
                    dismissOnSelect: false));
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
                                new Vector2(100, 20),
                                Vector2.zero,
                                UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                                UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                                TextAnchor.MiddleLeft,
                                UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 4,
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

        public static void PressFillEva(string name)
        {
            Util.PostUpperMessage("PRESS: " + name);
        }

        /// <summary>
        /// Manages the true condition which enables the "Fill EVA" button.
        /// </summary>
        /// <returns></returns>
        public static bool EnableFillButton()
        {
            // Technically should also check if on EVA, but this condition isn't possible
            // while on EVA anyway.
            return FlightGlobals.ActiveVessel.FindPartModulesImplementing<Cons2LSModule>().Count > 0;
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
            var parts = vessel.FindPartModulesImplementing<LifeSupportReportable>();
            List<string> deadKerbals = new List<string>();

            foreach (LifeSupportReportable module in parts)
            {
                List<ProtoCrewMember> crew = module.part.protoModuleCrew;

                string timestr = module.ReportLifeSupport();

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
                    string evastr = Util.DaysToString(evaLS / C.EVA_LS_DRAIN_PER_DAY);
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

        private int FillEVAResource()
        {
            Vessel active = FlightGlobals.ActiveVessel;

            double conversion_rate;
            string resource_name;
            EVA_Resource choice = EVA_Resource.LifeSupport;

            switch (choice)
            {
                case EVA_Resource.LifeSupport:
                    conversion_rate = C.CONS_TO_EVA_LS;
                    resource_name = C.NAME_EVA_LIFESUPPORT;
                    break;
                case EVA_Resource.Propellant:
                    conversion_rate = C.CONS_TO_EVA_PROP;
                    resource_name = C.NAME_EVA_PROPELLANT;
                    break;
                default:
                    throw new ArgumentException("Cons2LSModule.FillEVAResource, request enum not properly set");
            }

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

                    switch (choice)
                    {
                        case EVA_Resource.Propellant:
                            request = info.prop_max - info.prop_current;
                            break;
                        case EVA_Resource.LifeSupport:
                            request = info.ls_max - info.ls_current;
                            break;
                    }

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
                    EVALifeSupportTracker.AddEVAAmount(name, add, choice);

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
                Util.Log("FillEVA pressed for EVA: " + active.GetVesselCrew()[0].name);

                string name = active.GetVesselCrew()[0].name;

                // This works right now because the tracker updates live.
                // May break in the future.
                var info = EVALifeSupportTracker.GetEVALSInfo(name);
                double eva_request = 0;

                switch (choice)
                {
                    case EVA_Resource.Propellant:
                        eva_request = info.prop_max - info.prop_current;
                        break;
                    case EVA_Resource.LifeSupport:
                        eva_request = info.ls_max - info.ls_current;
                        break;
                }

                Cons2LSModule module = active.FindPartModuleImplementing<Cons2LSModule>();
                double obtained = module.part.RequestResource(C.NAME_CONSUMABLES, conversion_rate * eva_request);
                double add = obtained / conversion_rate;
                active.rootPart.RequestResource(resource_name, -add);

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
