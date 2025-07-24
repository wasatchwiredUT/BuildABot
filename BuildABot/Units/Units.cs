using SC2APIProtocol;
using Action = SC2APIProtocol.Action;
using System.Collections.Generic;

namespace Units
{
    /// <summary>
    /// SCV worker unit.  Provides a method to construct buildings using the
    /// appropriate build ability.  Does not implement resource gathering; that
    /// logic can be handled separately by the bot.
    /// </summary>
    public class SCV : TerranUnitBase
    {
        public SCV(Unit unit) : base(unit) { }

        /// <summary>
        /// Issues a build command for the specified structure at the given
        /// position.  The ability used depends on the structure type.
        /// </summary>
        public Action BuildStructure(UnitType structureType, Point2D position)
        {
            AbilityId ability = AbilityId.Build_CommandCenter;
            switch (structureType)
            {
                case UnitType.SupplyDepot:
                    ability = AbilityId.Build_SupplyDepot;
                    break;
                case UnitType.Barracks:
                    ability = AbilityId.Build_Barracks;
                    break;
                case UnitType.Factory:
                    ability = AbilityId.Build_Factory;
                    break;
                case UnitType.Starport:
                    ability = AbilityId.Build_Starport;
                    break;
                case UnitType.CommandCenter:
                    ability = AbilityId.Build_CommandCenter;
                    break;
                case UnitType.EngineeringBay:
                    ability = AbilityId.Build_EngineeringBay;
                    break;
                case UnitType.MissileTurret:
                    ability = AbilityId.Build_MissileTurret;
                    break;
                case UnitType.Bunker:
                    ability = AbilityId.Build_Bunker;
                    break;
                case UnitType.Armory:
                    ability = AbilityId.Build_Armory;
                    break;
                case UnitType.GhostAcademy:
                    ability = AbilityId.Build_GhostAcademy;
                    break;
                case UnitType.SensorTower:
                    ability = AbilityId.Build_SensorTower;
                    break;
                case UnitType.FusionCore:
                    ability = AbilityId.Build_FusionCore;
                    break;
                case UnitType.Refinery:
                    ability = AbilityId.Build_Refinery;
                    break;
            }
            return IssueCommand(ability, position);
        }
    }

    /// <summary>
    /// Marine combat unit.  Provides a Stim ability method.
    /// </summary>
    public class Marine : TerranUnitBase
    {
        public Marine(Unit unit) : base(unit) { }

        /// <summary>
        /// Uses the Stimpack ability on this marine.  The ability does not
        /// require a target.
        /// </summary>
        public Action Stim()
        {
            return IssueCommand(AbilityId.Effect_Stim_Marine);
        }
    }

    /// <summary>
    /// Marauder combat unit.  Currently no unique active abilities are
    /// implemented.
    /// </summary>
    public class Marauder : TerranUnitBase
    {
        public Marauder(Unit unit) : base(unit) { }
    }

    /// <summary>
    /// Reaper combat unit.  Provides method to throw KD8 Charge (grenade).
    /// </summary>
    public class Reaper : TerranUnitBase
    {
        public Reaper(Unit unit) : base(unit) { }

        /// <summary>
        /// Throws a KD8 charge at the specified location.
        /// </summary>
        public Action ThrowKD8Charge(Point2D target)
        {
            return IssueCommand(AbilityId.Effect_KD8Charge, target);
        }
    }

    /// <summary>
    /// Siege tank unit.  Provides methods to enter and exit siege mode.
    /// </summary>
    public class SiegeTank : TerranUnitBase
    {
        public SiegeTank(Unit unit) : base(unit) { }

        /// <summary>
        /// Returns true if the tank is currently in siege mode.
        /// </summary>
        public bool IsSieged
        {
            get { return UnitType == UnitType.SiegeTankSieged; }
        }

        /// <summary>
        /// Orders the tank to morph into siege mode.
        /// </summary>
        public Action Siege()
        {
            return IssueCommand(AbilityId.Morph_SiegeMode);
        }

        /// <summary>
        /// Orders the tank to revert from siege mode to tank mode.
        /// </summary>
        public Action Unsiege()
        {
            return IssueCommand(AbilityId.Morph_Unsiege);
        }
    }

    /// <summary>
    /// Raven support unit.  Provides method to drop an Auto Turret.
    /// </summary>
    public class Raven : TerranUnitBase
    {
        public Raven(Unit unit) : base(unit) { }

        /// <summary>
        /// Deploys an Auto Turret at the specified location.
        /// </summary>
        public Action DropAutoTurret(Point2D target)
        {
            return IssueCommand(AbilityId.Effect_AutoTurret, target);
        }
    }

    /// <summary>
    /// Medivac support unit.  Currently no active abilities are implemented.
    /// </summary>
    public class Medivac : TerranUnitBase
    {
        public Medivac(Unit unit) : base(unit) { }
    }

    /// <summary>
    /// Supply depot structure.  Provides methods to raise and lower the depot.
    /// </summary>
    public class SupplyDepot : TerranUnitBase
    {
        public SupplyDepot(Unit unit) : base(unit) { }

        public Action Raise()
        {
            return IssueCommand(AbilityId.Morph_SupplyDepot_Raise);
        }

        public Action Lower()
        {
            return IssueCommand(AbilityId.Morph_SupplyDepot_Lower);
        }
    }

