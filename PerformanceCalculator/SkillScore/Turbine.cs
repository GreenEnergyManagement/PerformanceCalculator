namespace PerformanceCalculator
{
    public class Turbine
    {
        public int ReferenceId { get; internal set; }
        public int ScadaId { get; internal set; }

        public Turbine(int referenceId, int scadaId)
        {
            ReferenceId = referenceId;
            ScadaId = scadaId;
        }
    }
}