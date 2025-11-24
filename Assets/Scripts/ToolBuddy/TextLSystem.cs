using System.Collections.Generic;
using UnityEngine;

namespace ToolBuddy {
    public class TextLSystem : MonoBehaviour {
        [SerializeField]
        private string axiom = "A";

        [SerializeField]
        private List<LSystemRule> rules = new() {
            new LSystemRule( 'A', "AB"),
            new LSystemRule('B',"A")
        };

        [SerializeField, Range(0,10)] private int iterations = 1;

        [ContextMenu(nameof(Generate))]
        private void Generate() {
            string result = LSystemEngine.GenerateSentence(axiom, rules, iterations);
            Debug.Log(result);
        }
    }
}
