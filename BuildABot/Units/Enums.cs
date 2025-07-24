using System;
using SC2APIProtocol;

namespace Units
{
    /// <summary>
    /// Enumeration of Terran unit and structure types.  The numeric values map directly
    /// to the unit type identifiers exposed by the SC2 protocol (UnitTypeId).  Only
    /// Terran types relevant to this framework are included.
    /// </summary>
    public enum UnitType
    {
        // Worker and combat units
        SCV = (int)SC2APIProtocol.UnitTypeId.Terran_Scv,
        Marine = (int)SC2APIProtocol.UnitTypeId.Terran_Marine,
        Marauder = (int)SC2APIProtocol.UnitTypeId.Terran_Marauder,
        Reaper = (int)SC2APIProtocol.UnitTypeId.Terran_Reaper,
        SiegeTank = (int)SC2APIProtocol.UnitTypeId.Terran_SiegeTank,
        SiegeTankSieged = (int)SC2APIProtocol.UnitTypeId.Terran_SiegeTankSieged,
        Raven = (int)SC2APIProtocol.UnitTypeId.Terran_Raven,
        Medivac = (int)SC2APIProtocol.UnitTypeId.Terran_Medivac,

        // Command and production structures
        CommandCenter = (int)SC2APIProtocol.UnitTypeId.Terran_CommandCenter,
        OrbitalCommand = (int)SC2APIProtocol.UnitTypeId.Terran_OrbitalCommand,
        PlanetaryFortress = (int)SC2APIProtocol.UnitTypeId.Terran_PlanetaryFortress,
        SupplyDepot = (int)SC2APIProtocol.UnitTypeId.Terran_SupplyDepot,
        SupplyDepotLowered = (int)SC2APIProtocol.UnitTypeId.Terran_SupplyDepotLowered,
        Barracks = (int)SC2APIProtocol.UnitTypeId.Terran_Barracks,
        BarracksTechLab = (int)SC2APIProtocol.UnitTypeId.Terran_BarracksTechLab,
        BarracksReactor = (int)SC2APIProtocol.UnitTypeId.Terran_BarracksReactor,
        Factory = (int)SC2APIProtocol.UnitTypeId.Terran_Factory,
        FactoryTechLab = (int)SC2APIProtocol.UnitTypeId.Terran_FactoryTechLab,
        FactoryReactor = (int)SC2APIProtocol.UnitTypeId.Terran_FactoryReactor,
        Starport = (int)SC2APIProtocol.UnitTypeId.Terran_Starport,
        StarportTechLab = (int)SC2APIProtocol.UnitTypeId.Terran_StarportTechLab,
        StarportReactor = (int)SC2APIProtocol.UnitTypeId.Terran_StarportReactor

        // --- Additional Terran units and structures introduced in updated protocol ---
        , Ghost = 50
        , Banshee = 55
        , Battlecruiser = 57
        , Hellion = 53
        , Hellbat = 484 // Hellion morph form
        , Cyclone = 692
        , Thor = 52
        , ThorAP = 691
        , VikingAssault = 34
        , VikingFighter = 35
        , Liberator = 689
        , LiberatorAG = 734
        , WidowMine = 498
        , Bunker = 24
        , GhostAcademy = 26
        , FusionCore = 30
        , SensorTower = 25
        , Armory = 29
        , EngineeringBay = 22
        , MissileTurret = 23
        , Refinery = 20
    }

    /// <summary>
    /// Enumeration of ability identifiers needed by this framework.  These values map
    /// directly to the SC2 protocol ability identifiers (AbilityID).  By using a
    /// strongly typed enum we avoid using magic numbers throughout the code.
    /// </summary>
    public enum AbilityId
    {
        // Training abilities
        Train_SCV = (int)SC2APIProtocol.AbilityID.Train_SCV,
        Train_Marine = (int)SC2APIProtocol.AbilityID.Train_Marine,
        Train_Marauder = (int)SC2APIProtocol.AbilityID.Train_Marauder,
        Train_Reaper = (int)SC2APIProtocol.AbilityID.Train_Reaper,
        Train_SiegeTank = (int)SC2APIProtocol.AbilityID.Train_SiegeTank,
        Train_Medivac = (int)SC2APIProtocol.AbilityID.Train_Medivac,
        Train_Raven = (int)SC2APIProtocol.AbilityID.Train_Raven,

