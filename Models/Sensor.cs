using AutonomousMissileGuidance.Utils;
using System;

namespace AutonomousMissileGuidance.Models
{
    public class Sensor
    {
        public double NoiseLevel { get; set; }
        public double UpdateRate { get; set; }
        public bool NoiseEnabled { get; set; }
        
        private double _lastUpdateTime;
        private MathHelpers.Vector2D _lastMeasurement;
        private readonly Random _random = new Random();

        public Sensor(double noiseLevel = 5.0, double updateRate = 20.0)
        {
            NoiseLevel = noiseLevel;
            UpdateRate = updateRate;
            NoiseEnabled = true;
            _lastUpdateTime = 0;
            _lastMeasurement = new MathHelpers.Vector2D(0, 0);
        }

        public MathHelpers.Vector2D GetTargetPosition(MathHelpers.Vector2D actualPosition, double currentTime)
        {
            if (currentTime - _lastUpdateTime < 1.0 / UpdateRate)
            {
                return _lastMeasurement;
            }

            _lastUpdateTime = currentTime;
            
            if (!NoiseEnabled)
            {
                _lastMeasurement = actualPosition;
                return actualPosition;
            }

            double noiseX = MathHelpers.GaussianNoise(0, NoiseLevel);
            double noiseY = MathHelpers.GaussianNoise(0, NoiseLevel);
            
            _lastMeasurement = new MathHelpers.Vector2D(
                actualPosition.X + noiseX,
                actualPosition.Y + noiseY
            );

            return _lastMeasurement;
        }

        public double GetRange(MathHelpers.Vector2D missilePosition, MathHelpers.Vector2D targetPosition)
        {
            double actualRange = MathHelpers.Distance(missilePosition, targetPosition);
            
            if (!NoiseEnabled)
                return actualRange;

            double rangeNoise = MathHelpers.GaussianNoise(0, NoiseLevel * 0.5);
            return Math.Max(0, actualRange + rangeNoise);
        }

        public double GetBearing(MathHelpers.Vector2D missilePosition, MathHelpers.Vector2D targetPosition)
        {
            MathHelpers.Vector2D toTarget = targetPosition - missilePosition;
            double actualBearing = toTarget.Angle;
            
            if (!NoiseEnabled)
                return actualBearing;

            double bearingNoise = MathHelpers.GaussianNoise(0, NoiseLevel * 0.01);
            return MathHelpers.NormalizeAngle(actualBearing + bearingNoise);
        }
    }
} 