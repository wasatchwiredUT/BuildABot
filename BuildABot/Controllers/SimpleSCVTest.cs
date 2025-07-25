using SC2APIProtocol;
using Units;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Action = SC2APIProtocol.Action;

namespace Controllers
    {
    /// <summary>
    /// Simple test class to verify SCV movement and build commands work.
    /// </summary>
    public class SCVMovementTest
        {
        private Point2D _targetPoint;
        private bool _testStarted = false;
        private bool _movementIssued = false;
        private bool _buildIssued = false;
        private ulong _testSCVTag = 0;

        public SCVMovementTest()
            {
            // Test coordinates - you can change these to your supply depot position
            _targetPoint = new Point2D { X = 139.5f, Y = 40.5f };
            }

        /// <summary>
        /// Run the SCV movement test each frame.
        /// </summary>
        public List<Action> RunTest(ResponseObservation observation, List<Unit> ourUnits)
            {
            List<Action> actions = new List<Action>();

            if (!_testStarted)
                {
                Debug.WriteLine("[SCVTest] Starting SCV movement test...");
                _testStarted = true;
                }

            // Find an SCV to test with
            var testSCV = FindTestSCV(ourUnits);
            if (testSCV == null)
                {
                Debug.WriteLine("[SCVTest] No SCV found for testing");
                return actions;
                }

            // Track the same SCV throughout the test
            if (_testSCVTag == 0)
                {
                _testSCVTag = testSCV.Tag;
                Debug.WriteLine($"[SCVTest] Selected SCV {_testSCVTag} at ({testSCV.UnitType}) for testing");
                }
            else if (testSCV.Tag != _testSCVTag)
                {
                // Find our specific test SCV
                var specificSCV = ourUnits.FirstOrDefault(u => u.Tag == _testSCVTag);
                if (specificSCV != null)
                    {
                    testSCV = new SCV(specificSCV);
                    }
                else
                    {
                    Debug.WriteLine("[SCVTest] Test SCV no longer exists");
                    return actions;
                    }
                }

            // Get the actual Unit for position checking
            var scvUnit = ourUnits.FirstOrDefault(u => u.Tag == _testSCVTag);
            if (scvUnit == null)
                return actions;

            float distanceToTarget = Distance(scvUnit.Pos, _targetPoint);

            // Phase 1: Move to target position
            if (!_movementIssued)
                {
                Debug.WriteLine($"[SCVTest] Ordering SCV {_testSCVTag} to move from ({scvUnit.Pos.X:F1}, {scvUnit.Pos.Y:F1}) to ({_targetPoint.X:F1}, {_targetPoint.Y:F1})");

                var moveAction = testSCV.Move(_targetPoint);
                actions.Add(moveAction);

                Debug.WriteLine($"[SCVTest] Move action created - Unit: {moveAction.ActionRaw.UnitCommand.UnitTags[0]}, Ability: {moveAction.ActionRaw.UnitCommand.AbilityId}, Target: ({moveAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X:F1}, {moveAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y:F1})");

                _movementIssued = true;
                return actions;
                }

            // Monitor movement progress
            if (_movementIssued && !_buildIssued)
                {
                Debug.WriteLine($"[SCVTest] SCV {_testSCVTag} position: ({scvUnit.Pos.X:F1}, {scvUnit.Pos.Y:F1}), distance to target: {distanceToTarget:F1}, orders: {scvUnit.Orders.Count}");

                // If SCV is close enough to target, try building
                if (distanceToTarget < 3.0f)
                    {
                    Debug.WriteLine($"[SCVTest] SCV reached target! Issuing build command...");

                    var buildAction = testSCV.BuildStructure(UnitType.SupplyDepot, _targetPoint);
                    actions.Add(buildAction);

                    Debug.WriteLine($"[SCVTest] Build action created - Unit: {buildAction.ActionRaw.UnitCommand.UnitTags[0]}, Ability: {buildAction.ActionRaw.UnitCommand.AbilityId}, Target: ({buildAction.ActionRaw.UnitCommand.TargetWorldSpacePos.X:F1}, {buildAction.ActionRaw.UnitCommand.TargetWorldSpacePos.Y:F1})");

                    _buildIssued = true;
                    }
                // If SCV is idle and far from target, something went wrong
                else if (scvUnit.Orders.Count == 0 && distanceToTarget > 5.0f)
                    {
                    Debug.WriteLine($"[SCVTest] SCV became idle before reaching target! Re-issuing move command...");
                    _movementIssued = false; // Reset to try again
                    }
                }

            // Monitor build progress
            if (_buildIssued)
                {
                Debug.WriteLine($"[SCVTest] Build phase - SCV position: ({scvUnit.Pos.X:F1}, {scvUnit.Pos.Y:F1}), orders: {scvUnit.Orders.Count}");

                if (scvUnit.Orders.Count > 0)
                    {
                    var currentOrder = scvUnit.Orders[0];
                    Debug.WriteLine($"[SCVTest] Current order: Ability {currentOrder.AbilityId}");
                    }
                }

            return actions;
            }

        /// <summary>
        /// Find an available SCV for testing.
        /// </summary>
        private SCV FindTestSCV(List<Unit> units)
            {
            foreach (var unit in units)
                {
                if ((UnitType)unit.UnitType == UnitType.SCV)
                    {
                    return new SCV(unit);
                    }
                }
            return null;
            }

        /// <summary>
        /// Calculate distance between a Point and Point2D.
        /// </summary>
        private float Distance(Point pos, Point2D target)
            {
            float dx = pos.X - target.X;
            float dy = pos.Y - target.Y;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
            }

        /// <summary>
        /// Reset the test to run again.
        /// </summary>
        public void ResetTest()
            {
            _testStarted = false;
            _movementIssued = false;
            _buildIssued = false;
            _testSCVTag = 0;
            Debug.WriteLine("[SCVTest] Test reset");
            }

        /// <summary>
        /// Set a new target position for testing.
        /// </summary>
        public void SetTargetPosition(float x, float y)
            {
            _targetPoint = new Point2D { X = x, Y = y };
            Debug.WriteLine($"[SCVTest] Target position set to ({x:F1}, {y:F1})");
            }
        }
    }