        // Structure build abilities
        Build_SupplyDepot = (int)SC2APIProtocol.AbilityID.Build_SupplyDepot,
        Build_Barracks = (int)SC2APIProtocol.AbilityID.Build_Barracks,
        Build_Factory = (int)SC2APIProtocol.AbilityID.Build_Factory,
        Build_Starport = (int)SC2APIProtocol.AbilityID.Build_Starport,
        Build_CommandCenter = (int)SC2APIProtocol.AbilityID.Build_CommandCenter,

        // Addon build abilities
        Build_BarracksTechLab = (int)SC2APIProtocol.AbilityID.Build_TechLab_Barracks,
        Build_BarracksReactor = (int)SC2APIProtocol.AbilityID.Build_Reactor_Barracks,
        Build_FactoryTechLab = (int)SC2APIProtocol.AbilityID.Build_TechLab_Factory,
        Build_FactoryReactor = (int)SC2APIProtocol.AbilityID.Build_Reactor_Factory,
        Build_StarportTechLab = (int)SC2APIProtocol.AbilityID.Build_TechLab_Starport,
        Build_StarportReactor = (int)SC2APIProtocol.AbilityID.Build_Reactor_Starport,

        // Unit abilities
        Morph_SiegeMode = (int)SC2APIProtocol.AbilityID.Morph_SiegeMode,
        Morph_Unsiege = (int)SC2APIProtocol.AbilityID.Morph_Unsiege,
        Effect_AutoTurret = (int)SC2APIProtocol.AbilityID.Effect_AutoTurret,
        Effect_KD8Charge = (int)SC2APIProtocol.AbilityID.Effect_KD8Charge,
        Effect_Stim_Marine = (int)SC2APIProtocol.AbilityID.Effect_Stim_Marine,
        Morph_SupplyDepot_Lower = (int)SC2APIProtocol.AbilityID.Morph_SupplyDepot_Lower,
        Morph_SupplyDepot_Raise = (int)SC2APIProtocol.AbilityID.Morph_SupplyDepot_Raise

        // --- Additional training abilities for extended Terran roster ---
        , Train_Ghost = 562
        , Train_Hellbat = 596
        , Train_Hellion = 595
        , Train_Cyclone = 597
        , Train_Thor = 594
        , Train_Banshee = 621
        , Train_Battlecruiser = 623
        , Train_Liberator = 626
        , Train_VikingFighter = 624
        , Train_WidowMine = 614

        // Additional structure build abilities
        , Build_Bunker = 324
        , Build_MissileTurret = 323
        , Build_EngineeringBay = 322
        , Build_Armory = 331
        , Build_GhostAcademy = 327
        , Build_SensorTower = 326
        , Build_FusionCore = 333
        , Build_Refinery = 320

        // Morph/transform abilities for new units
        , Morph_Hellbat = 1998
        , Morph_Hellion = 1978
        , Morph_LiberatorAAMode = 2560
        , Morph_LiberatorAGMode = 2558
        , Morph_VikingAssaultMode = 403
        , Morph_VikingFighterMode = 405
        , Morph_ThorExplosiveMode = 2364
        , Morph_ThorHighImpactMode = 2362
        , Morph_PlanetaryFortress = 1450
        , Morph_OrbitalCommand = 1516

        // Behaviour toggles for cloaking
        , Cloak_Banshee = 392
        , Decloak_Banshee = 393
        , Cloak_Ghost = 382
        , Decloak_Ghost = 383

        // Effect abilities for combat and support
        , Effect_LockOn = 2350
        , Effect_YamatoGun = 401
        , Effect_TacticalJump = 2358
        , Effect_EMP = 1628
        , Effect_GhostSnipe = 2714
        , Effect_PointDefenseDrone = 144
        , Effect_HunterSeekerMissile = 169
        , Effect_Salvage = 32
        , Effect_SupplyDrop = 255
        , Effect_Scan = 399
    }
}