    /// <summary>
    /// Ghost unit capable of cloaking and casting EMP or Snipe.  Only cloaking
    /// and EMP are implemented here; targeted snipe is omitted because it
    /// requires unit targeting support not provided by this framework.
    /// </summary>
    public class Ghost : TerranUnitBase
    {
        public Ghost(Unit unit) : base(unit) { }

        /// <summary>
        /// Activates cloaking on the ghost.  Cloaking has a duration and
        /// drains energy over time.
        /// </summary>
        public Action Cloak()
        {
            return IssueCommand(AbilityId.Cloak_Ghost);
        }

        /// <summary>
        /// Deactivates the ghost's cloak.
        /// </summary>
        public Action Decloak()
        {
            return IssueCommand(AbilityId.Decloak_Ghost);
        }

        /// <summary>
        /// Fires an EMP at the specified point.  This drains shields and
        /// energy of enemy units in the radius.
        /// </summary>
        public Action CastEMP(Point2D target)
        {
            return IssueCommand(AbilityId.Effect_EMP, target);
        }
    }

    /// <summary>
    /// Banshee air unit capable of cloaking.  Only cloaking abilities are
    /// exposed.
    /// </summary>
    public class Banshee : TerranUnitBase
    {
        public Banshee(Unit unit) : base(unit) { }

        /// <summary>
        /// Activates the Banshee's cloak.
        /// </summary>
        public Action Cloak()
        {
            return IssueCommand(AbilityId.Cloak_Banshee);
        }

        /// <summary>
        /// Deactivates the Banshee's cloak.
        /// </summary>
        public Action Decloak()
        {
            return IssueCommand(AbilityId.Decloak_Banshee);
        }
    }

    /// <summary>
    /// Battlecruiser capital ship with powerful tactical jump.  The Yamato
    /// Cannon is not implemented because it requires unit targeting.  Only
    /// Tactical Jump is exposed.
    /// </summary>
    public class Battlecruiser : TerranUnitBase
    {
        public Battlecruiser(Unit unit) : base(unit) { }

        /// <summary>
        /// Performs a Tactical Jump to the given position.
        /// </summary>
        public Action TacticalJump(Point2D target)
        {
            return IssueCommand(AbilityId.Effect_TacticalJump, target);
        }
    }

    /// <summary>
    /// Hellion ground vehicle.  Provides methods to morph to and from Hellbat
    /// mode.
    /// </summary>
    public class Hellion : TerranUnitBase
    {
        public Hellion(Unit unit) : base(unit) { }

        /// <summary>
        /// Morphs this Hellion into Hellbat mode.
        /// </summary>
        public Action MorphToHellbat()
        {
            return IssueCommand(AbilityId.Morph_Hellbat);
        }

        /// <summary>
        /// Morphs this Hellbat back into Hellion mode.
        /// </summary>
        public Action MorphToHellion()
        {
            return IssueCommand(AbilityId.Morph_Hellion);
        }
    }

    /// <summary>
    /// Cyclone anti-armor vehicle.  Lock-on ability is not implemented due to
    /// unit targeting requirements.
    /// </summary>
    public class Cyclone : TerranUnitBase
    {
        public Cyclone(Unit unit) : base(unit) { }
    }

    /// <summary>
    /// Thor heavy mechanical unit.  Provides methods to switch between
    /// explosive and high-impact modes.
    /// </summary>
    public class Thor : TerranUnitBase
    {
        public Thor(Unit unit) : base(unit) { }

        /// <summary>
        /// Switches the Thor into high impact mode (anti-air).
        /// </summary>
        public Action SwitchToHighImpactMode()
        {
            return IssueCommand(AbilityId.Morph_ThorHighImpactMode);
        }

        /// <summary>
        /// Switches the Thor into explosive splash mode (anti-ground).
        /// </summary>
        public Action SwitchToExplosiveMode()
        {
            return IssueCommand(AbilityId.Morph_ThorExplosiveMode);
        }
    }

    /// <summary>
    /// Viking transformable air/ground unit.  Provides methods to switch
    /// between assault (ground) and fighter (air) modes.
    /// </summary>
    public class Viking : TerranUnitBase
    {
        public Viking(Unit unit) : base(unit) { }

        public Action SwitchToAssaultMode()
        {
            return IssueCommand(AbilityId.Morph_VikingAssaultMode);
        }

        public Action SwitchToFighterMode()
        {
            return IssueCommand(AbilityId.Morph_VikingFighterMode);
        }
    }

    /// <summary>
    /// Liberator transformable air unit.  Provides methods to siege and
    /// unsiege.  Siege orders the Liberator to deploy an anti-ground mode at
    /// the specified location; unsiege returns it to anti-air mode.
    /// </summary>
    public class Liberator : TerranUnitBase
    {
        public Liberator(Unit unit) : base(unit) { }

        public Action Siege(Point2D target)
        {
            return IssueCommand(AbilityId.Morph_LiberatorAGMode, target);
        }

        public Action Unsiege()
        {
            return IssueCommand(AbilityId.Morph_LiberatorAAMode);
        }
    }

    /// <summary>
    /// Bunker defensive structure.  Allows salvaging to recover resources.
    /// Additional unload commands are omitted for simplicity.
    /// </summary>
    public class Bunker : TerranUnitBase
    {
        public Bunker(Unit unit) : base(unit) { }

        public Action Salvage()
        {
            return IssueCommand(AbilityId.Effect_Salvage);
        }
    }
}