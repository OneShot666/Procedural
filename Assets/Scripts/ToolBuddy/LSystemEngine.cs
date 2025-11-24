using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolBuddy {
    public static class LSystemEngine {
        /// <summary>
        /// Generates an L-System sentence by applying rules iteratively.
        /// </summary>
        /// <param name="axiom">The starting string</param>
        /// <param name="rules">List of production rules to apply</param>
        /// <param name="iterations">Number of iterations to perform</param>
        /// <returns>The generated sentence after all iterations</returns>
        public static string GenerateSentence(
            string axiom,
            List<LSystemRule> rules,
            int iterations) {
            StringBuilder stringBuilder = new();
            string currentString = axiom;

            for (int i = 0; i < iterations; i++) {
                foreach (char character in currentString) {
                    LSystemRule rule = rules.FirstOrDefault(r => r.Predecessor == character);

                    stringBuilder.Append(rule != null ? rule.Successor : character.ToString());
                }

                currentString = stringBuilder.ToString();
                stringBuilder.Clear();
            }

            return currentString;
        }
    }
}
