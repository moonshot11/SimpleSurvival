﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimpleSurvival
{
    /// <summary>
    /// Global tracking of EVA LifeSupport when Kerbals are not in EVA.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class EVALifeSupportTracker : MonoBehaviour
    {
        /// <summary>
        /// Title of each individual node holding one Kerbal's info
        /// </summary>
        private const string NODE_EVA_TRACK = "KERBAL_EVA_LS";

        public static bool AllowUnsafeActivity = false;

        /// <summary>
        /// Stores the EVA info for a Kerbal
        /// </summary>
        public class EvaInfo
        {
            /// <summary>
            /// The current amount of EVA LifeSupport for this Kerbal.
            /// </summary>
            public double ls_current;
            /// <summary>
            /// The maximum amount of LifeSupport this Kerbal has.
            /// Persists until s/he is recovered.
            /// </summary>
            public double ls_max;

            public EvaInfo(double ls_current, double ls_max)
            {
                this.ls_current = ls_current;
                this.ls_max = ls_max;
            }

            /// <summary>
            /// Returns a deep copy of this data structure.
            /// </summary>
            /// <returns></returns>
            public EvaInfo Copy()
            {
                return new EvaInfo(ls_current, ls_max);
            }
        }

        /// <summary>
        /// Stores the live EVA LS tracking info
        /// </summary>
        private static Dictionary<string, EvaInfo> evals_info = null;

        /// <summary>
        /// Run once on startup, set up hooks to rest of game
        /// </summary>
        private void Awake()
        {
            Log("Call -> Awake(..) " + HighLogic.LoadedScene.ToString());

            evals_info = new Dictionary<string, EvaInfo>();

            // Kerbals will be added to tracking
            GameEvents.OnVesselRollout.Add(OnVesselRollout);
            GameEvents.onCrewTransferred.Add(OnCrewTransferred);

            // Cover all the situations when a Kerbal's EVA LS will be reset.
            // Unfortunately, roster status changes can't be used exclusively,
            // since the game moves ProtoCrewMembers from Assigned to Available,
            // then back to Assigned when transferring between parts/vessels
            // (e.g. "Transfer Crew" or EVA).
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
            GameEvents.onKerbalStatusChange.Add(OnKerbalStatusChange);
            GameEvents.onKerbalRemoved.Add(OnKerbalRemoved);

            // Refresh EVA Max
            GameEvents.onDockingComplete.Add(OnDockingComplete);
            GameEvents.onVesselLoaded.Add(OnVesselLoaded);

            // Block unsafe actions
            GameEvents.onCrewTransferSelected.Add(OnCrewTransferSelected);
            GameEvents.onAttemptEva.Add(OnAttemptEva);
        }

        private void OnAttemptEva(ProtoCrewMember kerbal, Part part, Transform xform)
        {
            if (AllowUnsafeActivity || FlightEVA.fetch.overrideEVA)
                return;

            Util.Log($"OnAttemptEva: LS = {evals_info[kerbal.name].ls_current}");
            Util.Log($"OnAttemptEva: 30s = {C.EVA_LS_30_SECONDS}");

            if (evals_info[kerbal.name].ls_current < C.EVA_LS_30_SECONDS)
            {
                FlightEVA.fetch.overrideEVA = true;
                Util.PostUpperMessage($"{kerbal.name} has less than 30 seconds of EVA life support!");
            }
        }

        private void OnCrewTransferSelected(CrewTransfer.CrewTransferData data)
        {
            Util.Log($"OnAttemptTransfer: {data.destPart.partInfo.title}");
            if (AllowUnsafeActivity || !data.canTransfer)
                return;

            // Calculate whether total part+suit LS is less than 30 seconds
            double destLS = data.destPart.Resources[C.NAME_LIFESUPPORT].amount;
            double partFactor = destLS / C.LS_30_SECONDS / (data.destPart.protoModuleCrew.Count + 1);
            double evaFactor = evals_info[data.crewMember.name].ls_current / C.EVA_LS_30_SECONDS;

            Util.Log($"eva factor  = {evaFactor}");
            Util.Log($"part factor = {partFactor}");
            Util.Log($"crew count  = {data.destPart.protoModuleCrew.Count}");

            if (partFactor + evaFactor < 1.0 &&
                destLS < data.sourcePart.Resources[C.NAME_LIFESUPPORT].amount)
            {
                data.canTransfer = false;
                Util.PostUpperMessage($"Moving to part is unsafe!");
            }
        }

        /// <summary>
        /// Add a Kerbal to EVA LS tracking. By default, do nothing if entry
        /// already exists. This behavior can be overridden.
        /// </summary>
        /// <param name="name">Name of the Kerbal to add to tracking</param>
        /// <param name="overwrite">Overwrite the Kerbal's info if it already exists</param>
        public static void AddKerbalToTracking(string name, bool overwrite = false)
        {
            Log("Call -> AddKerbal(..) for " + name);

            double eva_max = Util.MaxAllowedEvaLS();

            // Assume that this Kerbal's info should be reset,
            // but warn in the log file just in case.
            if (evals_info.ContainsKey(name))
            {
                if (overwrite)
                {
                    Log("Uh oh! " + name + " is already being tracked. Resetting...");
                    evals_info.Remove(name);
                }
                else
                {
                    Log("Kerbal " + name + " is already being tracked. Skipping...");
                    return;
                }
            }

            Log("Adding current/max EVA LS of " + eva_max + " for " + name);

            EvaInfo info = new EvaInfo(eva_max, eva_max);
            evals_info.Add(name, info);
        }

        private void OnDockingComplete(GameEvents.FromToAction<Part, Part> action)
        {
            Util.Log("Call EVALSTrack -> OnDockingComplete()");
            RefreshEVALSMax(action.to.vessel);
        }

        private void OnVesselLoaded(Vessel vessel)
        {
            Util.Log("Call EVALSTrack -> OnVesselLoaded()");
            if (vessel.isEVA)
                return;
            RefreshEVALSMax(vessel);
        }

        /// <summary>
        /// Refresh EVA LS max value
        /// </summary>
        /// <param name="vessel"></param>
        private void RefreshEVALSMax(Vessel vessel)
        {
            if (vessel == null)
                throw new NullReferenceException();

            Util.Log($"Call EVALSTrack -> FillEVALS({vessel.vesselName})");

            var crew = vessel.GetVesselCrew();
            if (crew.Count == 0)
                return;

            bool updateMax = vessel.CanUpdateEVAStat(Config.EVA_MAX_UPDATE);

            foreach (ProtoCrewMember kerbal in crew)
            {
                AddKerbalToTracking(kerbal.name);
                if (updateMax)
                {
                    double lsmax = Util.MaxAllowedEvaLS();

                    Util.Log($"Updating Kerbal max values for: {kerbal.name}");
                    Util.Log($"  LS max -> {lsmax}");

                    evals_info[kerbal.name].ls_max = Util.MaxAllowedEvaLS();
                }
            }
        }

        private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> action)
        {
            Util.Log("Call EVALSTrack -> OnCrewTransferred()");

            // If transfer is to/from EVA, manage EVA resources
            // and return. Main case below is for part-to-part
            // transfers within same vessel.
            if (action.from.vessel.isEVA != action.to.vessel.isEVA)
            {
                if (!action.from.vessel.isEVA)
                    RefreshEVALSMax(action.from.vessel);
                else
                    RefreshEVALSMax(action.to.vessel);
                return;
            }

            ProtoCrewMember kerbal = action.host;

            // It's possible Kerbal is coming from a part that does not have a LifeSupportModule
            // Alternative solution is to add LifeSupportModule + default resource values
            // to any parts with ModuleCommand not defined in the cfg.
            // Undeterministic if other configs add their own definitions in the future.
            //
            // EVALifeSupportModule.OnStart() covers the case when destination part is EVA.
            AddKerbalToTracking(kerbal.name);

            double current_eva = evals_info[kerbal.name].ls_current;

            if (current_eva < C.EVA_LS_30_SECONDS &&
                action.from.Resources[C.NAME_LIFESUPPORT].amount > C.DOUBLE_MARGIN &&
                action.to.Resources[C.NAME_LIFESUPPORT].amount == 0.0)
            {
                TimeWarp.SetRate(0, true);
                Util.PostUpperMessage(kerbal.name + " has " + (int)(current_eva / C.EVA_LS_DRAIN_PER_SEC) + " seconds to live!", 1);
            }
        }

        /// <summary>
        /// Returns true if the Kerbal's EVA LS is currently being tracked
        /// </summary>
        /// <param name="name">Name of Kerbal as defined by game</param>
        /// <returns>True if tracked, false otherwise.</returns>
        public static bool InTracking(string name)
        {
            return evals_info.ContainsKey(name);
        }

        /// <summary>
        /// Returns a COPY to this Kerbal's EVA LS info.
        /// Cannot modify in place.
        /// </summary>
        /// <param name="name">Name of Kerbal</param>
        /// <returns></returns>
        public static EvaInfo GetEVALSInfo(string name)
        {
            if (!evals_info.ContainsKey(name))
                return null;

            return evals_info[name].Copy();
        }

        /// <summary>
        /// When the ship is "rolled out" from the VAB to the LaunchPad,
        /// via the VAB or the Launch GUI
        /// </summary>
        /// <param name="ship"></param>
        private void OnVesselRollout(ShipConstruct ship)
        {
            Log("Call -> OnVesselRollout(..) for vessel: " + ship.shipName);

            foreach (Part part in ship.Parts)
            {
                foreach (ProtoCrewMember kerbal in part.protoModuleCrew)
                {
                    AddKerbalToTracking(kerbal.name, true);
                }
            }
        }

        /// <summary>
        /// Set the amount of EVA LifeSupport a Kerbal currently has
        /// </summary>
        /// <param name="name">The Kerbal's name</param>
        /// <param name="amount">The current amount</param>
        public static void SetCurrentAmount(string name, double amount)
        {
            try
            {
                evals_info[name].ls_current = amount;
            }
            catch (KeyNotFoundException e)
            {
                Log("SetCurrentEVAAmount Exception thrown: Kerbal " + name + " not found to update tracking!");
                Log(e.ToString());
            }
        }

        /// <summary>
        /// Add to the current EVA amount
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        /// <returns>Returns the amount of EVA LS after adding</returns>
        public static double AddEVALSAmount(string name, double amount)
        {
            try
            {
                evals_info[name].ls_current =
                    Math.Min(evals_info[name].ls_current + amount,
                    evals_info[name].ls_max);
            }
            catch (KeyNotFoundException e)
            {
                Log("AddEVAAmount Exception thrown: Kerbal " + name + " not found to update tracking!");
                Log(e.ToString());
            }

            return evals_info[name].ls_current;
        }

        /// <summary>
        /// Remove Kerbal from tracking.
        /// </summary>
        /// <param name="proto"></param>
        private void OnVesselRecovered(ProtoVessel proto, bool mystery_var)
        {
            Log("Call -> OnVesselRecovered(..) for vessel: " + proto.vesselName);
            Log("Mystery bool = " + mystery_var);

            foreach (ProtoCrewMember kerbal in proto.GetVesselCrew())
            {
                Log("Clearing EVA LS data for: " + kerbal.name);
                evals_info.Remove(kerbal.name);
                Log("    Successful!");
            }
        }

        private void OnKerbalStatusChange(ProtoCrewMember kerbal,
            ProtoCrewMember.RosterStatus old_status, ProtoCrewMember.RosterStatus new_status)
        {
            // Kerbals regularly change to Available, then back to Assigned
            // when in space, so can't use this method to track Kerbals in service
            if (new_status == ProtoCrewMember.RosterStatus.Dead
                || new_status == ProtoCrewMember.RosterStatus.Missing)
            {
                Log("Call -> OnKerbalStatusChange(..) " + kerbal.name + ": " + old_status.ToString() + " -> " + new_status.ToString());
                Log("Clearing EVA LS info for " + kerbal.name);
                evals_info.Remove(kerbal.name);
            }
        }

        // Currently only seems to track Kerbals removed entirely from roster,
        // but keep in for the sake of completeness for now
        private void OnKerbalRemoved(ProtoCrewMember kerbal)
        {
            Log("Call -> OnKerbalRemoved(..) " + kerbal.name);
            evals_info.Remove(kerbal.name);
        }

        /// <summary>
        /// Load EVA tracking info to ConfigNode.
        /// </summary>
        /// <param name="scenario_node"></param>
        public static void Load(ConfigNode scenario_node)
        {
            Log("Call -> OnLoad(..)");

            Log("Clearing EVA LS tracking");
            evals_info = new Dictionary<string, EvaInfo>();

            // This isn't initialized when an old save is loaded.
            // This is purely for safety. All ConfigNode values
            // should be added naturally over the normal course of play.

            // Lvl "1" == fully upgraded complex
            string astro_lvl = "1";

            if (HighLogic.CurrentGame?.Mode == Game.Modes.CAREER)
                astro_lvl = HighLogic.CurrentGame.config
                .GetNode("SCENARIO", "name", "ScenarioUpgradeableFacilities")
                .GetNode("SpaceCenter/AstronautComplex").GetValue("lvl");

            double game_ls_max = Util.MaxAllowedEvaLS(astro_lvl);

            foreach (ConfigNode node in scenario_node.GetNodes(NODE_EVA_TRACK))
            {
                string name = node.GetValue("name");

                double ls_current = Convert.ToDouble(
                    Util.GetConfigNodeValue(node, "lifesupport_amount", game_ls_max));
                double ls_max = Convert.ToDouble(
                    Util.GetConfigNodeValue(node, "lifesupport_maxAmount", game_ls_max));

                evals_info.Add(name, new EvaInfo(ls_current, ls_max));
                Log("Adding " + name + ": [" + ls_current + ", " + ls_max + "]");
            }
        }

        /// <summary>
        /// Save EVA LifeSupport info to ConfigNode.
        /// </summary>
        /// <param name="scenario_node"></param>
        public static void Save(ConfigNode scenario_node)
        {
            Log("Call -> OnSave(..)");

            // This shouldn't ever happen.
            if (evals_info == null)
            {
                Log("CheckThis --> evals_info is null, aborting OnSave");
                return;
            }

            foreach (string name in evals_info.Keys)
            {
                var info = evals_info[name];

                Log("Adding " + name + " to ConfigNode");
                Log("  name      = " + name);
                Log("  lifesupport_amount    = " + info.ls_current);
                Log("  lifesupport_maxAmount = " + info.ls_max);

                ConfigNode node = scenario_node.AddNode(NODE_EVA_TRACK);
                node.AddValue("name", name);
                node.AddValue("lifesupport_amount", info.ls_current);
                node.AddValue("lifesupport_maxAmount", info.ls_max);
            }
        }

        // This module needs its own logger
        private static void Log(string message)
        {
            KSPLog.print("SimpleSurvival: EVALifeSupportTracker -> " + message);
        }
    }
}
