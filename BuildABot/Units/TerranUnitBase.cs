using System.Collections.Generic;
using Action = SC2APIProtocol.Action;
using SC2APIProtocol;

namespace Units
{
    /// <summary>
    /// Base wrapper around the SC2 Unit class.  All Terran units inherit from
    /// this class which provides helper methods for issuing commands and
    /// accessing common properties.  Instances of derived classes should be
    /// created with the corresponding SC2 unit from the observation.
    /// </summary>
    public abstract class TerranUnitBase
    {
        protected Unit Unit;

        protected TerranUnitBase(Unit unit)
        {
            Unit = unit;
        }

        /// <summary>
        /// Returns the tag of the underlying unit.  Tags uniquely identify
        /// individual units in the game.
        /// </summary>
        public ulong Tag
        {
            get { return Unit.Tag; }
        }

        /// <summary>
        /// Returns the unit type id as our UnitType enumeration.
        /// </summary>
        public UnitType UnitType
        {
            get { return (UnitType)Unit.UnitType; }
        }

        /// <summary>
        /// Indicates whether the unit is currently executing any orders.  A unit
        /// with no orders is considered idle and available for new commands.
        /// </summary>
        public bool IsIdle
        {
            get { return Unit.Orders == null || Unit.Orders.Count == 0; }
        }

        /// <summary>
        /// Helper method to create an action targeting this unit with the
        /// specified ability and optional target position.  Commands on a single
        /// unit will be grouped as raw actions when returned from OnFrame.
        /// </summary>
        /// <param name="abilityId">The ability to issue.</param>
        /// <param name="target">Optional target position for abilities that
        /// require a point.</param>
        /// <returns>An Action containing a raw unit command for this unit.</returns>
        protected Action IssueCommand(AbilityId abilityId, Point2D target)
        {
            Action action = new Action();
            action.ActionRaw = new ActionRaw();
            action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
            action.ActionRaw.UnitCommand.AbilityId = (int)abilityId;
            action.ActionRaw.UnitCommand.UnitTags.Add(Unit.Tag);
            if (target != null)
            {
                action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
                action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
                action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
            }
            return action;
        }

        /// <summary>
        /// Overload of IssueCommand without a target.  Convenience method for
        /// abilities that do not take a point.
        /// </summary>
        /// <param name="abilityId">The ability to use.</param>
        /// <returns>An Action containing the command.</returns>
        protected Action IssueCommand(AbilityId abilityId)
        {
            return IssueCommand(abilityId, null);
        }
    }
}