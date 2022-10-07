namespace Core.GateChoice
{
    /// <summary>
    /// Set of parameters to be used in Gate Choice algorithm
    /// </summary>
    public class Utilities
    {
        public float _distanceUtility = -0.256f;
        public float _congestionUtility = -0.138f;
        public float _flowExitUtility = -0.045f;  // This value is uncalibrated.

        public float _fltovis = -0.024f;
        public float _fltoinvis = 0.093f;
        public float _visibilityUtility = 0.71f;

        public FieldPotentials fieldPot = new FieldPotentials();

        /// <summary>
        /// Only used in Utilities1
        /// </summary>
        public float _split = 100;

        public Utilities() { }

    }

    public class FieldPotentials
    {

}
