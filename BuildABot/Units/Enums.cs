using System;

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
        SCV = 45,
        Marine = 48,
        Marauder = 51,
        Reaper = 49,
        SiegeTank = 33,
        SiegeTankSieged = 32,
        Raven = 56,
        Medivac = 54,

        // Command and production structures
        CommandCenter = 18,
        OrbitalCommand = 132,
        PlanetaryFortress = 130,
        SupplyDepot = 19,
        SupplyDepotLowered = 47,
        Barracks = 21,
        BarracksTechLab = 37,
        BarracksReactor = 38,
        Factory = 27,
        FactoryTechLab = 39,
        FactoryReactor = 40,
        Starport = 28,
        StarportTechLab = 41,
        StarportReactor = 42,

        // --- Additional Terran units and structures introduced in updated protocol ---
        Ghost = 50,
        Banshee = 55,
        Battlecruiser = 57,
        Hellion = 53,
        Hellbat = 484, // Hellion morph form
        Cyclone = 692,
        Thor = 52,
        ThorAP = 691,
        VikingAssault = 34,
        VikingFighter = 35,
        Liberator = 689,
        LiberatorAG = 734,
        WidowMine = 498,
        Bunker = 24,
        GhostAcademy = 26,
        FusionCore = 30,
        SensorTower = 25,
        Armory = 29,
        EngineeringBay = 22,
        MissileTurret = 23,
        Refinery = 20,

        // Basic Protoss units and structures
        Gateway = 62,
        Zealot = 73,
        Stalker = 74,
        Sentry = 77,
        Adept = 311,
        RoboFacility = 71,
        Observer = 82,
        WarpPrism = 81,
        Immortal = 83,
        Colossus = 4,
        Disruptor = 694,
        Stargate = 67,
        Phoenix = 78,
        VoidRay = 80,
        Oracle = 495,
        Carrier = 79,
        Tempest = 496,

        // Basic Zerg units and structures
        Hatchery = 86,
        Drone = 104,
        Zergling = 105,
        Baneling = 9,
        Roach = 110,
        Hydralisk = 107,
        Mutalisk = 108,
        Infestor = 111,
        Ultralisk = 109,
        Larva = 151,
        SpawningPool = 89,
        HydraliskDen = 91,
        Lurker = 911
    }

    /// <summary>
    /// Enumeration of ability identifiers needed by this framework.  These values map
    /// directly to the SC2 protocol ability identifiers (AbilityID).  By using a
    /// strongly typed enum we avoid using magic numbers throughout the code.
    /// </summary>
    public enum AbilityId
    {
        // Training abilities
        Train_SCV = 524,
        Train_Marine = 560,
        Train_Marauder = 563,
        Train_Reaper = 561,
        Train_SiegeTank = 591,
        Train_Medivac = 620,
        Train_Raven = 622,

        // Structure build abilities
        Build_SupplyDepot = 319,
        Build_Barracks = 321,
        Build_Factory = 328,
        Build_Starport = 329,
        Build_CommandCenter = 318,    

        // Addon build abilities
        Build_BarracksTechLab = 421,
        Build_BarracksReactor = 422,
        Build_FactoryTechLab = 454,
        Build_FactoryReactor = 455,
        Build_StarportTechLab = 487,
        Build_StarportReactor = 488,

        // Unit abilities
        Morph_SiegeMode = 388,
        Morph_Unsiege = 390,
        Effect_AutoTurret = 176,
        Effect_KD8Charge = 2588,
        Effect_Stim_Marine = 380,
        Morph_SupplyDepot_Lower = 556,
        Morph_SupplyDepot_Raise = 558

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