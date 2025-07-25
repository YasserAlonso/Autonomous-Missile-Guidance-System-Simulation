using AutonomousMissileGuidance.Utils;

namespace AutonomousMissileGuidance.Control
{
    public interface IGuidanceAlgorithm
    {
        double GetControlSignal(MathHelpers.Vector2D missilePosition, 
                               MathHelpers.Vector2D targetPosition, 
                               double missileHeading, 
                               double deltaTime);
        
        string Name { get; }
        void Reset();
    }
} 