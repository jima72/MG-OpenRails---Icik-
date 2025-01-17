﻿// COPYRIGHT 2013 - 2021 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public class CruiseControl

    {
        public CruiseControl(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
        }
        MSTSLocomotive Locomotive;
        Simulator Simulator;

        public bool Equipped = false;
        public bool SpeedIsMph = false;
        public bool SpeedRegulatorMaxForcePercentUnits = false;
        public float SpeedRegulatorMaxForceSteps = 0;
        public bool MaxForceSetSingleStep = false;
        public bool MaxForceKeepSelectedStepWhenManualModeSet = false;
        public bool ForceRegulatorAutoWhenNonZeroSpeedSelected = false;
        public List<string> SpeedRegulatorOptions = new List<string>();
        public SpeedRegulatorMode SpeedRegMode = SpeedRegulatorMode.Manual;
        public SpeedSelectorMode SpeedSelMode = SpeedSelectorMode.Neutral;
        public ControllerCruiseControlLogic CruiseControlLogic = new ControllerCruiseControlLogic();
        public float SelectedMaxAccelerationPercent = 0;
        public float SelectedMaxAccelerationStep = 0;
        public float SelectedSpeedMpS = 0;
        public int SelectedNumberOfAxles = 4;
        public float SpeedRegulatorNominalSpeedStepMpS = 0;
        public float MaxAccelerationMpSS = 0;
        public float MaxDecelerationMpSS = 0;
        public bool UseThrottle = false;
        public bool AntiWheelSpinEquipped = false;
        public float AntiWheelSpinSpeedDiffThreshold = 0.5f;
        public float DynamicBrakeMaxForceAtSelectorStep = 0;
        public float ForceThrottleAndDynamicBrake = 0;
        protected float maxForceN = 0;
        protected float trainBrakePercent = 0;
        protected float trainLength = 0;
        public int TrainLengthMeters = 0;
        public int RemainingTrainLengthToPassRestrictedZone = 0;
        public bool RestrictedSpeedActive = false;
        public float CurrentSelectedSpeedMpS = 0;
        protected float nextSelectedSpeedMps = 0;
        protected float restrictedRegionTravelledDistance = 0;
        protected float currentThrottlePercent = 0;
        protected double clockTime = 0;
        protected bool dynamicBrakeSetToZero = false;
        public float StartReducingSpeedDelta = 0.5f;
        public bool Battery = false;
        public bool DynamicBrakePriority = false;
        public List<int> ForceStepsThrottleTable = new List<int>();
        public List<float> AccelerationTable = new List<float>();
        public enum SpeedRegulatorMode { Manual, Auto, Testing, AVV }
        public enum SpeedSelectorMode { Parking, Neutral, On, Start }
        protected float absMaxForceN = 0;
        protected float brakePercent = 0;
        public float DynamicBrakeIncreaseSpeed = 0;
        public float DynamicBrakeDecreaseSpeed = 0;
        public uint MinimumMetersToPass = 19;
//        protected float relativeAcceleration;
        public float AccelerationRampMaxMpSSS = 0.7f;
        public float AccelerationDemandMpSS;
        public float AccelerationRampMinMpSSS = 0.01f;
        public bool ResetForceAfterAnyBraking = false;
        public float ParkingBrakeEngageSpeed = 0;
        public float ParkingBrakePercent = 0;
        public bool SkipThrottleDisplay = false;
        public bool DisableZeroForceStep = false;
        public bool DynamicBrakeIsSelectedForceDependant = false;
        public bool UseThrottleAsSpeedSelector = false;
        public bool UseThrottleAsForceSelector = false;
        public float Ampers = 0;
        public bool ContinuousSpeedIncreasing = false;
        public bool ContinuousSpeedDecreasing = false;
        public float PowerBreakoutAmpers = 0;
        public float PowerBreakoutSpeedDelta = 0;
        public float PowerResumeSpeedDelta = 0;
        public float PowerReductionDelayPaxTrain = 0;
        public float PowerReductionDelayCargoTrain = 0;
        public float PowerReductionValue = 0;
        public float MaxPowerThreshold = 0;
        public float SafeSpeedForAutomaticOperationMpS = 0;
        protected float SpeedSelectorStepTimeSeconds = 0;
        protected float elapsedTime = 0;
        public bool DisableCruiseControlOnThrottleAndZeroSpeed = false;
        public bool DisableCruiseControlOnThrottleAndZeroForce = false;

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortscruisecontrol(speedismph": SpeedIsMph = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(usethrottle": UseThrottle = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(speedselectorsteptimeseconds": SpeedSelectorStepTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.1f); break;
                case "engine(ortscruisecontrol(resetforceafteranybraking": ResetForceAfterAnyBraking = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(parkingbrakeengagespeed": ParkingBrakeEngageSpeed = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0); break;
                case "engine(ortscruisecontrol(parkingbrakepercent": ParkingBrakePercent = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                case "engine(ortscruisecontrol(maxpowerthreshold": MaxPowerThreshold = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                case "engine(ortscruisecontrol(safespeedforautomaticoperationmps": SafeSpeedForAutomaticOperationMpS = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                case "engine(ortscruisecontrol(maxforcepercentunits": SpeedRegulatorMaxForcePercentUnits = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(maxforcesteps": SpeedRegulatorMaxForceSteps = stf.ReadIntBlock(0); break;
                case "engine(ortscruisecontrol(maxforcesetsinglestep": MaxForceSetSingleStep = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(maxforcekeepselectedstepwhenmanualmodeset": MaxForceKeepSelectedStepWhenManualModeSet = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(forceregulatorautowhennonzerospeedselected": ForceRegulatorAutoWhenNonZeroSpeedSelected = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(continuousspeedincreasing": ContinuousSpeedIncreasing = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(disablecruisecontrolonthrottleandzerospeed": DisableCruiseControlOnThrottleAndZeroSpeed = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(disablecruisecontrolonthrottleandzeroforce": DisableCruiseControlOnThrottleAndZeroForce = stf.ReadBoolBlock(false); break;

                case "engine(ortscruisecontrol(forcestepsthrottletable":
                    foreach (var forceStepThrottleValue in stf.ReadStringBlock("").Replace(" ", "").Split(','))
                    {
                        ForceStepsThrottleTable.Add(int.Parse(forceStepThrottleValue));
                    }
                    break;
                case "engine(ortscruisecontrol(accelerationtable":
                    foreach (var accelerationValue in stf.ReadStringBlock("").Replace(" ", "").Split(','))
                    {
                        AccelerationTable.Add(float.Parse(accelerationValue));
                    }
                    break;
                case "engine(ortscruisecontrol(powerbreakoutampers": PowerBreakoutAmpers = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                case "engine(ortscruisecontrol(powerbreakoutspeeddelta": PowerBreakoutSpeedDelta = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                case "engine(ortscruisecontrol(powerresumespeeddelta": PowerResumeSpeedDelta = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                case "engine(ortscruisecontrol(powerreductiondelaypaxtrain": PowerReductionDelayPaxTrain = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.0f); break;
                case "engine(ortscruisecontrol(powerreductiondelaycargotrain": PowerReductionDelayCargoTrain = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.0f); break;
                case "engine(ortscruisecontrol(powerreductionvalue": PowerReductionValue = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                case "engine(ortscruisecontrol(disablezeroforcestep": DisableZeroForceStep = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(dynamicbrakeisselectedforcedependant": DynamicBrakeIsSelectedForceDependant = stf.ReadBoolBlock(false); break;                    
                case "engine(ortscruisecontrol(defaultforcestep": SelectedMaxAccelerationStep = stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f); break;
                case "engine(ortscruisecontrol(dynamicbrakemaxforceatselectorstep": DynamicBrakeMaxForceAtSelectorStep = stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f); break;
                case "engine(ortscruisecontrol(startreducingspeeddelta": StartReducingSpeedDelta = (stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f) / 10); break;
                case "engine(ortscruisecontrol(maxacceleration": MaxAccelerationMpSS = stf.ReadFloatBlock(STFReader.UNITS.Any, 1); break;
                case "engine(ortscruisecontrol(maxdeceleration": MaxDecelerationMpSS = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.5f); break;
                case "engine(ortscruisecontrol(antiwheelspinequipped": AntiWheelSpinEquipped = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(antiwheelspinspeeddiffthreshold": AntiWheelSpinSpeedDiffThreshold = stf.ReadFloatBlock(STFReader.UNITS.None, 0.5f); break;
                case "engine(ortscruisecontrol(nominalspeedstep": SpeedRegulatorNominalSpeedStepMpS = SpeedIsMph ? MpS.FromMpH(stf.ReadFloatBlock(STFReader.UNITS.Speed, 0)) : MpS.FromKpH(stf.ReadFloatBlock(STFReader.UNITS.Speed, 0)); break;
                case "engine(ortscruisecontrol(usethrottleasspeedselector": UseThrottleAsSpeedSelector = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(usethrottleasforceselector": UseThrottleAsForceSelector = stf.ReadBoolBlock(false); break;
                case "engine(ortscruisecontrol(dynamicbrakeincreasespeed": DynamicBrakeIncreaseSpeed = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.5f); break;
                case "engine(ortscruisecontrol(dynamicbrakedecreasespeed": DynamicBrakeDecreaseSpeed = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.5f); break;
                case "engine(ortscruisecontrol(options":
                    foreach (var speedRegulatorOption in stf.ReadStringBlock("").ToLower().Replace(" ", "").Split(','))
                    {
                        SpeedRegulatorOptions.Add(speedRegulatorOption.ToLower());
                    }
                    break;
                case "engine(ortscruisecontrol(controllercruisecontrollogic":
                    {
                        String speedControlLogic = stf.ReadStringBlock("none").ToLower();
                        switch (speedControlLogic)
                        {
                            case "full":
                                {
                                    CruiseControlLogic = ControllerCruiseControlLogic.Full;
                                    break;
                                }
                            case "speedonly":
                                {
                                    CruiseControlLogic = ControllerCruiseControlLogic.SpeedOnly;
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        public void Initialize()
        {
            Simulator = Locomotive.Simulator;
            clockTime = Simulator.ClockTime * 100;
        }

        public void Update()
        {
            if (SpeedRegMode != SpeedRegulatorMode.Manual)
            {
                return;
            }
            if (maxForceIncreasing) SpeedRegulatorMaxForceIncrease();
            if (maxForceDecreasing) SpeedRegulatorMaxForceDecrease();
        }

        public void Update(float elapsedClockSeconds, float AbsWheelSpeedMpS)
        {
            elapsedTime += elapsedClockSeconds;
            if (maxForceIncreasing) SpeedRegulatorMaxForceIncrease();
            if (maxForceDecreasing) SpeedRegulatorMaxForceDecrease();
            if (SpeedRegMode == SpeedRegulatorMode.Manual)
            {
                return;
            }

            if (absMaxForceN == 0) absMaxForceN = Locomotive.MaxForceN;

            if (selectedSpeedIncreasing) SpeedRegulatorSelectedSpeedIncrease();
            if (SelectedSpeedDecreasing) SpeedRegulatorSelectedSpeedDecrease();

            if (Locomotive.DynamicBrakePercent > 0)
                if (Locomotive.DynamicBrakePercent > 100)
                    Locomotive.DynamicBrakePercent = 100;
                ForceThrottleAndDynamicBrake = Locomotive.DynamicBrakePercent;

            UpdateMotiveForce(elapsedClockSeconds, AbsWheelSpeedMpS);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(this.applyingPneumaticBrake);
            outf.Write(this.Battery);
            outf.Write(this.brakeIncreasing);
            outf.Write(this.clockTime);
            outf.Write(this.controllerTime);
            outf.Write(this.CurrentSelectedSpeedMpS);
            outf.Write(this.currentThrottlePercent);
            outf.Write(this.dynamicBrakeSetToZero);
            outf.Write(this.fromAcceleration);
            outf.Write(this.maxForceDecreasing);
            outf.Write(this.maxForceIncreasing);
            outf.Write(this.maxForceN);
            outf.Write(this.nextSelectedSpeedMps);
            outf.Write(this.restrictedRegionTravelledDistance);
            outf.Write(this.RestrictedSpeedActive);
            outf.Write(this.SelectedMaxAccelerationPercent);
            outf.Write(this.SelectedMaxAccelerationStep);
            outf.Write(this.SelectedNumberOfAxles);
            outf.Write(this.SelectedSpeedMpS);
            outf.Write((int)this.SpeedRegMode);
            outf.Write((int)this.SpeedSelMode);
            outf.Write(this.throttleIsZero);
            outf.Write(this.trainBrakePercent);
            outf.Write(this.TrainLengthMeters);
        }

        public void Restore(BinaryReader inf)
        {
            applyingPneumaticBrake = inf.ReadBoolean();
            Battery = inf.ReadBoolean();
            brakeIncreasing = inf.ReadBoolean();
            clockTime = inf.ReadDouble();
            controllerTime = inf.ReadSingle();
            CurrentSelectedSpeedMpS = inf.ReadSingle();
            currentThrottlePercent = inf.ReadSingle();
            dynamicBrakeSetToZero = inf.ReadBoolean();
            fromAcceleration = inf.ReadSingle();
            maxForceDecreasing = inf.ReadBoolean();
            maxForceIncreasing = inf.ReadBoolean();
            maxForceN = inf.ReadSingle();
            nextSelectedSpeedMps = inf.ReadSingle();
            restrictedRegionTravelledDistance = inf.ReadSingle();
            RestrictedSpeedActive = inf.ReadBoolean();
            SelectedMaxAccelerationPercent = inf.ReadSingle();
            SelectedMaxAccelerationStep = inf.ReadSingle();
            SelectedNumberOfAxles = inf.ReadInt32();
            SelectedSpeedMpS = inf.ReadSingle();
            int fSpeedRegMode = inf.ReadInt32();
            SpeedRegMode = (SpeedRegulatorMode)fSpeedRegMode;
            int fSpeedSelMode = inf.ReadInt32();
            SpeedSelMode = (SpeedSelectorMode)fSpeedSelMode;
            throttleIsZero = inf.ReadBoolean();
            trainBrakePercent = inf.ReadSingle();
            TrainLengthMeters = inf.ReadInt32();
        }

        public void SpeedRegulatorModeIncrease()
        {
            if (!Locomotive.IsPlayerTrain) return;
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedRegulator);
            SpeedRegulatorMode previousMode = SpeedRegMode;
            if (!Equipped) return;
            if (SpeedRegMode == SpeedRegulatorMode.Testing) return;
            bool test = false;
            while (!test)
            {
                SpeedRegMode++;
                switch (SpeedRegMode)
                {
                    case SpeedRegulatorMode.Auto:
                        {
                            if (SpeedRegulatorOptions.Contains("regulatorauto")) test = true;
                            SelectedSpeedMpS = Locomotive.AbsSpeedMpS;
                            break;
                        }
                    case SpeedRegulatorMode.Testing: if (SpeedRegulatorOptions.Contains("regulatortest")) test = true; break;
                }
                if (!test && SpeedRegMode == SpeedRegulatorMode.Testing) // if we're here, then it means no higher option, return to previous state and get out
                {
                    SpeedRegMode = previousMode;
                    return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator mode changed to") + " " + Simulator.Catalog.GetString(SpeedRegMode.ToString()));
        }
        public void SpeedRegulatorModeDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedRegulator);
            if (!Equipped) return;
            if (SpeedRegMode == SpeedRegulatorMode.Manual) return;
            bool test = false;
            while (!test)
            {
                SpeedRegMode--;
                switch (SpeedRegMode)
                {
                    case SpeedRegulatorMode.Auto: if (SpeedRegulatorOptions.Contains("regulatorauto")) test = true; break;
                    case SpeedRegulatorMode.Manual:
                        {
                            Locomotive.ThrottleController.SetPercent(0);
                            currentThrottlePercent = 0;
                            if (SpeedRegulatorOptions.Contains("regulatormanual")) test = true;
                            SelectedSpeedMpS = 0;
                            foreach (MSTSLocomotive lc in playerNotDriveableTrainLocomotives)
                            {
                                lc.ThrottleOverriden = 0;
                                lc.IsAPartOfPlayerTrain = false; // in case we uncouple the loco later
                            }
                            break;
                        }
                }
                if (!test && SpeedRegMode == SpeedRegulatorMode.Manual)
                    return;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator mode changed to") + " " + Simulator.Catalog.GetString(SpeedRegMode.ToString()));
        }
        public void SpeedSelectorModeStartIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            if (!Equipped) return;
            if (SpeedSelMode == SpeedSelectorMode.Start) return;
            bool test = false;
            while (!test)
            {
                SpeedSelMode++;
                if (SpeedSelMode != SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority) Locomotive.SetEngineBrakePercent(0);
                switch (SpeedSelMode)
                {
                    case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                    case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                    case SpeedSelectorMode.Start: if (SpeedRegulatorOptions.Contains("selectorstart")) test = true; break;
                }
                if (!test && SpeedSelMode == SpeedSelectorMode.Start)
                    return;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed selector mode changed to") + " " + Simulator.Catalog.GetString(SpeedSelMode.ToString()));
        }
        public void SpeedSelectorModeStopIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            //Locomotive.Mirel.ResetVigilance();
            if (!Equipped) return;
            if (SpeedSelMode == SpeedSelectorMode.Start)
            {
                bool test = false;
                while (!test)
                {
                    SpeedSelMode--;
                    switch (SpeedSelMode)
                    {
                        case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                        case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                        case SpeedSelectorMode.Parking: if (SpeedRegulatorOptions.Contains("selectorparking")) test = true; break;
                    }
                    if (!test && SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority)
                        return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed selector mode changed to") + " " + Simulator.Catalog.GetString(SpeedSelMode.ToString()));
        }
        public void SpeedSelectorModeDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            SpeedSelectorMode previousMode = SpeedSelMode;
            if (!Equipped) return;
            if (SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority) return;
            bool test = false;
            while (!test)
            {
                SpeedSelMode--;
                switch (SpeedSelMode)
                {
                    case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                    case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                    case SpeedSelectorMode.Parking: if (SpeedRegulatorOptions.Contains("selectorparking")) test = true; break;
                }
                if (!test && SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority)
                {
                    SpeedSelMode = previousMode;
                    return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed selector mode changed to") + " " + Simulator.Catalog.GetString(SpeedSelMode.ToString()));
        }

        public void SetMaxForcePercent(float percent)
        {
            if (SelectedMaxAccelerationPercent == percent) return;
            SelectedMaxAccelerationPercent = percent;
            SelectedMaxAccelerationPercent = (float)Math.Round(SelectedMaxAccelerationStep, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration percent changed to") + " " + Simulator.Catalog.GetString(SelectedMaxAccelerationPercent.ToString()) + "%");
        }

        bool maxForceIncreasing = false;
        public void SpeedRegulatorMaxForceStartIncrease()
        {
            maxForceIncreasing = true;
            Update();
        }
        public void SpeedRegulatorMaxForceStopIncrease()
        {
            maxForceIncreasing = false;
        }
        protected void SpeedRegulatorMaxForceIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            if (MaxForceSetSingleStep) maxForceIncreasing = false;
            if (SelectedMaxAccelerationStep == 0.5f) SelectedMaxAccelerationStep = 0;
            if (!Equipped) return;
            if (SpeedRegulatorMaxForcePercentUnits)
            {
                if (SelectedMaxAccelerationPercent == 100)
                    return;
                SelectedMaxAccelerationPercent += 1f;
                SelectedMaxAccelerationPercent = (float)Math.Round(SelectedMaxAccelerationStep, 0);
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration percent changed to") + " " + Simulator.Catalog.GetString(SelectedMaxAccelerationPercent.ToString()) + "%");
            }
            else
            {
                if (SelectedMaxAccelerationStep == SpeedRegulatorMaxForceSteps)
                    return;
                SelectedMaxAccelerationStep++;
                SelectedMaxAccelerationStep = (float)Math.Round(SelectedMaxAccelerationStep, 0);
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration changed to") + " " + Simulator.Catalog.GetString(SelectedMaxAccelerationStep.ToString()));
            }
        }

        protected bool maxForceDecreasing = false;
        public void SpeedRegulatorMaxForceStartDecrease()
        {
            maxForceDecreasing = true;
            Update();
        }
        public void SpeedRegulatorMaxForceStopDecrease()
        {
            maxForceDecreasing = false;
        }
        protected void SpeedRegulatorMaxForceDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            if (MaxForceSetSingleStep) maxForceDecreasing = false;
            if (!Equipped) return;
            if (DisableZeroForceStep)
            {
                if (SelectedMaxAccelerationStep <= 1) return;
            }
            else
            {
                if (SelectedMaxAccelerationStep <= 0) return;
            }
            SelectedMaxAccelerationStep--;
            SelectedMaxAccelerationStep = (float)Math.Round(SelectedMaxAccelerationStep, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration changed to") + " " + Simulator.Catalog.GetString(SelectedMaxAccelerationStep.ToString()));
        }

        protected bool selectedSpeedIncreasing = false;
        public void SpeedRegulatorSelectedSpeedStartIncrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    if (!mpc.StateChanged)
                    {
                        mpc.StateChanged = true;
                        if (SpeedRegMode != SpeedRegulatorMode.Auto && ForceRegulatorAutoWhenNonZeroSpeedSelected)
                        {
                            SpeedRegMode = SpeedRegulatorMode.Auto;
                        }
                        mpc.DoMovement(Controllers.MultiPositionController.Movement.Forward);
                        return;
                    }
                }
            }
            if (SpeedRegMode != SpeedRegulatorMode.Auto && ForceRegulatorAutoWhenNonZeroSpeedSelected)
            {
                SpeedRegMode = SpeedRegulatorMode.Auto;
            }
            if (UseThrottleAsSpeedSelector)
                selectedSpeedIncreasing = true;
            else
                SpeedSelectorModeStartIncrease();
        }
        public void SpeedRegulatorSelectedSpeedStopIncrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    mpc.StateChanged = false;
                    mpc.DoMovement(Controllers.MultiPositionController.Movement.Neutral);
                    return;
                }
            }
            if (UseThrottleAsSpeedSelector)
                selectedSpeedIncreasing = false;
            else
                SpeedSelectorModeStopIncrease();
        }

        protected double selectedSpeedLeverHoldTime = 0;
        public void SpeedRegulatorSelectedSpeedIncrease()
        {
            if (!Equipped) return;

            if (selectedSpeedLeverHoldTime + SpeedSelectorStepTimeSeconds > elapsedTime)
                return;
            selectedSpeedLeverHoldTime = elapsedTime;

            SelectedSpeedMpS += SpeedRegulatorNominalSpeedStepMpS;
            if (SelectedSpeedMpS > Locomotive.MaxSpeedMpS)
                SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Selected speed changed to ") + Math.Round(MpS.FromMpS(SelectedSpeedMpS, true), 0, MidpointRounding.AwayFromZero).ToString() + " km/h");
        }

        public bool SelectedSpeedDecreasing = false;
        public void SpeedRegulatorSelectedSpeedStartDecrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    if (!mpc.StateChanged)
                    {
                        mpc.StateChanged = true;
                        mpc.DoMovement(Controllers.MultiPositionController.Movement.Aft);
                        return;
                    }
                }
            }
            if (UseThrottleAsSpeedSelector)
                SelectedSpeedDecreasing = true;
            else
                SpeedSelectorModeDecrease();
        }
        public void SpeedRegulatorSelectedSpeedStopDecrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    mpc.StateChanged = false;
                    mpc.DoMovement(Controllers.MultiPositionController.Movement.Neutral);
                    return;
                }
            }
            SelectedSpeedDecreasing = false;
        }
        public void SpeedRegulatorSelectedSpeedDecrease()
        {
            if (!Equipped) return;

            if (selectedSpeedLeverHoldTime + SpeedSelectorStepTimeSeconds > elapsedTime)
                return;
            selectedSpeedLeverHoldTime = elapsedTime;

            SelectedSpeedMpS -= SpeedRegulatorNominalSpeedStepMpS;
            if (SelectedSpeedMpS < 0)
                SelectedSpeedMpS = 0f;
            if (SpeedRegMode == SpeedRegulatorMode.Auto && ForceRegulatorAutoWhenNonZeroSpeedSelected && SelectedSpeedMpS == 0)
            {
                // return back to manual, clear all we have controlled before and let the driver to set up new stuff
                SpeedRegMode = SpeedRegulatorMode.Manual;
                Locomotive.ThrottleController.SetPercent(0);
                Locomotive.SetDynamicBrakePercent(0);
                Locomotive.DynamicBrakeChangeActiveState(false);
            }
            if (SelectedSpeedMpS == 0)
                SelectedSpeedDecreasing = false;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Selected speed changed to ") + Math.Round(MpS.FromMpS(SelectedSpeedMpS, true), 0, MidpointRounding.AwayFromZero).ToString() + " km/h");
        }
        public void NumerOfAxlesIncrease()
        {
            NumerOfAxlesIncrease(1);
        }
        public void NumerOfAxlesIncrease(int ByAmount)
        {
            SelectedNumberOfAxles += ByAmount;
            trainLength = SelectedNumberOfAxles * 6.6f;
            TrainLengthMeters = (int)Math.Round(trainLength + 0.5, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Number of axles increased to ") + SelectedNumberOfAxles.ToString());
        }
        public void NumberOfAxlesDecrease()
        {
            NumberOfAxlesDecrease(1);
        }
        public void NumberOfAxlesDecrease(int ByAmount)
        {
            if ((SelectedNumberOfAxles - ByAmount) < 1) return;
            SelectedNumberOfAxles -= ByAmount;
            trainLength = SelectedNumberOfAxles * 6.6f;
            TrainLengthMeters = (int)Math.Round(trainLength + 0.5, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Number of axles decreased to ") + SelectedNumberOfAxles.ToString());
        }
        public void ActivateRestrictedSpeedZone()
        {
            RemainingTrainLengthToPassRestrictedZone = TrainLengthMeters;
            if (!RestrictedSpeedActive)
            {
                restrictedRegionTravelledDistance = Simulator.PlayerLocomotive.Train.DistanceTravelledM;
                CurrentSelectedSpeedMpS = SelectedSpeedMpS;
                RestrictedSpeedActive = true;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed restricted zone active."));
        }

        public virtual void CheckRestrictedSpeedZone()
        {
            RemainingTrainLengthToPassRestrictedZone = (int)Math.Round((Simulator.PlayerLocomotive.Train.DistanceTravelledM - restrictedRegionTravelledDistance));
            if (RemainingTrainLengthToPassRestrictedZone < 0) RemainingTrainLengthToPassRestrictedZone = 0;
            if ((Simulator.PlayerLocomotive.Train.DistanceTravelledM - restrictedRegionTravelledDistance) >= trainLength)
            {
                RestrictedSpeedActive = false;
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed restricted zone off."));
                Locomotive.SignalEvent(Common.Event.Alert);
            }
        }

        public void SetSpeed(float Speed)
        {
            if (!Equipped) return;
            if (SpeedRegMode == SpeedRegulatorMode.Manual && ForceRegulatorAutoWhenNonZeroSpeedSelected)
                SpeedRegMode = SpeedRegulatorMode.Auto;
            if (SpeedRegMode == SpeedRegulatorMode.Manual)
                return;
            Locomotive.SignalEvent(Common.Event.Alert1);
            SelectedSpeedMpS = SpeedIsMph ? MpS.FromMpH(Speed) : MpS.FromKpH(Speed);
            if (SelectedSpeedMpS > Locomotive.MaxSpeedMpS)
                SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Selected speed set to ") + Speed.ToString() + (SpeedIsMph? "mph" : "kmh"));
        }

        protected List<MSTSLocomotive> playerNotDriveableTrainLocomotives = new List<MSTSLocomotive>();
        protected float _AccelerationMpSS = 0;
        protected bool throttleIsZero = false;
        protected bool brakeIncreasing = false;
        protected float controllerTime = 0;
        protected float fromAcceleration = 0;
        protected bool applyingPneumaticBrake = false;
        protected bool firstIteration = true;
        protected float previousMotiveForce = 0;
        protected float addPowerTimeCount = 0;
        public float controllerVolts = 0;
        protected float throttleChangeTime = 0;
        protected bool breakout = false;
        protected  float timeFromEngineMoved = 0;
        protected bool reducingForce = false;
        protected bool canAddForce = true;
        protected List<float> concurrentAccelerationList = new List<float>();
        public float TrainElevation = 0;
        protected float skidSpeedDegratation = 0;
        protected float previousAccelerationDemand = 0;
        public bool TrainBrakePriority = false;
        public bool WasBraking = false;
        public bool WasForceReset = true;

        protected virtual void UpdateMotiveForce(float elapsedClockSeconds, float AbsWheelSpeedMpS)
        {
            if (!Locomotive.PowerOn)
                ForceThrottleAndDynamicBrake = 0;
            float speedDiff = AbsWheelSpeedMpS - Locomotive.AbsSpeedMpS;
            foreach (MSTSLocomotive loco in playerNotDriveableTrainLocomotives)
            {
                if ((loco.AbsWheelSpeedMpS - loco.AbsSpeedMpS) > speedDiff)
                    speedDiff = loco.AbsWheelSpeedMpS - loco.AbsSpeedMpS;
            }
            float newThrotte = 0;
            // calculate new max force if MaxPowerThreshold is set
            if (MaxPowerThreshold > 0)
            {
                float currentSpeed = SpeedIsMph ? MpS.ToMpH(AbsWheelSpeedMpS) : MpS.ToKpH(AbsWheelSpeedMpS);
                float percentComplete = (int)Math.Round((double)(100 * currentSpeed) / MaxPowerThreshold);
                if (percentComplete > 100)
                    percentComplete = 100;
                newThrotte = percentComplete;
            }

            int count = 0;
            /*TrainElevation = 0;
            foreach (TrainCar tc in Locomotive.Train.Cars)
            {
                count++;
                TrainElevation += tc.Flipped ? tc.CurrentElevationPercent : -tc.CurrentElevationPercent;
            }
            TrainElevation = TrainElevation / count;*/

            if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Release ||
                Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Neutral)
                TrainBrakePriority = false;

            if (TrainBrakePriority || DynamicBrakePriority)
            {
                WasForceReset = false;
                WasBraking = true;
            }

            if ((SpeedSelMode == SpeedSelectorMode.On || SpeedSelMode == SpeedSelectorMode.Start) && !TrainBrakePriority)
            {
                canAddForce = true;
            }
            else
            {
                canAddForce = false;
                timeFromEngineMoved = 0;
                reducingForce = true;
                Locomotive.TractiveForceN = 0;
                if (TrainBrakePriority)
                {
                    if (SpeedSelMode == SpeedSelectorMode.Parking)
                        if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                            Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                    if (Locomotive.DynamicBrakePercent > 0 && SelectedSpeedMpS > 0)
                        Locomotive.SetDynamicBrakePercent(0);
                    controllerVolts = 0;
                    return;
                }
            }


            if ((SelectedMaxAccelerationStep == 0 && SelectedMaxAccelerationPercent == 0) || SpeedSelMode == SpeedSelectorMode.Start)
            {
                WasForceReset = true;
                WasBraking = false;
            }

            if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
            {
                WasBraking = false;
                Locomotive.SetThrottlePercent(0);
            }
            if (WasBraking)
                if (SpeedSelMode == SpeedSelectorMode.Start)
                    WasBraking = false;
            if (ResetForceAfterAnyBraking && WasBraking && (SelectedMaxAccelerationStep > 0 || SelectedMaxAccelerationPercent > 0) && !DynamicBrakePriority)
            {
                Locomotive.SetThrottlePercent(0);
                controllerVolts = 0;
                maxForceN = 0;
                if (SpeedSelMode == SpeedSelectorMode.Parking)
                    if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                        Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                return;
            }

            if (ResetForceAfterAnyBraking && !WasForceReset && !DynamicBrakePriority)
            {
                Locomotive.SetThrottlePercent(0);
                controllerVolts = 0;
                maxForceN = 0;
                if (SpeedSelMode == SpeedSelectorMode.Parking)
                    if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                        Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                if (!DynamicBrakePriority)
                    return;
            }


             if (canAddForce)
            {
                if (Locomotive.AbsSpeedMpS == 0)
                {
                    timeFromEngineMoved = 0;
                    reducingForce = true;
                }
                else if (reducingForce)
                {
                    
                    timeFromEngineMoved += elapsedClockSeconds;
                    float timeToReduce = Locomotive.SelectedTrainType == MSTSLocomotive.TrainType.Pax ? PowerReductionDelayPaxTrain : PowerReductionDelayCargoTrain;
                    if (timeFromEngineMoved > timeToReduce)
                        reducingForce = false;
                }
            }
            if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 4.98)
            {
                canAddForce = false;
                reducingForce = true;
                timeFromEngineMoved = 0;
                maxForceN = 0;
                if (controllerVolts > 0)
                    controllerVolts = 0;
                Ampers = 0;
                Locomotive.ThrottleController.SetPercent(0);
                return;
            }
            else if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.7)
            {
                canAddForce = true;
            }

            if (SpeedRegulatorOptions.Contains("engageforceonnonzerospeed") && SelectedSpeedMpS > 0)
            {
                SpeedSelMode = SpeedSelectorMode.On;
                SpeedRegMode = SpeedRegulatorMode.Auto;
                SkipThrottleDisplay = true;
                reducingForce = false;
            }
            if (SpeedRegulatorOptions.Contains("engageforceonnonzerospeed") && SelectedSpeedMpS == 0)
            {
                if (playerNotDriveableTrainLocomotives.Count > 0) // update any other than the player's locomotive in the consist throttles to percentage of the current force and the max force
                {
                    foreach (MSTSLocomotive lc in playerNotDriveableTrainLocomotives)
                    {
                        if (UseThrottle)
                        {
                            lc.SetThrottlePercent(0);
                        }
                        else
                        {
                            lc.IsAPartOfPlayerTrain = true;
                            lc.ThrottleOverriden = 0;
                        }
                    }
                }
                Locomotive.TractiveForceN = Locomotive.MotiveForceN = 0;
                Locomotive.SetThrottlePercent(0);
                return;
            }

            float t = 0;
            if (SpeedRegMode == SpeedRegulatorMode.Manual) DynamicBrakePriority = false;

            if (RestrictedSpeedActive)
                CheckRestrictedSpeedZone();
            if (DynamicBrakePriority)
            {
                controllerVolts = 0;
                if (Locomotive.TractiveForceN > 0)
                {
                    float force = Locomotive.TractiveForceN - 1000;
                    if (force < 0)
                    {
                        force = 0;
                        Locomotive.SetThrottlePercent(0);
                    }
                    Locomotive.TractiveForceN = force;
                    ForceThrottleAndDynamicBrake = ((Locomotive.MaxForceN - (Locomotive.MaxForceN - force)) / Locomotive.MaxForceN) * 100;
                }
                if (Locomotive.TractiveForceN == 0)
                    ForceThrottleAndDynamicBrake = -Locomotive.DynamicBrakePercent;

                return;
            }

           if (firstIteration) // if this is exetuted the first time, let's check all other than player engines in the consist, and record them for further throttle manipulation
            {
                if (SelectedNumberOfAxles == 0) SelectedNumberOfAxles = (int)(Locomotive.Train.Length / 6.6f) + 4; // also set the axles, for better delta computing, if user omits to set it
                foreach (TrainCar tc in Locomotive.Train.Cars)
                {
                    if (tc.GetType() == typeof(MSTSLocomotive) || tc.GetType() == typeof(MSTSDieselLocomotive) || tc.GetType() == typeof(MSTSElectricLocomotive))
                    {
                        if (tc != Locomotive)
                        {
                            try
                            {
                                playerNotDriveableTrainLocomotives.Add((MSTSLocomotive)tc);
                            }
                            catch { }
                        }
                    }
                }
                firstIteration = false;
            }

            if (SelectedMaxAccelerationStep == 0) // no effort, no throttle (i.e. for reverser change, etc) and return
            {
                Locomotive.SetThrottlePercent(0);
                if (Locomotive.DynamicBrakePercent > 0)
                Locomotive.SetDynamicBrakePercent(0);
            }

            if (SpeedRegMode == SpeedRegulatorMode.Auto)
            {
                if (SpeedSelMode == SpeedSelectorMode.Parking && !Locomotive.EngineBrakePriority)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        if (AbsWheelSpeedMpS == 0)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                    if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                    throttleIsZero = true;

                    if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                        Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                }
                else if (SpeedSelMode == SpeedSelectorMode.Neutral || SpeedSelMode < SpeedSelectorMode.Start && !SpeedRegulatorOptions.Contains("startfromzero") && AbsWheelSpeedMpS < SafeSpeedForAutomaticOperationMpS)
                {
                    if (controllerVolts > 0)
                    {
                        float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                        step *= elapsedClockSeconds;
                        controllerVolts -= step;
                        if (controllerVolts < 0) controllerVolts = 0;
                        if (controllerVolts > 0 && controllerVolts < 0.1) controllerVolts = 0;
                    }

                    float delta = 0;
                    if (!RestrictedSpeedActive)
                        delta = SelectedSpeedMpS - AbsWheelSpeedMpS;
                    else
                        delta = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;

                    if (delta > 0)
                    {
                        if (controllerVolts < -0.1)
                        {
                            float step = 100 / Locomotive.ThrottleFullRangeDecreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            controllerVolts += step;
                        }
                        else if (controllerVolts > 0.1)
                        {

                            float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            controllerVolts -= step;
                        }
                        else
                        {
                            controllerVolts = 0;
                        }
                    }

                    if (delta < 0) // start braking
                    {
                        if (maxForceN > 0)
                        {
                            if (controllerVolts > 0)
                            {
                                float step = 100 / Locomotive.ThrottleFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts -= step;
                            }
                        }
                        else
                        {
                            if (Locomotive.DynamicBrakeAvailable)
                            {
                                delta = 0;
                                if (!RestrictedSpeedActive)
                                    delta = SelectedSpeedMpS - AbsWheelSpeedMpS;
                                else
                                    delta = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;

                                AccelerationDemandMpSS = (float)-Math.Sqrt(-StartReducingSpeedDelta * delta) * 5;
                                if (maxForceN > 0)
                                {
                                    if (controllerVolts > 0)
                                    {
                                        float step = 100 / Locomotive.ThrottleFullRangeDecreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        controllerVolts -= step;
                                    }
                                }
                                if (maxForceN == 0)
                                {
                                    if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                                    if (Locomotive.DynamicBrakePercent < -(AccelerationDemandMpSS * 100) && AccelerationDemandMpSS < -0.05f)
                                    {
                                        if (DynamicBrakeIsSelectedForceDependant && SpeedRegulatorMaxForceSteps == 100)
                                        {
                                            if (controllerVolts > -SelectedMaxAccelerationStep)
                                            {
                                                float step = 100 / Locomotive.DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts -= step;
                                            }
                                        }
                                        else
                                        {
                                            if (controllerVolts > -100 && (DynamicBrakeMaxForceAtSelectorStep == 0 || SelectedMaxAccelerationStep >= DynamicBrakeMaxForceAtSelectorStep))
                                            {
                                                float step = 100 / Locomotive.DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts -= step;
                                            }
                                            float maxVolts = -100;
                                            if (DynamicBrakeMaxForceAtSelectorStep != 0)
                                            {
                                                if (SelectedMaxAccelerationStep < DynamicBrakeMaxForceAtSelectorStep)
                                                {
                                                    float difference = 100 / DynamicBrakeMaxForceAtSelectorStep;
                                                    maxVolts = -difference * SelectedMaxAccelerationStep;
                                                }
                                            }
                                            if (controllerVolts < -100)
                                                controllerVolts = -100;
                                            if (controllerVolts < maxVolts)
                                            {
                                                float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts += step;
                                            }
                                        }
                                    }
                                    if (Locomotive.DynamicBrakePercent > -((AccelerationDemandMpSS - 0.05f) * 100))
                                    {
                                        if (controllerVolts < 0)
                                        {
                                            float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                                            step *= elapsedClockSeconds;
                                            controllerVolts += step;
                                        }
                                    }
                                }
                            }
                            else // use TrainBrake
                            {
                                if (delta > -0.1)
                                {
                                    if (!UseThrottle)
                                        Locomotive.ThrottleController.SetPercent(100);
                                    throttleIsZero = false;
                                    maxForceN = 0;
                                }
                                else if (delta > -1)
                                {
                                    if (!UseThrottle)
                                        Locomotive.ThrottleController.SetPercent(0);
                                    throttleIsZero = true;

                                    brakePercent = 10 + (-delta * 10);
                                }
                                else
                                {
                                    Locomotive.TractiveForceN = 0;
                                    if (!UseThrottle)
                                        Locomotive.ThrottleController.SetPercent(0);
                                    throttleIsZero = true;

                                    if (_AccelerationMpSS > -MaxDecelerationMpSS)
                                        brakePercent += 0.5f;
                                    else if (_AccelerationMpSS < -MaxDecelerationMpSS)
                                        brakePercent -= 1;
                                    if (brakePercent > 100)
                                        brakePercent = 100;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakeAvailable)
                        {
                            if (Locomotive.DynamicBrakePercent > 0)
                            {
                                if (controllerVolts < 0)
                                {
                                    float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts += step;
                                }
                            }
                        }
                    }
                }

                if ((AbsWheelSpeedMpS > SafeSpeedForAutomaticOperationMpS || SpeedSelMode == SpeedSelectorMode.Start || SpeedRegulatorOptions.Contains("startfromzero")) && (SpeedSelMode != SpeedSelectorMode.Neutral && SpeedSelMode != SpeedSelectorMode.Parking))
                {
                    float delta = 0;
                    if (!RestrictedSpeedActive)
                        delta = SelectedSpeedMpS - AbsWheelSpeedMpS;
                    else
                        delta = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;
                    float coeff = 1;
                    float speed = SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS);
                    if (speed > 100)
                    {
                        coeff = (speed / 100) * 1.2f;
                    }
                    else
                    {
                        coeff = 1;
                    }
                    float tempAccDemand = AccelerationDemandMpSS;
                    AccelerationDemandMpSS = (float)Math.Sqrt((StartReducingSpeedDelta) * coeff * (delta));
                    if (float.IsNaN(AccelerationDemandMpSS))
                    {
                        AccelerationDemandMpSS = tempAccDemand;
                    }
                    if (delta > 0.0f && Locomotive.DynamicBrakePercent < 1)
                    {
                        if (Locomotive.DynamicBrakePercent > 0)
                        {
                            float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            controllerVolts += step;
                        }
                        if (Locomotive.DynamicBrakePercent < 1 && Locomotive.DynamicBrake)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                    else // start braking
                    {
                        if (maxForceN > 0)
                        {
                            if (controllerVolts > 0)
                            {
                                float step = 100 / Locomotive.ThrottleFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts -= step;
                            }
                        }
                        else
                        {
                            if (Locomotive.DynamicBrakeAvailable)
                            {
                                delta = 0;
                                if (!RestrictedSpeedActive)
                                    delta = SelectedSpeedMpS - AbsWheelSpeedMpS;
                                else
                                    delta = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;

                                AccelerationDemandMpSS = (float)-Math.Sqrt(-StartReducingSpeedDelta * delta) * 5;
                                if (maxForceN > 0)
                                {
                                    if (controllerVolts > 0)
                                    {
                                        float step = 100 / Locomotive.ThrottleFullRangeDecreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        controllerVolts -= step;
                                    }
                                }
                                if (maxForceN == 0)
                                {
                                    if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                                    if (Locomotive.DynamicBrakePercent < -(AccelerationDemandMpSS * 100) && AccelerationDemandMpSS < -0.05f)
                                    {
                                        if (DynamicBrakeIsSelectedForceDependant && SpeedRegulatorMaxForceSteps == 100)
                                        {
                                            if (controllerVolts > -SelectedMaxAccelerationStep)
                                            {
                                                float step = 100 / Locomotive.DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts -= step;
                                            }
                                        }
                                        else
                                        {
                                            if (controllerVolts > -100 && (DynamicBrakeMaxForceAtSelectorStep == 0 || SelectedMaxAccelerationStep >= DynamicBrakeMaxForceAtSelectorStep))
                                            {
                                                float step = 100 / Locomotive.DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts -= step;
                                            }
                                            float maxVolts = -100;
                                            if (DynamicBrakeMaxForceAtSelectorStep != 0)
                                            {
                                                if (SelectedMaxAccelerationStep < DynamicBrakeMaxForceAtSelectorStep)
                                                {
                                                    float difference = 100 / DynamicBrakeMaxForceAtSelectorStep;
                                                    maxVolts = -difference * SelectedMaxAccelerationStep;
                                                }
                                            }
                                            if (controllerVolts < -100)
                                                controllerVolts = -100;
                                            if (controllerVolts < maxVolts)
                                            {
                                                float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                controllerVolts += step;
                                            }
                                        }
                                    }
                                    if (Locomotive.DynamicBrakePercent > -((AccelerationDemandMpSS - 0.05f) * 100))
                                    {
                                        if (controllerVolts < 0)
                                        {
                                            float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                                            step *= elapsedClockSeconds;
                                            controllerVolts += step;
                                        }
                                    }
                                }
                            }
                            else // use TrainBrake
                            {
                                if (delta > -0.1)
                                {
                                    if (!UseThrottle)
                                        Locomotive.ThrottleController.SetPercent(100);
                                    throttleIsZero = false;
                                    maxForceN = 0;
                                }
                                else if (delta > -1)
                                {
                                    if (!UseThrottle)
                                        Locomotive.ThrottleController.SetPercent(0);
                                    throttleIsZero = true;

                                    brakePercent = 10 + (-delta * 10);
                                }
                                else
                                {
                                    Locomotive.TractiveForceN = 0;
                                    if (!UseThrottle)
                                        Locomotive.ThrottleController.SetPercent(0);
                                    throttleIsZero = true;

                                    if (_AccelerationMpSS > -MaxDecelerationMpSS)
                                        brakePercent += 0.5f;
                                    else if (_AccelerationMpSS < -MaxDecelerationMpSS)
                                        brakePercent -= 1;
                                    if (brakePercent > 100)
                                        brakePercent = 100;
                                }
                            }
                        }
                    }

                    if ((SpeedSelMode == SpeedSelectorMode.On || SpeedSelMode == SpeedSelectorMode.Start) && delta > 0)
                    {
                        if (Locomotive.DynamicBrakePercent > 0)
                        {
                            if (controllerVolts <= 0)
                            {
                                float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                if (step > (AccelerationDemandMpSS - Locomotive.AccelerationMpSS) * 2)
                                    step = (AccelerationDemandMpSS - Locomotive.AccelerationMpSS) * 2;
                                controllerVolts += step;
                            }
                        }
                        else
                        {
                            if (!UseThrottle)
                            {
                                if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                                    Locomotive.ThrottleController.SetPercent(0);
                                else
                                    Locomotive.ThrottleController.SetPercent(100);
                            }
                            throttleIsZero = false;
                        }
                    }
                    float a = 0;
                    if (Locomotive.PowerOn && Locomotive.Direction != Direction.N)
                    {
                        if (Locomotive.DynamicBrakePercent < 0)
                        {
                            if (Locomotive.AccelerationMpSS < AccelerationDemandMpSS)
                            {
                                if (ForceStepsThrottleTable.Count > 0)
                                {
                                    t = ForceStepsThrottleTable[(int)SelectedMaxAccelerationStep - 1];
                                    if (AccelerationTable.Count > 0)
                                        a = AccelerationTable[(int)SelectedMaxAccelerationStep - 1];
                                }
                                else
                                    t = SelectedMaxAccelerationStep;
                                if (t < newThrotte)
                                    t = newThrotte;
                                t /= 100;
                            }
                        }
                        if (reducingForce)
                        {
                            if (t > PowerReductionValue / 100)
                                t = PowerReductionValue / 100;
                        }
                        float demandedVolts = t * 100;
                        float current = maxForceN / Locomotive.MaxForceN * 1400;// Locomotive.MaxCurrentA;
                        if (!RestrictedSpeedActive)
                            delta = SelectedSpeedMpS - Locomotive.AbsSpeedMpS;
                        else
                            delta = CurrentSelectedSpeedMpS - Locomotive.AbsSpeedMpS;

                        if (current < PowerBreakoutAmpers && delta < PowerResumeSpeedDelta / 2)
                            breakout = true;
                        if (breakout && delta > PowerResumeSpeedDelta)
                            breakout = false;
                        if (UseThrottle) // not valid for diesel engines.
                            breakout = false;
                        if (!RestrictedSpeedActive)
                            delta = SelectedSpeedMpS - AbsWheelSpeedMpS;
                        else
                            delta = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;

                        if ((controllerVolts != demandedVolts) && delta > 0)
                        {
                            if (a > 0 && (SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS)) > 5)
                            {
                                if (controllerVolts < demandedVolts && Locomotive.AccelerationMpSS < a - 0.02)
                                {
                                    float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts += step;
                                }
                            }
                            else
                            {
                                if (controllerVolts < demandedVolts && controllerVolts >= 0)
                                {
                                    float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    float accelDiff = AccelerationDemandMpSS - Locomotive.AccelerationMpSS;
                                    if (step / 10 > accelDiff)
                                        step = accelDiff * 10;
                                    controllerVolts += step;
                                }
                            }
                            if (a > 0 && (SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS)) > 5)
                            {
                                if (controllerVolts > demandedVolts && Locomotive.AccelerationMpSS > a + 0.02)
                                {
                                    float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                            else
                            {
                                if (controllerVolts - 0.2f > demandedVolts)
                                {
                                    float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                            if (controllerVolts > demandedVolts && delta < 0.8)
                            {
                                float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                controllerVolts -= step;
                            }
                        }
                        if (a > 0 && (SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS)) > 5)
                        {
                            if ((a != Locomotive.AccelerationMpSS) && delta > 0.8)
                            {
                                if (Locomotive.AccelerationMpSS < a + 0.02)
                                {
                                    float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts += step;
                                }
                                if (Locomotive.AccelerationMpSS > a - 0.02)
                                {
                                    float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    controllerVolts -= step;
                                }
                            }
                        }

                        if (UseThrottle)
                        {
                            if (controllerVolts > 0)
                                Locomotive.ThrottleController.SetPercent(controllerVolts);
                        }
                    }
                }
                else if (UseThrottle)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        float newValue = (Locomotive.ThrottlePercent - 1) / 100;
                        if (newValue < 0)
                            newValue = 0;
                        Locomotive.StartThrottleDecrease(newValue);
                    }
                }

                if (Locomotive.WheelSpeedMpS == 0 && controllerVolts < 0)
                    controllerVolts = 0;
                ForceThrottleAndDynamicBrake = controllerVolts;

                if (controllerVolts > 0)
                {
                    if (speedDiff > AntiWheelSpinSpeedDiffThreshold)
                    {
                        skidSpeedDegratation += 0.05f;
                    }
                    else if (skidSpeedDegratation > 0)
                    {
                        skidSpeedDegratation -= 0.1f;
                    }
                    if (speedDiff < AntiWheelSpinSpeedDiffThreshold - 0.05f)
                        skidSpeedDegratation = 0;
                    if (AntiWheelSpinEquipped)
                        controllerVolts -= skidSpeedDegratation;
                    if (breakout || Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 4.98)
                    {
                        maxForceN = 0;
                        controllerVolts = 0;
                        Ampers = 0;
                        if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100 && SpeedSelMode != SpeedSelectorMode.Parking && !UseThrottle)
                        {
                            if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                            {
                                Locomotive.ThrottleController.SetPercent(0);
                                throttleIsZero = true;
                            }
                            else
                            {
                                Locomotive.ThrottleController.SetPercent(100);
                                throttleIsZero = false;
                            }
                        }
                        if (Locomotive.DynamicBrakePercent > -1)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }

                        if (Locomotive.TractiveForceCurves != null && !UseThrottle)
                        {
                            maxForceN = Locomotive.TractiveForceCurves.Get(controllerVolts / 100, AbsWheelSpeedMpS) * (1 - Locomotive.PowerReduction);
                        }
                        else
                        {
                            if (Locomotive.TractiveForceCurves == null)
                                maxForceN = Locomotive.MaxForceN * (controllerVolts / 100);
                            else
                                maxForceN = Locomotive.TractiveForceCurves.Get(controllerVolts / 100, AbsWheelSpeedMpS) * (1 - Locomotive.PowerReduction);
                        }
                    }
                }
                else if (controllerVolts < 0)
                {
                    if (maxForceN > 0) maxForceN = 0;
                    if (Locomotive.ThrottlePercent > 0) Locomotive.ThrottleController.SetPercent(0);
                    if (Locomotive.DynamicBrakePercent <= 0)
                    {
                        string status = Locomotive.GetDynamicBrakeStatus();
                        Locomotive.DynamicBrakeChangeActiveState(true);
                    }
                    if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                    {
                        Locomotive.SetDynamicBrakePercent(0);
                        Locomotive.DynamicBrakePercent = 0;
                        controllerVolts = 0;
                    }
                    else
                    {
                        Locomotive.SetDynamicBrakePercent(-controllerVolts);
                        Locomotive.DynamicBrakePercent = -controllerVolts;
                    }
                }
                else if (controllerVolts == 0)
                {
                    if (!breakout)
                    {

                        /*if (Locomotive.MultiPositionController.controllerPosition == Controllers.MultiPositionController.ControllerPosition.DynamicBrakeIncrease || Locomotive.MultiPositionController.controllerPosition == Controllers.MultiPositionController.ControllerPosition.DynamicBrakeIncreaseFast)
                        {
                            controllerVolts = -Locomotive.DynamicBrakePercent;
                        }
                        else
                        {*/
                        if (maxForceN > 0) maxForceN = 0;
                        if (Locomotive.ThrottlePercent > 0 && !UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                        if (Locomotive.DynamicBrakePercent > -1)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                }

                if (!Locomotive.PowerOn) // || Locomotive.Mirel.NZ1 || Locomotive.Mirel.NZ2 || Locomotive.Mirel.NZ3 || Locomotive.Mirel.NZ4 || Locomotive.Mirel.NZ5)
                {
                    controllerVolts = 0;
                    Locomotive.ThrottleController.SetPercent(0);
                    if (Locomotive.DynamicBrakePercent > 0)
                        Locomotive.SetDynamicBrakePercent(0);
                    Locomotive.DynamicBrakeIntervention = -1;
                    maxForceN = 0;
                    ForceThrottleAndDynamicBrake = 0;
                    Ampers = 0;
                }
                else
                    ForceThrottleAndDynamicBrake = controllerVolts;
                if (Locomotive.extendedPhysics == null)
                {
                    Locomotive.MotiveForceN = maxForceN;
                    Locomotive.TractiveForceN = maxForceN;
                }
                if (controllerVolts > 0)
                {
                    Locomotive.ControllerVolts = controllerVolts / 10;
                }
            }

            if (playerNotDriveableTrainLocomotives.Count > 0) // update any other than the player's locomotive in the consist throttles to percentage of the current force and the max force
            {
                float locoPercent = Locomotive.MaxForceN - (Locomotive.MaxForceN - Locomotive.MotiveForceN);
                locoPercent = (locoPercent / Locomotive.MaxForceN) * 100;
                //Simulator.Confirmer.MSG(locoPercent.ToString());
                foreach (MSTSLocomotive lc in playerNotDriveableTrainLocomotives)
                {
                    if (Locomotive.PowerOn)
                    {
                        if (UseThrottle)
                        {
                            lc.SetThrottlePercent(Locomotive.ThrottlePercent);
                        }
                        else
                        {
                            lc.IsAPartOfPlayerTrain = true;
                            lc.ThrottleOverriden = locoPercent / 100;
                        }
                    }
                    else
                    {
                        if (UseThrottle)
                        {
                            lc.SetThrottlePercent(0);
                        }
                        else
                        {
                            lc.IsAPartOfPlayerTrain = true;
                            lc.ThrottleOverriden = 0;
                        }
                    }
                }
            }
        }

        private float previousSelectedSpeed = 0;
        public float GetDataOf(CabViewControl cvc)
        {
            float data = 0;
            switch (cvc.ControlType)
            { 
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MODE:
                    data = (float)SpeedSelMode;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_REGULATOR_MODE:
                    data = (float)SpeedRegMode;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MAXIMUM_ACCELERATION:
                    if (SpeedRegMode == SpeedRegulatorMode.Auto || MaxForceKeepSelectedStepWhenManualModeSet)
                    {
                        data = (SelectedMaxAccelerationStep - 1) * (float)cvc.MaxValue / 100;
                    }
                    else
                        data = 0;
                    break;
                case CABViewControlTypes.ORTS_RESTRICTED_SPEED_ZONE_ACTIVE:
                    data = RestrictedSpeedActive ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_UNITS:
                    data = SelectedNumberOfAxles % 10;
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_TENS:
                    data = (SelectedNumberOfAxles / 10) % 10;
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_HUNDREDS:
                    data = (SelectedNumberOfAxles / 100) % 10;
                    break;
                case CABViewControlTypes.ORTS_TRAIN_LENGTH_METERS:
                    data = TrainLengthMeters;
                    break;
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGHT_SPEED_RESTRICTED:
                    if (RemainingTrainLengthToPassRestrictedZone == 0)
                        data = 0;
                    else
                        data = TrainLengthMeters - RemainingTrainLengthToPassRestrictedZone;
                    break;
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGTH_PERCENT:
                    if (SpeedRegMode != CruiseControl.SpeedRegulatorMode.Auto)
                    {
                        data = 0;
                        break;
                    }
                    if (TrainLengthMeters > 0 && RemainingTrainLengthToPassRestrictedZone > 0)
                    {
                        data = (((float)TrainLengthMeters - (float)RemainingTrainLengthToPassRestrictedZone) / (float)TrainLengthMeters) * 100;
                    }
                    break;
                case CABViewControlTypes.ORTS_MOTIVE_FORCE:
                    data = Locomotive.FilteredMotiveForceN;
                    break;
                case CABViewControlTypes.ORTS_MOTIVE_FORCE_KILONEWTON:
                    if (Locomotive.FilteredMotiveForceN > Locomotive.DynamicBrakeForceN)
                        data = (float)Math.Round(Locomotive.FilteredMotiveForceN / 1000, 0);
                    else if (Locomotive.DynamicBrakeForceN > 0)
                        data = -(float)Math.Round(Locomotive.DynamicBrakeForceN / 1000, 0);
                    break;
                case CABViewControlTypes.ORTS_MAXIMUM_FORCE:
                    data = Locomotive.MaxForceN;
                    break;
                case CABViewControlTypes.ORTS_FORCE_IN_PERCENT_THROTTLE_AND_DYNAMIC_BRAKE:
                    if (SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto)
                    {
                        data = ForceThrottleAndDynamicBrake;
                        if (Locomotive.DynamicBrakePercent > 0 && data > -Locomotive.DynamicBrakePercent) data = -Locomotive.DynamicBrakePercent;
                    }
                    else
                    {
                        data = Locomotive.ControllerVolts * 10;
                    }
                    break;
                case CABViewControlTypes.ORTS_TRAIN_TYPE_PAX_OR_CARGO:
                    data = (int)Locomotive.SelectedTrainType;
                    break;
                case CABViewControlTypes.ORTS_CONTROLLER_VOLTAGE:
                    data = controllerVolts;
                    break;
                case CABViewControlTypes.ORTS_ACCELERATION_IN_TIME:
                    {
                        data = Locomotive.AccelerationBits;
                        break;
                    }
                case CABViewControlTypes.ORTS_ODOMETER:
                    data = cvc.Units == CABViewControlUnits.KILOMETRES ? float.Parse(Math.Round(Locomotive.OdometerM / 1000, 0).ToString()) : Locomotive.OdometerM;
                    break;
                case CABViewControlTypes.ORTS_CC_SELECT_SPEED:
                    data = Locomotive.SelectingSpeedPressed ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_CC_SPEED_0:
                    {
                        data = Locomotive.Speed0Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_10:
                    {
                        data = Locomotive.Speed10Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_20:
                    {
                        data = Locomotive.Speed20Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_30:
                    {
                        data = Locomotive.Speed30Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_40:
                    {
                        data = Locomotive.Speed40Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_50:
                    {
                        data = Locomotive.Speed50Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_60:
                    {
                        data = Locomotive.Speed60Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_70:
                    {
                        data = Locomotive.Speed70Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_80:
                    {
                        data = Locomotive.Speed80Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_90:
                    {
                        data = Locomotive.Speed90Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_100:
                    {
                        data = Locomotive.Speed100Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_110:
                    {
                        data = Locomotive.Speed110Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_120:
                    {
                        data = Locomotive.Speed120Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_130:
                    {
                        data = Locomotive.Speed130Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_140:
                    {
                        data = Locomotive.Speed140Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_150:
                    {
                        data = Locomotive.Speed150Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_160:
                    {
                        data = Locomotive.Speed160Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_170:
                    {
                        data = Locomotive.Speed170Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_180:
                    {
                        data = Locomotive.Speed180Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_190:
                    {
                        data = Locomotive.Speed190Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_200:
                    {
                        data = Locomotive.Speed200Pressed ? 1 : 0;
                        break;
                    }
                default:
                    data = 0;
                    break;
            }
            return data;
        }

        public enum AvvSignal {
            Stop,
            Restricted,
            Restricting40,
            Clear,
            Restricting60,
            Restricting80,
            Restricting100
        };

        public enum ControllerCruiseControlLogic
        {
            None,
            Full,
            SpeedOnly
        }

        public AvvSignal avvSignal = AvvSignal.Stop;
        public void DrawAvvSignal(AvvSignal ToState)
        {
            avvSignal = ToState;
        }
    }
}