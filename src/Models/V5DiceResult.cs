namespace Melpominee.Models
{
    public class V5DiceResult
    {
        public enum RerollType
        {
            None = 0,
            RerollFailures = 1,
            MaximizeCrits = 2,
            AvoidMessy = 3,
        }

        public int Successes { get; set; }
        public int[] DiceResults { get; set; }
        public int[] HungerResults { get; set; }
        public RerollType Reroll { get; set; } = RerollType.None;
        public bool Critical { get; set; } = false;
        public bool MessyCritical { get; set; } = false;
        public bool BestialFailure { get; set; } = false;
        public string SourceUser { get; set; } = "";
        public string SourceUserIcon { get; set; } = "";
    }
}
