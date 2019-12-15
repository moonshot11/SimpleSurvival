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
        public DialogGUILabel nameLabel = new DialogGUILabel("", true, true);

        public string[] compressNames;
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
            compressNames = new string[2]
            {
                kerbal.name.Replace(" Kerman", " (Ship)"),
                kerbal.name.Replace(" Kerman", $" {C.GUI_LITEWARN_COLOR}(Suit)</color>")
            };
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
        /// <summary>
        /// Show the minimal GUI instead of the full GUI?
        /// </summary>
        private static bool compress = true;

        private static ApplicationLauncherButton toolbarButton = null;
        private static PopupDialog gui = null;
        private static Vector2 position = new Vector2(0.5f, 0.5f);
        private static Vector2 posInitOffset = Vector2.zero;
        private static Vector2 size = new Vector2(470, 200);
        private readonly RectOffset headerOffset = new RectOffset(20, 0, 0, 0);
        private readonly RectOffset noOffset = new RectOffset(0, 0, 0, 0);
        private static Dictionary<string, GUIElements> labelMap
            = new Dictionary<string, GUIElements>();

        private DialogGUIButton sizeUpButton;
        private DialogGUIButton sizeDownButton;
        private static DialogGUIToggle riskToggle;
        private static DialogGUIHorizontalLayout riskLayout;

        private DialogGUILabel statusLabel = new DialogGUILabel("Status", true, true);
        private DialogGUILabel consLabel = new DialogGUILabel("Consumables: x/x", 200, 0);

        private int consID = PartResourceLibrary.Instance.GetDefinition(C.NAME_CONSUMABLES).id;
        private int lsID = PartResourceLibrary.Instance.GetDefinition(C.NAME_LIFESUPPORT).id;
        private int evapropID = PartResourceLibrary.Instance.GetDefinition(C.NAME_EVA_PROPELLANT).id;

        private const int cellWidth = 100;
        private const int HEIGHT_INCR = 50;
        private const int HEIGHT_MIN = 150;
        private const int HEIGHT_MAX = 900000;
        private const string ORANGE = "<color=#f4b00c>";

        private Dictionary<Part, DialogGUILabel> emptyPartLabels =
            new Dictionary<Part, DialogGUILabel>();

        public void Awake()
        {
            Util.Log("LifeSupportGUI Awake");
            GameEvents.onGUIApplicationLauncherReady.Add(AddToolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveToolbar);

            // -- State changes that require GUI refresh --
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onCrewTransferred.Add(OnCrewTransferred);
            GameEvents.onDockingComplete.Add(OnDockingComplete);
            GameEvents.onPartUndockComplete.Add(OnPartUndockComplete);
            GameEvents.onPartDeCoupleComplete.Add(OnPartDecoupleComplete);

            GameEvents.onGamePause.Add(OnGamePause);
        }

        public void OnDisable()
        {
            Util.Log("LifeSupportGUI OnDisable");
            GameEvents.onGUIApplicationLauncherReady.Remove(AddToolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveToolbar);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onCrewTransferred.Remove(OnCrewTransferred);
            GameEvents.onDockingComplete.Remove(OnDockingComplete);
            GameEvents.onPartUndockComplete.Remove(OnPartUndockComplete);
            GameEvents.onPartDeCoupleComplete.Remove(OnPartDecoupleComplete);
            GameEvents.onGamePause.Remove(OnGamePause);
            // Ensure it's removed from the MainMenu scene
            RemoveToolbar();
        }

        public void OnDockingComplete(GameEvents.FromToAction<Part, Part> ev)
        {
            Util.PostUpperMessage("ON DOCKING COMPLETE");
            RefreshGUI();
        }

        public void OnPartUndockComplete(Part part)
        {
            Util.PostUpperMessage("ON PART UNDOCK");
            RefreshGUI();
        }

        public void OnPartDecoupleComplete(Part part)
        {
            Util.PostUpperMessage("ON VESSEL DECOUPLE");
            RefreshGUI();
        }

        public void OnVesselChange(Vessel vessel)
        {
            Util.PostUpperMessage("ON VESSEL CHANGE");
            RefreshGUI();
        }

        public void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> ev)
        {
            Util.PostUpperMessage("ON CREW TRANSFER");
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

        public void SizeUp()
        {
            size.y += HEIGHT_INCR;
            posInitOffset.y = -HEIGHT_INCR / 2;
            RefreshGUI();
            posInitOffset.y = 0;
        }

        public void SizeDown()
        {
            size.y -= HEIGHT_INCR;
            posInitOffset.y = HEIGHT_INCR / 2;
            RefreshGUI();
            posInitOffset.y = 0;
        }

        public bool CanSizeUp() => size.y + HEIGHT_INCR <= HEIGHT_MAX;
        public bool CanSizeDown() => size.y - HEIGHT_INCR >= HEIGHT_MIN;

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
            Util.PostUpperMessage("Button set to " + arg.ToString());
            EVALifeSupportTracker.AllowUnsafeActivity = arg;
        }

        private void ToggleCompressGUI(bool arg)
        {
            compress = !compress;
            RefreshGUI();
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
            DialogGUIVerticalLayout vert = new DialogGUIVerticalLayout(
                false, false, 1f,
                new RectOffset(), TextAnchor.UpperLeft);

            DialogGUIBase[] vertHeader;

            if (compress)
            {
                vertHeader = new DialogGUIBase[2]
                {
                    new DialogGUILabel("<b>Kerbal</b>", true, true),
                    new DialogGUILabel("<b>Life Support</b>", true, true)
                };
            }
            else
            {
                vertHeader = new DialogGUIBase[4]
                {
                    new DialogGUILabel("<b>Kerbal</b>", true, true),
                    new DialogGUILabel("<b>Ship Life Support</b>", true, true),
                    new DialogGUILabel("<b>Suit Life Support</b>", true, true),
                    new DialogGUILabel("", true, true)
                };
            }

            vert.AddChild(
                new DialogGUIGridLayout(new RectOffset(),
                    new Vector2(cellWidth, 20),
                    Vector2.zero,
                    UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                    UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                    TextAnchor.MiddleLeft,
                    UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                    compress ? 2 : 4,
                    vertHeader
                    ));

            emptyPartLabels.Clear();

            DialogGUILabel emptyLabel = new DialogGUILabel("", true, true);

            foreach (LifeSupportReportable module in modules)
            {
                if (module.part.protoModuleCrew.Count == 0)
                {
                    emptyPartLabels.Add(module.part, new DialogGUILabel("EE", true, true));
                    continue;
                }

                List<ProtoCrewMember> crew = new List<ProtoCrewMember>(module.part.protoModuleCrew);
                List<DialogGUIBase> kerbalCells = new List<DialogGUIBase>();
                crew.Sort(CompareCrewNames);
                vert.AddChild(new DialogGUILabel($"{ORANGE}<b>{module.part.partInfo.title}</b></color>"));

                foreach (ProtoCrewMember kerbal in crew)
                {
                    GUIElements elems = new GUIElements(kerbal, buttonEnable);
                    labelMap.Add(kerbal.name, elems);
                    kerbalCells.Add(elems.nameLabel);
                    kerbalCells.Add(elems.shipLS);
                    if (!compress)
                    {
                        elems.nameLabel.SetOptionText(kerbal.name);
                        kerbalCells.Add(elems.evaLS);
                        kerbalCells.Add(elems.fillEVAButton);
                    }

                    // Add raw EVA tracking values
                    if (Config.DEBUG_SHOW_EVA)
                    {
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
                        UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                        compress ? 2 : 4,
                        kerbalCells.ToArray()));
            }

            if (emptyPartLabels.Count > 0 && !compress)
            {
                vert.AddChild(new DialogGUISpace(20));

                foreach (Part part in emptyPartLabels.Keys)
                {
                    vert.AddChild(
                           new DialogGUIGridLayout(new RectOffset(),
                               new Vector2(cellWidth * 2, 20),
                               Vector2.zero,
                               UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                               UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                               TextAnchor.MiddleLeft,
                               UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 2,
                               new DialogGUILabel($"{ORANGE}{part.partInfo.title}</color>"),
                               emptyPartLabels[part]
                               ));
                }
            }

            // How can the SizeUp() -> RefreshGUI() -> OnButtonTrue() dependency
            // be altered to make these fields static, so that they don't have
            // to be re-initialized each time?
            sizeUpButton = new DialogGUIButton("+", SizeUp, CanSizeUp, false);
            sizeDownButton = new DialogGUIButton("-", SizeDown, CanSizeDown, false);

            DialogGUIToggleButton compressButton = new DialogGUIToggleButton(
                compress, "Minimize", ToggleCompressGUI, w: 70);
            DialogGUIHorizontalLayout compressLayout = new DialogGUIHorizontalLayout(
                false, true, 0f, noOffset, TextAnchor.MiddleLeft,
                compressButton, sizeDownButton, sizeUpButton);

            riskToggle = new DialogGUIToggle(
                EVALifeSupportTracker.AllowUnsafeActivity,
                "Allow unsafe crew transfer",
                RiskButtonSelected);
            riskLayout = new DialogGUIHorizontalLayout(
                true, false, 0f, noOffset, TextAnchor.MiddleLeft,
                riskToggle);

            // Define the header which contains additional info
            // (status, Consumables)
            DialogGUIGridLayout statusGrid =
                new DialogGUIGridLayout(headerOffset,
                    new Vector2(cellWidth * (compress ? 1 : 2), 20),
                    Vector2.zero,
                    UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                    UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                    TextAnchor.MiddleLeft,
                    UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, 2,
                    statusLabel, compressLayout, consLabel, riskLayout);

            // Set up the pop window
            size.x = compress ? 270 : 470;
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
            Util.Log("FillEVA pressed for: " + name);
            FillEVAResource(name);
        }

        private void ButtonOnFalse()
        {
            Util.PostUpperMessage("CALL: onfalse");
            if (showgui && drewgui)
            {
                position = gui.GetComponent<RectTransform>().position;
                position.x = (position.x + posInitOffset.x) / Screen.width + 0.5f;
                position.y = (position.y + posInitOffset.y) / Screen.height + 0.5f;
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
                bool empty;
                string timestr = module.ReportLifeSupport(out empty);

                if (emptyPartLabels.ContainsKey(module.part))
                {
                    emptyPartLabels[module.part].SetOptionText($"{ORANGE}{timestr}</color>");
                    continue;
                }

                foreach (ProtoCrewMember kerbal in module.part.protoModuleCrew)
                {
                    double evaLS = EVALifeSupportTracker.GetEVALSInfo(kerbal.name).ls_current;

                    if (compress)
                    {
                        if (!vessel.isEVA && !empty)
                        {
                            labelMap[kerbal.name].nameLabel.SetOptionText(labelMap[kerbal.name].compressNames[0]);
                            labelMap[kerbal.name].shipLS.SetOptionText(timestr);
                        }
                        else
                        {
                            labelMap[kerbal.name].nameLabel.SetOptionText(labelMap[kerbal.name].compressNames[1]);
                            string evastr = Util.DaysToString(evaLS / C.EVA_LS_DRAIN_PER_DAY);
                            labelMap[kerbal.name].shipLS.SetOptionText(evastr);
                        }
                    }
                    else
                    {
                        string evastr = Util.DaysToString(evaLS / C.EVA_LS_DRAIN_PER_DAY);
                        labelMap[kerbal.name].shipLS.SetOptionText(timestr);
                        labelMap[kerbal.name].evaLS.SetOptionText(evastr);
                    }

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
                status = "ACTIVE";
            statusLabel.SetOptionText($"{(compress ? "" : "Status:  ")}{status}");

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
                consLabel.SetOptionText($"{(compress ? "" : "Consumables:  ")}{Util.DaysToString(consDays)}");
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

        private static void FillEVAResource(string kerbalName)
        {
            // This works right now because the tracker updates live.
            // May break in the future.
            var info = EVALifeSupportTracker.GetEVALSInfo(kerbalName);
            double eva_request = info.ls_max - info.ls_current;

            Cons2LSModule module = FlightGlobals.ActiveVessel.FindPartModuleImplementing<Cons2LSModule>();
            double obtained = module.part.RequestResource(C.NAME_CONSUMABLES, C.CONS_PER_EVA_LS * eva_request);
            double add = obtained / C.CONS_PER_EVA_LS;
            EVALifeSupportTracker.AddEVAAmount(kerbalName, add, EVA_Resource.LifeSupport);

            Util.Log("    EVA Request  = " + eva_request);
            Util.Log("    Amt Obtained = " + obtained);
        }
    }
}
