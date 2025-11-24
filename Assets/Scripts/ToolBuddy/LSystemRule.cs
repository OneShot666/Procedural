using System;
using UnityEngine;

namespace ToolBuddy {
    [Serializable] public record LSystemRule(
        char Predecessor,
        string Successor) {
        [field: SerializeField] public char Predecessor { get; set; } = Predecessor;
        [field: SerializeField] public string Successor { get; set; } = Successor;
    }
}
