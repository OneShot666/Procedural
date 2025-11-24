using System.Collections.Generic;
using UnityEngine;

namespace ToolBuddy {
    [ExecuteAlways]
    public class PlantGenerator : MonoBehaviour {
        [Header("L-System")]
        [SerializeField]
        private string axiom = "F";
        [SerializeField]
        private List<LSystemRule> rules = new() {
            new LSystemRule('F', "F[+F]F[-F]F")
        };
        [SerializeField] [Range(0,6)] private int iterations = 4;

        [Header("Drawing")]
        [SerializeField] private float rotationAngle = 25.0f;
        [SerializeField] private Material material;

        private readonly List<Vector3> _points = new();

        private void OnValidate() {
            string sentence = LSystemEngine.GenerateSentence(axiom, rules, iterations);
            Debug.Log(sentence);
            GeneratePoints(sentence);
        }

        private void OnRenderObject() {
            DrawPoints();
        }

        private void GeneratePoints(string sentence) {
            _points.Clear();

            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;

            Stack<Vector3> positionStack = new();
            Stack<Quaternion> rotationStack = new();

            foreach (char c in sentence)
                switch (c) {
                    case 'F': {
                        Vector3 end = currentPosition + (currentRotation * Vector3.up);
                        AddSegment(currentPosition, end );
                        currentPosition = end;
                        break;
                    }
                    case '+':
                        currentRotation = Quaternion.AngleAxis( rotationAngle, Vector3.forward ) * currentRotation;
                        break;
                    case '-':
                        currentRotation = Quaternion.AngleAxis( -rotationAngle, Vector3.forward ) * currentRotation;
                        break;
                    case '[':
                        positionStack.Push(currentPosition);
                        rotationStack.Push(currentRotation);
                        break;
                    case ']':
                        if (positionStack.Count > 0) {
                            currentPosition = positionStack.Pop();
                            currentRotation = rotationStack.Pop();
                        }
                        break;
                }
        }

        private void AddSegment(Vector3 segmentStart, Vector3 segmentEnd) {
            _points.Add(segmentStart);
            _points.Add(segmentEnd);
        }

        [ContextMenu("Draw points")]
        private void DrawPoints() {
            if (!material || _points.Count < 2) return;

            material.SetPass(0);
            GL.Begin(GL.LINES);

            for (int i = 0; i < _points.Count; i += 2) {
                GL.Vertex(_points[i]);
                GL.Vertex(_points[i + 1]);
            }

            GL.End();
        }
    }
}
