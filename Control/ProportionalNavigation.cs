using AutonomousMissileGuidance.Utils;
using System;

namespace AutonomousMissileGuidance.Control
{
    public class ProportionalNavigation : IGuidanceAlgorithm
    {
        public double NavigationConstant { get; set; }
        
        private MathHelpers.Vector2D _lastLineOfSight;
        private bool _firstUpdate = true;

        public string Name => "Proportional Navigation";

        public ProportionalNavigation(double navigationConstant = 3.0)
        {
            NavigationConstant = navigationConstant;
            Reset();
        }

        public double GetControlSignal(MathHelpers.Vector2D missilePosition, 
                                     MathHelpers.Vector2D targetPosition, 
                                     double missileHeading, 
                                     double deltaTime)
        {
            MathHelpers.Vector2D lineOfSight = (targetPosition - missilePosition).Normalized;
            
            if (_firstUpdate)
            {
                _lastLineOfSight = lineOfSight;
                _firstUpdate = false;
                return 0;
            }

            double losRate = MathHelpers.AngleDifference(_lastLineOfSight.Angle, lineOfSight.Angle) / deltaTime;
            
            MathHelpers.Vector2D missileVelocity = MathHelpers.Vector2D.FromAngle(missileHeading);
            double closingSpeed = MathHelpers.Vector2D.Dot(-lineOfSight, missileVelocity);
            
            double lateralAcceleration = NavigationConstant * closingSpeed * losRate;
            double control = lateralAcceleration / 100.0;
            
            _lastLineOfSight = lineOfSight;
            
            return MathHelpers.Clamp(control, -Math.PI, Math.PI);
        }

        public void Reset()
        {
            _firstUpdate = true;
            _lastLineOfSight = new MathHelpers.Vector2D(0, 0);
        }
    }
} 