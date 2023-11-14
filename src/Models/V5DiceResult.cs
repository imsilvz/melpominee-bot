using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melpominee.Models
{
    public class V5DiceResult
    {
        public int Successes { get; set; }
        public int[] DiceResults { get; set; }
        public int[] HungerResults { get; set; }
        public bool Reroll { get; set; } = false;
        public bool Critical { get; set; } = false;
        public bool MessyCritical { get; set; } = false;
        public bool BestialFailure { get; set; } = false;
    }
}
