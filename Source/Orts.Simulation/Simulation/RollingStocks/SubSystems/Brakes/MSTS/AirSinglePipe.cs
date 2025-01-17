﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// Debug for Airbrake operation - Train Pipe Leak
//#define DEBUG_TRAIN_PIPE_LEAK

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class AirSinglePipe : MSTSBrakeSystem
    {
        protected TrainCar Car;
        protected float HandbrakePercent;
        protected float CylPressurePSI = 64;
        protected float AutoCylPressurePSI = 64;
        protected float AuxResPressurePSI = 64;
        protected float EmergResPressurePSI = 64;
        protected float FullServPressurePSI = 50;
        protected float MaxCylPressurePSI = 64;
        protected float AuxCylVolumeRatio = 2.5f;
        protected float AuxBrakeLineVolumeRatio;
        protected float EmergResVolumeM3 = 0.07f;
        protected float RetainerPressureThresholdPSI;
        protected float ReleaseRatePSIpS = 1.86f;
        protected float MaxReleaseRatePSIpS = 1.86f;
        protected float MaxApplicationRatePSIpS = .9f;
        protected float MaxAuxilaryChargingRatePSIpS = 1.684f;
        protected float BrakeInsensitivityPSIpS = 0;
        protected float EmergResChargingRatePSIpS = 1.684f;
        protected float EmergAuxVolumeRatio = 1.4f;
        protected string DebugType = string.Empty;
        protected string RetainerDebugState = string.Empty;
        protected bool NoMRPAuxResCharging;
        protected float CylVolumeM3;

        protected bool TrainBrakePressureChanging = false;
        protected bool BrakePipePressureChanging = false;
        protected float SoundTriggerCounter = 0;
        protected float prevCylPressurePSI = 0;
        protected float prevBrakePipePressurePSI = 0;
        protected bool BailOffOn;

        protected bool StartOn = true;        
        protected float BrakePipeChangeRate = 0;
        protected float T0 = 0;
        protected float T1 = 0;
        protected int T00 = 0;
        protected float PrevAuxResPressurePSI = 0;
        protected float Threshold = 0;
        protected float prevBrakeLine1PressurePSI = 0;
        protected bool NotConnected = false;
        protected bool BrakeCylApply = false;
        protected bool BrakeCylRelease = false;     

        /// <summary>
        /// EP brake holding valve. Needs to be closed (Lap) in case of brake application or holding.
        /// For non-EP brake types must default to and remain in Release.
        /// </summary>
        protected ValveState HoldingValve = ValveState.Release;

        public enum ValveState
        {
            [GetString("Lap")] Lap,
            [GetString("Apply")] Apply,
            [GetString("Release")] Release,
            [GetString("Emergency")] Emergency
        };
        protected ValveState TripleValveState = ValveState.Lap;

        public AirSinglePipe(TrainCar car)
        {
            Car = car;
            // taking into account very short (fake) cars to prevent NaNs in brake line pressures
            //DebugType = "1P";
            // Force graduated releasable brakes. Workaround for MSTS with bugs preventing to set eng/wag files correctly for this.
            (Car as MSTSWagon).DistributorPresent |= Car.Simulator.Settings.GraduatedRelease;

            if (Car.Simulator.Settings.RetainersOnAllCars && !(Car is MSTSLocomotive))
                (Car as MSTSWagon).RetainerPositions = 4;
        }

        public override bool GetHandbrakeStatus()
        {
            return HandbrakePercent > 0;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            AirSinglePipe thiscopy = (AirSinglePipe)copy;
            MaxCylPressurePSI = thiscopy.MaxCylPressurePSI;
            AuxCylVolumeRatio = thiscopy.AuxCylVolumeRatio;
            AuxBrakeLineVolumeRatio = thiscopy.AuxBrakeLineVolumeRatio;
            EmergResVolumeM3 = thiscopy.EmergResVolumeM3;
            BrakePipeVolumeM3 = thiscopy.BrakePipeVolumeM3;
            RetainerPressureThresholdPSI = thiscopy.RetainerPressureThresholdPSI;
            ReleaseRatePSIpS = thiscopy.ReleaseRatePSIpS;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            MaxAuxilaryChargingRatePSIpS = thiscopy.MaxAuxilaryChargingRatePSIpS;
            BrakeInsensitivityPSIpS = thiscopy.BrakeInsensitivityPSIpS;
            EmergResChargingRatePSIpS = thiscopy.EmergResChargingRatePSIpS;
            EmergAuxVolumeRatio = thiscopy.EmergAuxVolumeRatio;
            TwoPipes = thiscopy.TwoPipes;
            NoMRPAuxResCharging = thiscopy.NoMRPAuxResCharging;
            HoldingValve = thiscopy.HoldingValve;
            TrainPipeLeakRatePSIpS = thiscopy.TrainPipeLeakRatePSIpS;            
            TripleValveState = thiscopy.TripleValveState;
            BrakeSensitivityPSIpS = thiscopy.BrakeSensitivityPSIpS;
            OverchargeEliminationRatePSIpS = thiscopy.OverchargeEliminationRatePSIpS;
            BrakeCylinderMaxSystemPressurePSI = thiscopy.BrakeCylinderMaxSystemPressurePSI;
            TrainBrakesControllerMaxOverchargePressurePSI = thiscopy.TrainBrakesControllerMaxOverchargePressurePSI;
            BrakeMassG = thiscopy.BrakeMassG;
            BrakeMassP = thiscopy.BrakeMassP;
            BrakeMassR = thiscopy.BrakeMassR;
            BrakeMassEmpty = thiscopy.BrakeMassEmpty;
            BrakeMassLoaded = thiscopy.BrakeMassLoaded;
            DebugKoef = thiscopy.DebugKoef;
            MaxReleaseRatePSIpSG = thiscopy.MaxReleaseRatePSIpSG;
            MaxApplicationRatePSIpSG = thiscopy.MaxApplicationRatePSIpSG;
            MaxReleaseRatePSIpSP = thiscopy.MaxReleaseRatePSIpSP;
            MaxApplicationRatePSIpSP = thiscopy.MaxApplicationRatePSIpSP;
            MaxReleaseRatePSIpSR = thiscopy.MaxReleaseRatePSIpSR;
            MaxApplicationRatePSIpSR = thiscopy.MaxApplicationRatePSIpSR;
            maxPressurePSI0 = thiscopy.maxPressurePSI0;
            AutoLoadRegulatorEquipped = thiscopy.AutoLoadRegulatorEquipped;
            AutoLoadRegulatorMaxBrakeMass = thiscopy.AutoLoadRegulatorMaxBrakeMass;
        }

        // Get the brake BC & BP for EOT conditions
        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            string s = string.Format(
                " BC {0}",
                FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true));
                s += string.Format(" BP {0}", FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true));
            return s;
        }

        // Get Brake information for train
        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            string s = string.Format(" EQ {0}", FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.PSI, units[BrakeSystemComponent.EqualizingReservoir], true));
            s += string.Format(
                " BC {0}",
                FormatStrings.FormatPressure(AutoCylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)
            );

            s += string.Format(
                " BP {0}",                
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)
            );

            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += "  Konec vlaku" + lastCarBrakeSystem.GetStatus(units);
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);

            s += string.Format("  Rychlost změny tlaku v potrubí {0:F5}bar/s", BrakePipeChangeRate / 14.50377f);
            s += string.Format("  Netěsnost potrubí {0:F5}bar/s", Car.Train.TotalTrainTrainPipeLeakRate / 14.50377f);
            s += string.Format("  Objem potrubí {0:F0}l", Car.Train.TotalTrainBrakePipeVolumeM3 * 1000);
            s += string.Format("  Kapacita hl.jímky a přilehlého potrubí {0:F0}l", Car.Train.TotalCapacityMainResBrakePipe * 1000 / 14.50377f);
          
            //s += string.Format("    maxPressurePSI {0:F1}bar", maxPressurePSI0 / 14.50377f);            
            //s += string.Format("    MaxCylPressurePSI {0:F1}bar", MaxCylPressurePSI / 14.50377f);
            //s += string.Format("    MCP {0:F1}bar", MCP / 14.50377f);
            //s += string.Format("    EngineBrakeStatus{0:F1}", Car.GetEngineBrakeStatus());
            //s += string.Format("    Tlak nízkotlak.přebití {0:F1}bar", TrainBrakesControllerMaxOverchargePressurePSI / 14.50377f);           
            //s += string.Format("    Max tlak do BV {0:F1}bar", BrakeCylinderMaxSystemPressurePSI / 14.50377f);
            //s += string.Format("    Rychlost odvětrávání nízkotlakého přebití {0:F5}bar/s", Car.BrakeSystem.OverchargeEliminationRatePSIpS / 14.50377f);            
            //s += string.Format("    BrakeLine1PressurePSI {0:F4}", BrakeLine1PressurePSI);
            //s += string.Format("    prevBrakeLine1PressurePSI {0:F4}", prevBrakeLine1PressurePSI);
            //s += string.Format("    Citlivost brzdiče {0} ", FormatStrings.FormatPressure(BrakeSensitivityPSIpS, PressureUnit.PSI, units[BrakeSystemComponent.EqualizingReservoir], true));            
            //s += string.Format("    MaxTlakVálce {0:F0}", MaxCylPressurePSI);
            //s += string.Format("    TlakVálce {0:F0}", AutoCylPressurePSI);          
            //s += string.Format("    PrevAuxResPressure {0:F1} bar", PrevAuxResPressurePSI / 14.50377f);
            //s += string.Format("    threshold {0:F1} bar", Threshold / 14.50377f);
            //s += string.Format("    T0 {0:F0}", T0);
            return s;
        }

        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return new string[] {
                DebugType,
                FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true),
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true),
                FormatStrings.FormatPressure(AuxResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.AuxiliaryReservoir], true),
                (Car as MSTSWagon).EmergencyReservoirPresent ? FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.EmergencyReservoir], true) : string.Empty,
                TwoPipes ? FormatStrings.FormatPressure(BrakeLine2PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.MainPipe], true) : string.Empty,
                (Car as MSTSWagon).RetainerPositions == 0 ? string.Empty : RetainerDebugState,
                Simulator.Catalog.GetString(GetStringAttribute.GetPrettyName(TripleValveState)),
                string.Empty, // Spacer because the state above needs 2 columns.
                (Car as MSTSWagon).HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : " ",//HudScroll feature requires for the last value, at least one space instead of string.Empty,
                
                string.Empty, // Spacer because the state above needs 2 columns.
                string.Format("{0:F5}bar/s", TrainPipeLeakRatePSIpS / 14.50377f),
                string.Empty, // Spacer because the state above needs 2 columns.
                string.Format("{0:F0}l", BrakePipeVolumeM3 * 1000),
                string.Empty, // Spacer because the state above needs 2 columns.
                string.Format("{0:F0}l", CylVolumeM3 * 1000),
                string.Empty, // Spacer because the state above needs 2 columns.
                string.Format("{0:F0}l", TotalCapacityMainResBrakePipe * 1000 / 14.50377f),               
                string.Format("{0:F0}", BrakeCarModeText),
                string.Format("{0} {1:F0}t", AutoLoadRegulatorEquipped ? "Auto   " : "", BrakeMassKG / 1000),              
                                
                string.Empty, // Spacer because the state above needs 2 columns.              
                string.Format("DebugKoef {0:F1}", DebugKoef),
                string.Empty, // Spacer because the state above needs 2 columns.
                
                //string.Format("Napousteni {0:F3}bar/s", MaxApplicationRatePSIpS / 14.50377f),
                //string.Empty, // Spacer because the state above needs 2 columns.
                //string.Format("Vypousteni {0:F3}bar/s", ReleaseRatePSIpS / 14.50377f),                
            };
        }

        public override float GetCylPressurePSI()
        {
            return CylPressurePSI;
        }

        public override float GetCylVolumeM3()
        {
            return CylVolumeM3;
        }

        public float GetFullServPressurePSI()
        {
            return FullServPressurePSI;
        }

        public float GetMaxCylPressurePSI()
        {
            return MaxCylPressurePSI;
        }

        public float GetAuxCylVolumeRatio()
        {
            return AuxCylVolumeRatio;
        }

        public float GetMaxReleaseRatePSIpS()
        {
            return MaxReleaseRatePSIpS;
        }

        public float GetMaxApplicationRatePSIpS()
        {
            return MaxApplicationRatePSIpS;
        }

        public override float GetVacResPressurePSI()
        {
            return 0;
        }

        public override float GetVacResVolume()
        {
            return 0;
        }
        public override float GetVacBrakeCylNumber()
        {
            return 0;
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxCylPressurePSI = AutoCylPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(triplevalveratio": AuxCylVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(brakedistributorreleaserate":
                case "wagon(maxreleaserate": MaxReleaseRatePSIpS = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(brakedistributorapplicationrate":
                case "wagon(maxapplicationrate": MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxauxilarychargingrate": MaxAuxilaryChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyreschargingrate": EmergResChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyresvolumemultiplier": EmergAuxVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(emergencyrescapacity": EmergResVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                
                // OpenRails specific parameters
                case "wagon(brakepipevolume": BrakePipeVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                //case "wagon(ortsbrakeinsensitivity": BrakeInsensitivityPSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Načte hodnotu netěsnosti lokomotivy i vozů
                case "wagon(trainpipeleakrate": TrainPipeLeakRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                
                // Načte hodnotu citivosti brzdy lokomotivy i vozů
                case "wagon(brakesensitivity": BrakeSensitivityPSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Načte brzdící váhu lokomotivy i vozů v režimech G, P, R, Prázdný, Ložený
                case "wagon(brakemassg": BrakeMassG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassp": BrakeMassP = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassr": BrakeMassR = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassempty": BrakeMassEmpty = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassloaded": BrakeMassLoaded = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;

                // Načte hodnoty napouštění a vypouštění brzdových válců lokomotivy i vozů v režimech G, P, R
                case "wagon(maxapplicationrateg": MaxApplicationRatePSIpSG = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxreleaserateg": MaxReleaseRatePSIpSG = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxapplicationratep": MaxApplicationRatePSIpSP = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxreleaseratep": MaxReleaseRatePSIpSP = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxapplicationrater": MaxApplicationRatePSIpSR = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxreleaserater": MaxReleaseRatePSIpSR = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Automatický zátěžový regulátor pro vozy
                case "wagon(autoloadregulatorequipped": AutoLoadRegulatorEquipped = stf.ReadBoolBlock(false); break;
                case "wagon(autoloadregulatormaxbrakemass": AutoLoadRegulatorMaxBrakeMass = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;

                // Ladící koeficient pro ladiče brzd
                case "wagon(debugkoef": DebugKoef = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;

                // Načte hodnotu rychlosti eliminace níkotlakého přebití                              
                case "engine(overchargeeliminationrate": OverchargeEliminationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                
                // Načte hodnotu maximálního tlaku v brzdovém válci
                case "engine(brakecylindermaxsystempressure": BrakeCylinderMaxSystemPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                
                // Načte hodnotu tlaku při nízkotlakém přebití
                case "engine(trainbrakescontrollermaxoverchargepressure": TrainBrakesControllerMaxOverchargePressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(HandbrakePercent);
            outf.Write(ReleaseRatePSIpS);
            outf.Write(RetainerPressureThresholdPSI);
            outf.Write(AutoCylPressurePSI);
            outf.Write(AuxResPressurePSI);
            outf.Write(EmergResPressurePSI);
            outf.Write(FullServPressurePSI);
            outf.Write((int)TripleValveState);
            outf.Write(FrontBrakeHoseConnected);
            outf.Write(AngleCockAOpen);
            outf.Write(AngleCockBOpen);
            outf.Write(BleedOffValveOpen);
            outf.Write((int)HoldingValve);
            outf.Write(CylVolumeM3);
            outf.Write(BailOffOn);
            outf.Write(StartOn);
            outf.Write(PrevAuxResPressurePSI);
            outf.Write(TrainPipeLeakRatePSIpS);
            outf.Write(AutoCylPressurePSI1);
            outf.Write(AutoCylPressurePSI0);
            outf.Write(BrakeCarMode);
            outf.Write(BrakeCarModeText);
            outf.Write(BrakeCarModePL);
            outf.Write(BrakeCarModeTextPL);
            outf.Write(MaxApplicationRatePSIpS0);
            outf.Write(MaxReleaseRatePSIpS0);
            outf.Write(maxPressurePSI0);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            HandbrakePercent = inf.ReadSingle();
            ReleaseRatePSIpS = inf.ReadSingle();
            RetainerPressureThresholdPSI = inf.ReadSingle();
            AutoCylPressurePSI = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            EmergResPressurePSI = inf.ReadSingle();
            FullServPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
            FrontBrakeHoseConnected = inf.ReadBoolean();
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockBOpen = inf.ReadBoolean();
            BleedOffValveOpen = inf.ReadBoolean();
            HoldingValve = (ValveState)inf.ReadInt32();
            CylVolumeM3 = inf.ReadSingle();
            BailOffOn = inf.ReadBoolean();
            StartOn = inf.ReadBoolean();
            PrevAuxResPressurePSI = inf.ReadSingle();
            TrainPipeLeakRatePSIpS = inf.ReadSingle();
            AutoCylPressurePSI1 = inf.ReadSingle();
            AutoCylPressurePSI0 = inf.ReadSingle();
            BrakeCarMode = inf.ReadSingle();
            BrakeCarModeText = inf.ReadString();
            BrakeCarModePL = inf.ReadSingle();
            BrakeCarModeTextPL = inf.ReadString();
            MaxApplicationRatePSIpS0 = inf.ReadSingle();
            MaxReleaseRatePSIpS0 = inf.ReadSingle();
            maxPressurePSI0 = inf.ReadSingle();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            // reducing size of Emergency Reservoir for short (fake) cars
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && Car.CarLengthM <= 1)
            EmergResVolumeM3 = Math.Min (0.02f, EmergResVolumeM3);

            // In simple brake mode set emergency reservoir volume, override high volume values to allow faster brake release.
            if (Car.Simulator.Settings.SimpleControlPhysics && EmergResVolumeM3 > 2.0)
                EmergResVolumeM3 = 0.7f;

            // Zjistí maximální pracovní tlak v systému
            if (StartOn) maxPressurePSI0 = Car.Train.EqualReservoirPressurePSIorInHg;
            
            Car.Train.EqualReservoirPressurePSIorInHg = maxPressurePSI = maxPressurePSI0 = 5.0f * 14.50377f;
            BrakeLine1PressurePSI = maxPressurePSI0;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            PrevAuxResPressurePSI = 0;
            prevBrakeLine1PressurePSI = 0;
            
            if ((Car as MSTSWagon).EmergencyReservoirPresent || maxPressurePSI > 0)
                EmergResPressurePSI = maxPressurePSI;
            FullServPressurePSI = fullServPressurePSI;
            AutoCylPressurePSI0 = immediateRelease ? 0 : Math.Min((maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, MaxCylPressurePSI);
            AuxResPressurePSI = AutoCylPressurePSI == 0 ? (maxPressurePSI > BrakeLine1PressurePSI ? maxPressurePSI : BrakeLine1PressurePSI)
                : Math.Max(maxPressurePSI - AutoCylPressurePSI / AuxCylVolumeRatio, BrakeLine1PressurePSI);
            TripleValveState = ValveState.Lap;
            HoldingValve = ValveState.Release;
            HandbrakePercent = handbrakeOn & (Car as MSTSWagon).HandBrakePresent ? 100 : 0;
            SetRetainer(RetainerSetting.Exhaust);
            MSTSLocomotive loco = Car as MSTSLocomotive;
            if (loco != null) 
                loco.MainResPressurePSI = loco.MaxMainResPressurePSI;
            }

        /// <summary>
        /// Used when initial speed > 0
        /// </summary>
        public override void InitializeMoving ()
        {
            var emergResPressurePSI = EmergResPressurePSI;
            Initialize(false, 0, FullServPressurePSI, true);
            EmergResPressurePSI = emergResPressurePSI;
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
        }

        public virtual void UpdateTripleValveState(float controlPressurePSI)
        {
            // Funkční 3-cestný ventil
            if (BrakeLine1PressurePSI < AuxResPressurePSI - 0.1f) TripleValveState = ValveState.Apply;
                else TripleValveState = ValveState.Lap;
            if (BrakeLine1PressurePSI > AuxResPressurePSI + 0.1f) TripleValveState = ValveState.Release;     
        }

        public override void Update(float elapsedClockSeconds)
        {
            // Emergency reservoir's second role (in OpenRails) is to act as a control reservoir,
            // maintaining a reference control pressure for graduated release brake actions.
            // Thus this pressure must be set even in brake systems ER not present otherwise. It just stays static in this case.

            //float threshold = Math.Max(RetainerPressureThresholdPSI,
            //                (Car as MSTSWagon).DistributorPresent ? (PrevAuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio : 0);
            float threshold = (PrevAuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
            Threshold = threshold;

            // Studeny start lokomotivy (vzduchojemy na 0)            
            if (StartOn)
            {
                MSTSLocomotive loco = Car as MSTSLocomotive;
                if (loco != null) loco.MainResPressurePSI = 0;
                FullServPressurePSI = 0;
                AutoCylPressurePSI = 0;
                AutoCylPressurePSI0 = 0;
                AuxResPressurePSI = 0;
                PrevAuxResPressurePSI = 0;
                BrakeLine1PressurePSI = 0;
                BrakeLine2PressurePSI = 0;
                BrakeLine3PressurePSI = 0;
                prevBrakeLine1PressurePSI = 0;
                HandbrakePercent = (Car as MSTSWagon).HandBrakePresent ? 100 : 100;                
                MaxReleaseRatePSIpS0 = MaxReleaseRatePSIpS;
                MaxApplicationRatePSIpS0 = MaxApplicationRatePSIpS;
                StartOn = false;
            }

            // Časy pro napouštění a vypouštění brzdového válce v sekundách režimy G, P, R
            float TimeApplyG = 22.0f;
            float TimeReleaseG = 50.0f;

            float TimeApplyP = 5.3f;
            float TimeReleaseP = 22.4f;

            float TimeApplyR = 3.5f;
            float TimeReleaseR = 22.4f;

            // Vypočítá rychlost plnění/vyprazdňování brzdových válců s ohledem na režim
            switch (BrakeCarMode)
            {
                case 0: // Režim G                     
                    if (MaxApplicationRatePSIpSG == 0) MaxApplicationRatePSIpS = MaxApplicationRatePSIpS0 / (TimeApplyG / TimeApplyP);
                    else MaxApplicationRatePSIpS = MaxApplicationRatePSIpSG;

                    if (MaxReleaseRatePSIpSG == 0) MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpS0 / (TimeReleaseG / TimeReleaseP);
                    else MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpSG;
                    break;
                case 1: // Režim P                    
                    if (MaxApplicationRatePSIpSP == 0) MaxApplicationRatePSIpS = MaxApplicationRatePSIpS0;
                    else MaxApplicationRatePSIpS = MaxApplicationRatePSIpSP;

                    if (MaxReleaseRatePSIpSP == 0) MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpS0;
                    else MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpSP;
                    break;
                case 2: // Režim R
                    if (MaxApplicationRatePSIpSR == 0) MaxApplicationRatePSIpS = MaxApplicationRatePSIpS0 / (TimeApplyR / TimeApplyP);
                    else MaxApplicationRatePSIpS = MaxApplicationRatePSIpSR;

                    if (MaxReleaseRatePSIpSR == 0) MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpS0 / (TimeReleaseR / TimeReleaseP);
                    else MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpSR;
                    break;
            }

            // Načte hodnotu maximálního tlaku v BV
            MCP = GetMaxCylPressurePSI();

            // Výsledný tlak v brzdovém válci - přičte tlak přímočinné brzdy k tlaku v BV průběžné brzdy
            AutoCylPressurePSI = AutoCylPressurePSI0 + AutoCylPressurePSI1;

            // Tlak v BV nepřekročí maximální tlak pro BV nadefinovaný v eng lokomotivy
            if (BrakeCylinderMaxSystemPressurePSI == 0) BrakeCylinderMaxSystemPressurePSI = MaxCylPressurePSI * 1.03f; // Výchozí hodnota pro maximální tlak přímočinné brzdy v BV 
            if (AutoCylPressurePSI > BrakeCylinderMaxSystemPressurePSI) AutoCylPressurePSI = BrakeCylinderMaxSystemPressurePSI;

            // Snižuje tlak v potrubí kvůli netěsnosti
            if (BrakeLine1PressurePSI - Car.Train.TotalTrainTrainPipeLeakRate > 0)
                BrakeLine1PressurePSI -= Car.Train.TotalTrainTrainPipeLeakRate * elapsedClockSeconds;

            // Odvětrání pomocné jímky při přebití
            if (AuxResPressurePSI > maxPressurePSI0 && BrakeLine1PressurePSI < AuxResPressurePSI - 0.1f) AuxResPressurePSI -= elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;

            // Výpočet objemu vzduchu brzdových válců a násobiče pro objem pomocné jímky
            CylVolumeM3 = EmergResVolumeM3 / EmergAuxVolumeRatio / AuxCylVolumeRatio;
            AuxBrakeLineVolumeRatio = EmergResVolumeM3 / EmergAuxVolumeRatio / BrakePipeVolumeM3;

            if (BleedOffValveOpen)
            {
                if (AuxResPressurePSI < 0.01f && AutoCylPressurePSI < 0.01f && BrakeLine1PressurePSI < 0.01f && (EmergResPressurePSI < 0.01f || !(Car as MSTSWagon).EmergencyReservoirPresent))
                {
                    BleedOffValveOpen = false;
                }
                else
                {
                    AuxResPressurePSI -= elapsedClockSeconds * MaxApplicationRatePSIpS;
                    if (AuxResPressurePSI < 0)
                        AuxResPressurePSI = 0;
                    
                    AutoCylPressurePSI0 -= elapsedClockSeconds * MaxReleaseRatePSIpS;                  
                    if (AutoCylPressurePSI0 < 0)
                        AutoCylPressurePSI0 = 0;
                    
                    if ((Car as MSTSWagon).EmergencyReservoirPresent)
                    {
                        EmergResPressurePSI -= elapsedClockSeconds * EmergResChargingRatePSIpS;
                        if (EmergResPressurePSI < 0)
                            EmergResPressurePSI = 0;
                    }
                    TripleValveState = ValveState.Release;
                }
            }
            else
                UpdateTripleValveState(threshold);

             // Zjistí rychlost změny tlaku v potrubí
            if (T0 >= 1.0f) T0 = 0.0f;
            if (T0 == 0.0f) prevBrakeLine1PressurePSI = BrakeLine1PressurePSI;
            T0 += elapsedClockSeconds;
            if (T0 > 0.08f && T0 < 0.12f)
            {
               T0 = 0.0f;
               BrakePipeChangeRate = Math.Abs(prevBrakeLine1PressurePSI - BrakeLine1PressurePSI) * 15;
            }

            if (AutoCylPressurePSI0 < 1.0f) T00 = 0;

            // triple valve is set to charge the brake cylinder
            if (TripleValveState == ValveState.Apply || TripleValveState == ValveState.Emergency)
            {
                BrakeCylRelease = false;

                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                //if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                //    dp = (AuxResPressurePSI - AutoCylPressurePSI) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                //if (TwoPipes && dp > threshold - AutoCylPressurePSI)
                //    dp = threshold - AutoCylPressurePSI;
                //if (AutoCylPressurePSI + dp > MaxCylPressurePSI)
                //    dp = MaxCylPressurePSI - AutoCylPressurePSI;
                //if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                //    dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                //if (dp < 0)
                //    dp = 0;

                // Zaznamená poslední stav pomocné jímky pro určení pracovního bodu pomocné jímky
                if (T00 == 0)
                    PrevAuxResPressurePSI = AuxResPressurePSI;
                T00++;

                if (TwoPipes && dp > threshold - AutoCylPressurePSI0)
                    dp = threshold - AutoCylPressurePSI0;

                if (AutoCylPressurePSI0 + dp > MaxCylPressurePSI)
                    dp = MaxCylPressurePSI - AutoCylPressurePSI0;

                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                    dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;

                if (BrakeCylApply) AutoCylPressurePSI0 += dp;
                if (AutoCylPressurePSI0 >= threshold) BrakeCylApply = false;
                
                // Otestuje citlivost brzdy 
                if (BrakeSensitivityPSIpS == 0) BrakeSensitivityPSIpS = 0.07252f; // Výchozí nastavení 0.07252PSI/s ( 0.005bar/s)
                if (BrakePipeChangeRate >= BrakeSensitivityPSIpS)
                    BrakeCylApply = true;
                
                 // Plní pomocnou jímku stále stejnou rychlostí 0.1bar/s
                 if (AuxResPressurePSI > maxPressurePSI0 && BrakeLine1PressurePSI > AuxResPressurePSI)
                 {
                    dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                    AuxResPressurePSI += dp;
                 }

                AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                //AutoCylPressurePSI += dp;

                if (TripleValveState == ValveState.Emergency && (Car as MSTSWagon).EmergencyReservoirPresent)
                {
                    dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                        dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                    EmergResPressurePSI -= dp;
                    AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                }
            }

            if (BrakeCylRelease) 
            {
                if (AutoCylPressurePSI0 > threshold)
                {
                    AutoCylPressurePSI0 -= elapsedClockSeconds * ReleaseRatePSIpS;
                    if (AutoCylPressurePSI0 < threshold)
                        AutoCylPressurePSI0 = threshold;
                }
                else BrakeCylRelease = false;
            }

            // triple valve set to release pressure in brake cylinder and EP valve set
            if (TripleValveState == ValveState.Release && HoldingValve == ValveState.Release)
            {
                BrakeCylRelease = true;
                BrakeCylApply = false;

                if ((Car as MSTSWagon).EmergencyReservoirPresent)
				{
                    if (!(Car as MSTSWagon).DistributorPresent && AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
					{
						float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
						if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
							dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
						if (BrakeLine1PressurePSI < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
							dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / EmergAuxVolumeRatio;
						EmergResPressurePSI -= dp;
						AuxResPressurePSI += dp * EmergAuxVolumeRatio;
					}
					if (AuxResPressurePSI > EmergResPressurePSI)
					{
						float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
						if (EmergResPressurePSI + dp > AuxResPressurePSI - dp * EmergAuxVolumeRatio)
							dp = (AuxResPressurePSI - EmergResPressurePSI) / (1 + EmergAuxVolumeRatio);
						EmergResPressurePSI += dp;
						AuxResPressurePSI -= dp * EmergAuxVolumeRatio;
					}
				}
                if (AuxResPressurePSI < BrakeLine1PressurePSI && (!TwoPipes || NoMRPAuxResCharging || BrakeLine2PressurePSI < BrakeLine1PressurePSI) && !BleedOffValveOpen)
                {
                    float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS; // Change in pressure for train brake pipe.
                    if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                        dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI += dp;
                    BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;  // Adjust the train brake pipe pressure
                }
                //if (AuxResPressurePSI > BrakeLine1PressurePSI) // Allow small flow from auxiliary reservoir to brake pipe so the triple valve is not sensible to small pressure variations when in release position
                //{
                //    float dp = elapsedClockSeconds * BrakeInsensitivityPSIpS;
                //    if (AuxResPressurePSI - dp < BrakeLine1PressurePSI + dp * AuxBrakeLineVolumeRatio)
                //        dp = (AuxResPressurePSI - BrakeLine1PressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                //    AuxResPressurePSI -= dp;
                //    BrakeLine1PressurePSI += dp * AuxBrakeLineVolumeRatio;
                //}
                }

            if (TwoPipes
                && !NoMRPAuxResCharging
                && AuxResPressurePSI < BrakeLine2PressurePSI
                && AuxResPressurePSI < EmergResPressurePSI
                && (BrakeLine2PressurePSI > BrakeLine1PressurePSI || TripleValveState != ValveState.Release) && !BleedOffValveOpen)
            {
                float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                if (AuxResPressurePSI + dp > BrakeLine2PressurePSI - dp * AuxBrakeLineVolumeRatio)
                    dp = (BrakeLine2PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                AuxResPressurePSI += dp;
                BrakeLine2PressurePSI -= dp * AuxBrakeLineVolumeRatio;
            }

            if (Car is MSTSLocomotive && (Car as MSTSLocomotive).PowerOn
                || Car is MSTSLocomotive && (Car as MSTSLocomotive).EDBIndependent && (Car as MSTSLocomotive).PowerOnFilter > 0)
            {
                var loco = Car as MSTSLocomotive;
                BailOffOn = false;
                if ((loco.Train.LeadLocomotiveIndex >= 0 && ((MSTSLocomotive)loco.Train.Cars[loco.Train.LeadLocomotiveIndex]).BailOff) || loco.DynamicBrakeAutoBailOff && loco.Train.MUDynamicBrakePercent > 0 && loco.DynamicBrakeForceCurves == null)
                {
                    BailOffOn = true;
                }
                else if (loco.DynamicBrakeAutoBailOff && loco.Train.MUDynamicBrakePercent > 0 && loco.DynamicBrakeForceCurves != null)
                {
                    var dynforce = loco.DynamicBrakeForceCurves.Get(1.0f, loco.AbsSpeedMpS);  // max dynforce at that speed
                    if ((loco.MaxDynamicBrakeForceN == 0 && dynforce > 0) || dynforce > loco.MaxDynamicBrakeForceN * 0.6)
                        BailOffOn = true;
                }
                if (BailOffOn)
                    AutoCylPressurePSI0 -= MaxReleaseRatePSIpS * elapsedClockSeconds;
            }

            if (AutoCylPressurePSI0 < 0)
                AutoCylPressurePSI0 = 0;
            if (AutoCylPressurePSI < BrakeLine3PressurePSI) // Brake Cylinder pressure will be the greater of engine brake pressure or train brake pressure
                CylPressurePSI = BrakeLine3PressurePSI;
            else
                CylPressurePSI = AutoCylPressurePSI;

            // Record HUD display values for brake cylinders depending upon whether they are wagons or locomotives/tenders (which are subject to their own engine brakes)   
            if (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender)
            {
                Car.Train.HUDLocomotiveBrakeCylinderPSI = CylPressurePSI;
                Car.Train.HUDWagonBrakeCylinderPSI = Car.Train.HUDLocomotiveBrakeCylinderPSI;  // Initially set Wagon value same as locomotive, will be overwritten if a wagon is attached
            }
            else
            {
                // Record the Brake Cylinder pressure in first wagon, as EOT is also captured elsewhere, and this will provide the two extremeties of the train
                // Identifies the first wagon based upon the previously identified UiD 
                if (Car.UiD == Car.Train.FirstCarUiD)
                {
                    Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSI;
                }

            }

            // If wagons are not attached to the locomotive, then set wagon BC pressure to same as locomotive in the Train brake line
            if (!Car.Train.WagonsAttached &&  (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender) ) 
            {
                Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSI;
            }

            float f;
            if (!Car.BrakesStuck)
            {
                f = Car.MaxBrakeForceN * Math.Min(CylPressurePSI / MaxCylPressurePSI, 1);
                if (f < Car.MaxHandbrakeForceN * HandbrakePercent / 100)
                    f = Car.MaxHandbrakeForceN * HandbrakePercent / 100;
            }
            else f = Math.Max(Car.MaxBrakeForceN, Car.MaxHandbrakeForceN / 2); 
            Car.BrakeRetardForceN = f * Car.BrakeShoeRetardCoefficientFrictionAdjFactor; // calculates value of force applied to wheel, independent of wheel skid
            if (Car.BrakeSkid) // Test to see if wheels are skiding to excessive brake force
            {
                Car.BrakeForceN = f * Car.SkidFriction;   // if excessive brakeforce, wheel skids, and loses adhesion
            }
            else
            {
                Car.BrakeForceN = f * Car.BrakeShoeCoefficientFrictionAdjFactor; // In advanced adhesion model brake shoe coefficient varies with speed, in simple model constant force applied as per value in WAG file, will vary with wheel skid.
            }

            // sound trigger checking runs every half second, to avoid the problems caused by the jumping BrakeLine1PressurePSI value, and also saves cpu time :)
            if (SoundTriggerCounter >= 0.5f)
            {
                SoundTriggerCounter = 0f;
                if ( Math.Abs(threshold - prevCylPressurePSI) > 0.1f) //(AutoCylPressurePSI != prevCylPressurePSI)
                {
                    if (!TrainBrakePressureChanging)
                    {
                        if (threshold > prevCylPressurePSI)
                            Car.SignalEvent(Event.TrainBrakePressureIncrease);
                        else
                            Car.SignalEvent(Event.TrainBrakePressureDecrease);
                        TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    }

                }
                else if (TrainBrakePressureChanging)
                {
                    TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    Car.SignalEvent(Event.TrainBrakePressureStoppedChanging);
                }

                if ( Math.Abs(BrakeLine1PressurePSI - prevBrakePipePressurePSI) > 0.1f /*BrakeLine1PressurePSI > prevBrakePipePressurePSI*/)
                {
                    if (!BrakePipePressureChanging)
                    {
                        if (BrakeLine1PressurePSI > prevBrakePipePressurePSI)
                            Car.SignalEvent(Event.BrakePipePressureIncrease);
                        else
                            Car.SignalEvent(Event.BrakePipePressureDecrease);
                        BrakePipePressureChanging = !BrakePipePressureChanging;
                    }

                }
                else if (BrakePipePressureChanging)
                {
                    BrakePipePressureChanging = !BrakePipePressureChanging;
                    Car.SignalEvent(Event.BrakePipePressureStoppedChanging);
                }
                prevCylPressurePSI = threshold;
                prevBrakePipePressurePSI = BrakeLine1PressurePSI;
            }
            SoundTriggerCounter = SoundTriggerCounter + elapsedClockSeconds;
        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            PropagateBrakeLinePressures(elapsedClockSeconds, Car, TwoPipes);
        }

        protected static void PropagateBrakeLinePressures(float elapsedClockSeconds, TrainCar trainCar, bool twoPipes)
        {
            // Brake pressures are calculated on the lead locomotive first, and then propogated along each wagon in the consist.
            var train = trainCar.Train;
            var lead = trainCar as MSTSLocomotive;
            var brakePipeTimeFactorS = lead == null ? 0.003f : lead.BrakePipeTimeFactorS; // Průrazná rychlost tlakové vlny 250m/s 0.003f
            var BrakePipeChargingRatePSIorInHgpS0 = lead == null ? 29 : lead.BrakePipeChargingRatePSIorInHgpS;

            float brakePipeTimeFactorS0 = brakePipeTimeFactorS;           
            float brakePipeTimeFactorS_Apply = brakePipeTimeFactorS * 30; // Vytvoří zpoždění náběhu brzdy vlaku kvůli průrazné tlakové vlně            
            float brakePipeChargingNormalPSIpS = BrakePipeChargingRatePSIorInHgpS0; // Rychlost plnění průběžného potrubí při normálním plnění 29 PSI/s
            float brakePipeChargingQuickPSIpS = 200; // Rychlost plnění průběžného potrubí při švihu 200 PSI/s

            int nSteps = (int)(elapsedClockSeconds / brakePipeTimeFactorS + 1);
            float TrainPipeTimeVariationS = elapsedClockSeconds / nSteps;
            bool NotConnected = false;

            // Výpočet netěsnosti vzduchu v potrubí pro každý vůz
            train.TotalTrainTrainPipeLeakRate = 0f;
            foreach (TrainCar car in train.Cars)
            {
                //  Pokud není netěstnost vozu definována
                if (car.BrakeSystem.TrainPipeLeakRatePSIpS == 0)                                 
                    car.BrakeSystem.TrainPipeLeakRatePSIpS = 0.00010f * 14.50377f; // Výchozí netěsnost 0.00010bar/s                
                
                //  První vůz
                if (car == train.Cars[0] && !car.BrakeSystem.AngleCockBOpen) NotConnected = true;

                //  Ostatní kromě prvního a posledního vozu
                if (car != train.Cars[0] && car != train.Cars[train.Cars.Count - 1])
                {
                    if (NotConnected)
                    {
                        car.BrakeSystem.TrainPipeLeakRatePSIpS = 0;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                    if (!car.BrakeSystem.FrontBrakeHoseConnected || !car.BrakeSystem.AngleCockAOpen)
                    {
                        NotConnected = true;
                        car.BrakeSystem.TrainPipeLeakRatePSIpS = 0;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                    if (!car.BrakeSystem.AngleCockBOpen) NotConnected = true;
                }

                //  Poslední vůz
                if (car != train.Cars[0] && car == train.Cars[train.Cars.Count - 1])
                {
                    if (NotConnected)
                    {
                        car.BrakeSystem.TrainPipeLeakRatePSIpS = 0;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                    if (!car.BrakeSystem.FrontBrakeHoseConnected || !car.BrakeSystem.AngleCockAOpen)
                    {
                        car.BrakeSystem.TrainPipeLeakRatePSIpS = 0;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                }

                // Spočítá celkovou netěsnost vlaku 
                train.TotalTrainTrainPipeLeakRate += car.BrakeSystem.TrainPipeLeakRatePSIpS;
            }

            // Propagate brake line (1) data if pressure gradient disabled
            if (lead != null && lead.BrakePipeChargingRatePSIorInHgpS >= 1000)
            {   // pressure gradient disabled
                if (lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg)
                {
                    var dp1 = train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;
                    lead.MainResPressurePSI -= dp1 * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3;
                }
                foreach (TrainCar car in train.Cars)
                {
                    if (car.BrakeSystem.BrakeLine1PressurePSI >= 0)
                        car.BrakeSystem.BrakeLine1PressurePSI = train.EqualReservoirPressurePSIorInHg;
                }
            }
            else
            {   // approximate pressure gradient in train pipe line1
                float serviceTimeFactor = lead != null ? lead.TrainBrakeController != null && lead.TrainBrakeController.EmergencyBraking ? lead.BrakeEmergencyTimeFactorS : lead.BrakeServiceTimeFactorS : 0;
                for (int i = 0; i < nSteps; i++)
                {

                    if (lead != null)
                    {
                        // Ohlídá hodnotu v hlavní jímce, aby nepodkročila 0bar
                        if (lead.MainResPressurePSI < 0) lead.MainResPressurePSI = 0;

                        // Výchozí hodnota pro nízkotlaké přebití je 5.4 barů, pokud není definována v sekci engine
                        if (lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI == 0) lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI = 5.4f * 14.50377f;

                        // Výchozí hodnota pro odvětrávání 3 minuty 0.00222bar/s, pokud není definována v sekci engine
                        if (lead.BrakeSystem.OverchargeEliminationRatePSIpS == 0) lead.BrakeSystem.OverchargeEliminationRatePSIpS = 0.00222f * 14.50377f;

                        // Pohlídá tlak v equalizéru, aby nebyl větší než tlak hlavní jímky
                        if (train.EqualReservoirPressurePSIorInHg > lead.MainResPressurePSI) train.EqualReservoirPressurePSIorInHg = lead.MainResPressurePSI;

                        // Vyrovnává maximální tlak s tlakem v potrubí    
                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Lap) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI;

                        // Změna rychlosti plnění vzduchojemu při švihu
                        if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease)
                        {
                            BrakePipeChargingRatePSIorInHgpS0 = brakePipeChargingQuickPSIpS;  // Rychlost plnění ve vysokotlakém švihu 
                            if (lead.TrainBrakeController.MaxPressurePSI < lead.MainResPressurePSI) lead.TrainBrakeController.MaxPressurePSI = lead.MainResPressurePSI;
                        }

                        // Nízkotlaké přebití
                        else if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.OverchargeStart)
                        {
                            BrakePipeChargingRatePSIorInHgpS0 = brakePipeChargingNormalPSIpS;  // Standardní rychlost plnění 
                            if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI - lead.TrainBrakeController.ReleaseRatePSIpS * (elapsedClockSeconds / 1.0f);
                            else lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI;
                        }

                        else if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap)
                        {
                            BrakePipeChargingRatePSIorInHgpS0 = brakePipeChargingNormalPSIpS;  // Standardní rychlost plnění 
                            if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI * 1.11f) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI - lead.TrainBrakeController.QuickReleaseRatePSIpS * (elapsedClockSeconds / 1.0f);
                            else if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI - 0.03f; // Zpomalí 
                            else if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.maxPressurePSI0) lead.TrainBrakeController.MaxPressurePSI -= lead.BrakeSystem.OverchargeEliminationRatePSIpS * (elapsedClockSeconds / 12.0f);

                            if (lead.BrakeSystem.BrakeLine1PressurePSI < lead.BrakeSystem.maxPressurePSI0) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.maxPressurePSI0;
                        }

                            // Charge train brake pipe - adjust main reservoir pressure, and lead brake pressure line to maintain brake pipe equal to equalising resevoir pressure - release brakes
                            if (lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg)
                            {
                                // Calculate change in brake pipe pressure between equalising reservoir and lead brake pipe
                                float PressureDiffEqualToPipePSI = TrainPipeTimeVariationS * BrakePipeChargingRatePSIorInHgpS0; // default condition - if EQ Res is higher then Brake Pipe Pressure

                                if (lead.BrakeSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > train.EqualReservoirPressurePSIorInHg)
                                    PressureDiffEqualToPipePSI = train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;

                                if (lead.BrakeSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > lead.MainResPressurePSI)
                                    PressureDiffEqualToPipePSI = lead.MainResPressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;

                                if (PressureDiffEqualToPipePSI < 0)
                                    PressureDiffEqualToPipePSI = 0;

                                // U těchto funkcí se kompenzují ztráty vzduchu o netěsnosti
                                if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Release
                                || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease
                                || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.OverchargeStart
                                || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Running
                                || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Neutral       // Vyrovná ztráty vzduchu pro neutrální pozici kontroléru
                                || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Suppression   // Klesne na tlak v potrubí snížený o FullServicePressureDrop 
                                || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.GSelfLapH)    // Postupné odbržďování pro BS2
                                {
                                        lead.BrakeSystem.BrakeLine1PressurePSI += PressureDiffEqualToPipePSI;  // Increase brake pipe pressure to cover loss
                                        lead.MainResPressurePSI = lead.MainResPressurePSI - (PressureDiffEqualToPipePSI * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3);   // Decrease main reservoir pressure
                                    }
                            }
                            // reduce pressure in lead brake line if brake pipe pressure is above equalising pressure - apply brakes
                            else if (lead.BrakeSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg)
                            {
                                float ServiceVariationFactor = (1 - TrainPipeTimeVariationS / (serviceTimeFactor * 2));
                                ServiceVariationFactor = MathHelper.Clamp(ServiceVariationFactor, 0.05f, 1.0f); // Keep factor within acceptable limits - prevent value from going negative
                                lead.BrakeSystem.BrakeLine1PressurePSI *= ServiceVariationFactor;                                
                                if (lead.TrainBrakeController.MaxPressurePSI <= lead.BrakeSystem.maxPressurePSI0) brakePipeTimeFactorS0 = brakePipeTimeFactorS_Apply;
                            }                            

                        train.LeadPipePressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI;  // Keep a record of current train pipe pressure in lead locomotive
                    }
                    
                    // Propogate lead brake line pressure from lead locomotive along the train to each car
                    TrainCar car0 = train.Cars[0];
                    float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                    float brakePipeVolumeM30 = car0.BrakeSystem.BrakePipeVolumeM3;
                    train.TotalTrainBrakePipeVolumeM3 = 0.0f; // initialise train brake pipe volume
                    train.TotalCapacityMainResBrakePipe = 0.0f;

#if DEBUG_TRAIN_PIPE_LEAK

                    Trace.TraceInformation("======================================= Train Pipe Leak (AirSinglePipe) ===============================================");
                    Trace.TraceInformation("Before:  CarID {0}  TrainPipeLeak {1} Lead BrakePipe Pressure {2}", trainCar.CarID, lead.TrainBrakePipeLeakPSIpS, lead.BrakeSystem.BrakeLine1PressurePSI);
                    Trace.TraceInformation("Brake State {0}", lead.TrainBrakeController.TrainBrakeControllerState);
                    Trace.TraceInformation("Main Resevoir {0} Compressor running {1}", lead.MainResPressurePSI, lead.CompressorIsOn);

#endif
                    foreach (TrainCar car in train.Cars)               
                    {
                        // Výpočet objemu potrubí pro každý vůz
                        if (car.BrakeSystem.BrakePipeVolumeM3 == 0) car.BrakeSystem.BrakePipeVolumeM3 = ((0.032f / 2) * (0.032f / 2) * (float)Math.PI) * (2 + car.CarLengthM);

                        // Výpočet celkového objemu potrubí
                        train.TotalTrainBrakePipeVolumeM3 += car.BrakeSystem.BrakePipeVolumeM3;

                        // Výpočet celkové kapacity hlavních jímek
                        train.TotalCapacityMainResBrakePipe += car.BrakeSystem.TotalCapacityMainResBrakePipe;

                        float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                        if (car != train.Cars[0] && car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen && car0.BrakeSystem.AngleCockBOpen)
                        {
                            // Based on the principle of pressure equualization between adjacent cars
                            // First, we define a variable storing the pressure diff between cars, but limited to a maximum flow rate depending on pipe characteristics
                            // The sign in the equation determines the direction of air flow.
                            //float TrainPipePressureDiffPropogationPSI = (p0>p1 ? -1 : 1) * Math.Min(TrainPipeTimeVariationS * Math.Abs(p1 - p0) / brakePipeTimeFactorS, Math.Abs(p1 - p0));

                            float TrainPipePressureDiffPropogationPSI = TrainPipeTimeVariationS * (p1 - p0) / (brakePipeTimeFactorS0);

                            // Air flows from high pressure to low pressure, until pressure is equal in both cars.
                            // Brake pipe volumes of both cars are taken into account, so pressure increase/decrease is proportional to relative volumes.
                            // If TrainPipePressureDiffPropagationPSI equals to p1-p0 the equalization is achieved in one step.
                            car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPropogationPSI * brakePipeVolumeM30 / (brakePipeVolumeM30 + car.BrakeSystem.BrakePipeVolumeM3);
                            car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipePressureDiffPropogationPSI * car.BrakeSystem.BrakePipeVolumeM3 / (brakePipeVolumeM30 + car.BrakeSystem.BrakePipeVolumeM3);
                        }
                        
                        if (!car.BrakeSystem.FrontBrakeHoseConnected)  // Car front brake hose not connected
                        {
                            if (car.BrakeSystem.AngleCockAOpen) //  AND Front brake cock opened
                            {
                                car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * p1 / (brakePipeTimeFactorS * 300);
                                if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                                    car.BrakeSystem.BrakeLine1PressurePSI = 0;
                            }

                            if (car0.BrakeSystem.AngleCockBOpen && car != car0) //  AND Rear cock of wagon opened, and car is not the first wagon
                            {
                                car0.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * p0 / (brakePipeTimeFactorS * 300);
                                if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                                    car.BrakeSystem.BrakeLine1PressurePSI = 0;
                            }
                        }
                        if (car == train.Cars[train.Cars.Count - 1] && car.BrakeSystem.AngleCockBOpen) // Last car in train and rear cock of wagon open
                        {
                            car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * p1 / (brakePipeTimeFactorS * 300);
                            if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                                car.BrakeSystem.BrakeLine1PressurePSI = 0;
                        }
                        p0 = car.BrakeSystem.BrakeLine1PressurePSI;
                        car0 = car;
                        brakePipeVolumeM30 = car0.BrakeSystem.BrakePipeVolumeM3;
                    }
#if DEBUG_TRAIN_PIPE_LEAK
                    Trace.TraceInformation("After: Lead Brake Pressure {0}", lead.BrakeSystem.BrakeLine1PressurePSI);
#endif
                }
            }

            // Propagate main reservoir pipe (2) and engine brake pipe (3) data
            int first = -1;
            int last = -1;
            train.FindLeadLocomotives(ref first, ref last);
            float sumpv = 0;
            float sumv = 0;
            int continuousFromInclusive = 0;
            int continuousToExclusive = train.Cars.Count;

            for (int i = 0; i < train.Cars.Count; i++)
            {
                BrakeSystem brakeSystem = train.Cars[i].BrakeSystem;
                if (i < first && (!train.Cars[i + 1].BrakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockBOpen || !train.Cars[i + 1].BrakeSystem.AngleCockAOpen || !train.Cars[i].BrakeSystem.TwoPipes))
                {
                    if (continuousFromInclusive < i + 1)
                    {
                        sumv = sumpv = 0;
                        continuousFromInclusive = i + 1;
                    }
                    continue;
                }
                if (i > last && i > 0 && (!brakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockAOpen || !train.Cars[i - 1].BrakeSystem.AngleCockBOpen || !train.Cars[i].BrakeSystem.TwoPipes))
                {
                    if (continuousToExclusive > i)
                        continuousToExclusive = i;
                    continue;
                }

                // Collect main reservoir pipe (2) data
                if (first <= i && i <= last || twoPipes && continuousFromInclusive <= i && i < continuousToExclusive)
                {
                    sumv += brakeSystem.BrakePipeVolumeM3;
                    sumpv += brakeSystem.BrakePipeVolumeM3 * brakeSystem.BrakeLine2PressurePSI;
                    var eng = train.Cars[i] as MSTSLocomotive;
                    if (eng != null)
                    {
                        sumv += eng.MainResVolumeM3;
                        sumpv += eng.MainResVolumeM3 * eng.MainResPressurePSI;

                        // Výpočet kapacity hlavní jímky a přilehlého potrubí
                        brakeSystem.TotalCapacityMainResBrakePipe = (brakeSystem.BrakePipeVolumeM3 * brakeSystem.BrakeLine1PressurePSI) + (eng.MainResVolumeM3 * eng.MainResPressurePSI);
                    }
                }
            }

            // Udrží správný tlak v BV pro každou loko
            //lead.BrakeSystem.EB = lead.BrakeSystem.AutoCylPressurePSI1 / lead.BrakeSystem.MCP;                        
            //lead.SetEngineBrakeValue(lead.BrakeSystem.EB);

            if (sumv > 0)
                sumpv /= sumv;

            if (!train.Cars[continuousFromInclusive].BrakeSystem.FrontBrakeHoseConnected && train.Cars[continuousFromInclusive].BrakeSystem.AngleCockAOpen
                || (continuousToExclusive == train.Cars.Count || !train.Cars[continuousToExclusive].BrakeSystem.FrontBrakeHoseConnected) && train.Cars[continuousToExclusive - 1].BrakeSystem.AngleCockBOpen
                 )
                sumpv = 0;

            // Propagate main reservoir pipe (2) data
            train.BrakeLine2PressurePSI = sumpv;
            for (int i = 0; i < train.Cars.Count; i++)
                {
                if (first <= i && i <= last || twoPipes && continuousFromInclusive <= i && i < continuousToExclusive)
                {
                    train.Cars[i].BrakeSystem.BrakeLine2PressurePSI = sumpv;
                    if (sumpv != 0 && train.Cars[i] is MSTSLocomotive)
                        (train.Cars[i] as MSTSLocomotive).MainResPressurePSI = sumpv;
                }
                else
                    train.Cars[i].BrakeSystem.BrakeLine2PressurePSI = train.Cars[i] is MSTSLocomotive ? (train.Cars[i] as MSTSLocomotive).MainResPressurePSI : 0;
            }

            // Collect and propagate engine brake pipe (3) data
            // This appears to be calculating the engine brake cylinder pressure???
                    if (lead != null)
                    {
                        var prevState = lead.EngineBrakeState;

                if (train.BrakeLine3PressurePSI > lead.MainResPressurePSI) train.BrakeLine3PressurePSI = lead.MainResPressurePSI;

                //if (lead.BrakeSystem.AutoCylPressurePSI1 < train.BrakeLine3PressurePSI && train.BrakeLine3PressurePSI < lead.MainResPressurePSI)  // Apply the engine brake as the pressure decreases
                if (lead.BrakeSystem.AutoCylPressurePSI1 < train.BrakeLine3PressurePSI 
                    && lead.MainResPressurePSI > 0 
                    && lead.BrakeSystem.AutoCylPressurePSI0 + lead.BrakeSystem.AutoCylPressurePSI1 < lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                    && lead.BrakeSystem.AutoCylPressurePSI0 + lead.BrakeSystem.AutoCylPressurePSI1 < lead.MainResPressurePSI
                    )  // Apply the engine brake as the pressure decreases
                        {
                    BrakeSystem brakeSystem = train.Cars[0].BrakeSystem;
                    float dp = elapsedClockSeconds * lead.EngineBrakeApplyRatePSIpS;

                    if (lead.BrakeSystem.AutoCylPressurePSI1 + dp > train.BrakeLine3PressurePSI)
                        dp = train.BrakeLine3PressurePSI - lead.BrakeSystem.AutoCylPressurePSI1;

                    if (dp * brakeSystem.GetCylVolumeM3() > lead.MainResPressurePSI * lead.MainResVolumeM3)
                        dp = (lead.MainResPressurePSI * lead.MainResVolumeM3) / brakeSystem.GetCylVolumeM3();

                    if (lead.BrakeSystem.AutoCylPressurePSI0 + lead.BrakeSystem.AutoCylPressurePSI1 + dp > lead.MainResPressurePSI)
                        dp = lead.MainResPressurePSI - lead.BrakeSystem.AutoCylPressurePSI1 - lead.BrakeSystem.AutoCylPressurePSI0;

                    lead.BrakeSystem.AutoCylPressurePSI1 += dp;
                            lead.EngineBrakeState = ValveState.Apply;
                    
                    lead.MainResPressurePSI = lead.MainResPressurePSI - (dp * brakeSystem.GetCylVolumeM3() / lead.MainResVolumeM3);                         
                        }
                else if (lead.BrakeSystem.AutoCylPressurePSI1 > train.BrakeLine3PressurePSI)  // Release the engine brake as the pressure increases in the brake cylinder                
                        {
                    float dp = elapsedClockSeconds * lead.EngineBrakeReleaseRatePSIpS;
                    if (lead.BrakeSystem.AutoCylPressurePSI1 - dp < train.BrakeLine3PressurePSI)
                        dp = lead.BrakeSystem.AutoCylPressurePSI1 - train.BrakeLine3PressurePSI;
                        lead.BrakeSystem.AutoCylPressurePSI1 -= dp;
                            lead.EngineBrakeState = ValveState.Release;
                        }
                        else  // Engine brake does not change
                            lead.EngineBrakeState = ValveState.Lap;
                        if (lead.EngineBrakeState != prevState)
                            switch (lead.EngineBrakeState)
                            {
                                case ValveState.Release: lead.SignalEvent(Event.EngineBrakePressureIncrease); break;
                                case ValveState.Apply: lead.SignalEvent(Event.EngineBrakePressureDecrease); break;
                                case ValveState.Lap: lead.SignalEvent(Event.EngineBrakePressureStoppedChanging); break;
                            }
                    }

                }

        public override float InternalPressure(float realPressure)
        {
            return realPressure;
        }

        public override void SetRetainer(RetainerSetting setting)
        {
            switch (setting)
            {
                case RetainerSetting.Exhaust:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = MaxReleaseRatePSIpS;
                    RetainerDebugState = "EX";
                    break;
                case RetainerSetting.HighPressure:
                    if ((Car as MSTSWagon).RetainerPositions > 0)
                    {
                        RetainerPressureThresholdPSI = 20;
                        ReleaseRatePSIpS = (50 - 20) / 90f;
                        RetainerDebugState = "HP";
                    }
                    break;
                case RetainerSetting.LowPressure:
                    if ((Car as MSTSWagon).RetainerPositions > 3)
                    {
                        RetainerPressureThresholdPSI = 10;
                        ReleaseRatePSIpS = (50 - 10) / 60f;
                        RetainerDebugState = "LP";
                    }
                    else if ((Car as MSTSWagon).RetainerPositions > 0)
                    {
                        RetainerPressureThresholdPSI = 20;
                        ReleaseRatePSIpS = (50 - 20) / 90f;
                        RetainerDebugState = "HP";
                    }
                    break;
                case RetainerSetting.SlowDirect:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = (50 - 10) / 86f;
                    RetainerDebugState = "SD";
                    break;
            }
        }

        public override void SetHandbrakePercent(float percent)
        {
            if (!(Car as MSTSWagon).HandBrakePresent)
            {
                HandbrakePercent = 0;
                return;
            }
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            HandbrakePercent = percent;
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            Car.Train.EqualReservoirPressurePSIorInHg = 90 - (90 - FullServPressurePSI) * percent / 100;
        }

        // used when switching from autopilot to player driven mode, to move from default values to values specific for the trainset
        public void NormalizePressures(float maxPressurePSI)
        {
            if (AuxResPressurePSI > maxPressurePSI) AuxResPressurePSI = maxPressurePSI;
            if (BrakeLine1PressurePSI > maxPressurePSI) BrakeLine1PressurePSI = maxPressurePSI;
            if (EmergResPressurePSI > maxPressurePSI) EmergResPressurePSI = maxPressurePSI;
        }

        public override bool IsBraking()
        {
            if (AutoCylPressurePSI > MaxCylPressurePSI * 0.3)
            return true;
            return false;
        }

        //Corrects MaxCylPressure (e.g 380.eng) when too high
        public override void CorrectMaxCylPressurePSI(MSTSLocomotive loco)
        {
            //if (MaxCylPressurePSI > loco.TrainBrakeController.MaxPressurePSI - MaxCylPressurePSI / AuxCylVolumeRatio)
            //{
            //    MaxCylPressurePSI = loco.TrainBrakeController.MaxPressurePSI * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
            //}
            }
        }
    }
