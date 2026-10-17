// ReSharper disable ArrangeNamespaceBody

using System.Collections.Generic;

namespace Compose.Net.Unity.Samples.Behaviors
{
    internal class CubeDog : MonoBehaviour
    {
        private static readonly UnityEngine.Color Fur = new(0.82f, 0.56f, 0.30f);
        private static readonly UnityEngine.Color DarkFur = new(0.45f, 0.27f, 0.12f);
        private static readonly UnityEngine.Color Belly = new(0.96f, 0.88f, 0.74f);
        private static readonly UnityEngine.Color Black = new(0.05f, 0.05f, 0.05f);
        private static readonly UnityEngine.Color White = new(1f, 1f, 1f);
        private static readonly UnityEngine.Color Collar = new(0.85f, 0.12f, 0.15f);
        private static readonly UnityEngine.Color Gold = new(1f, 0.8f, 0.1f);
        private static readonly UnityEngine.Color Tongue = new(0.95f, 0.45f, 0.55f);

        [SerializeField] private float _wagSpeed = 12f;
        [SerializeField] private float _wagAngle = 35f;

        private readonly Dictionary<UnityEngine.Color, Material> _materials = new();
        private Transform _tail = null!;
        private Transform _head = null!;

        private void Awake()
        {
            Cube("Body", transform, new Vector3(0f, 1f, 0f), new Vector3(0.7f, 0.7f, 1.6f), Fur);
            Cube("Belly", transform, new Vector3(0f, 0.74f, 0f), new Vector3(0.6f, 0.2f, 1.2f), Belly);

            Leg("FrontLeftLeg", new Vector3(-0.22f, 0f, 0.6f));
            Leg("FrontRightLeg", new Vector3(0.22f, 0f, 0.6f));
            Leg("BackLeftLeg", new Vector3(-0.22f, 0f, -0.6f));
            Leg("BackRightLeg", new Vector3(0.22f, 0f, -0.6f));

            Cube("Collar", transform, new Vector3(0f, 1.25f, 0.78f), new Vector3(0.74f, 0.14f, 0.14f), Collar);
            Cube("Tag", transform, new Vector3(0f, 1.12f, 0.87f), new Vector3(0.1f, 0.1f, 0.04f), Gold);

            _head = Pivot("Head", transform, new Vector3(0f, 1.5f, 0.95f));
            Cube("Skull", _head, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), Fur);
            Cube("Snout", _head, new Vector3(0f, -0.1f, 0.42f), new Vector3(0.36f, 0.3f, 0.3f), Belly);
            Cube("Nose", _head, new Vector3(0f, 0f, 0.58f), new Vector3(0.14f, 0.1f, 0.06f), Black);
            Cube("Tongue", _head, new Vector3(0f, -0.28f, 0.48f), new Vector3(0.14f, 0.08f, 0.16f), Tongue);
            Eye("LeftEye", -0.15f);
            Eye("RightEye", 0.15f);
            Cube("LeftEar", _head, new Vector3(-0.34f, 0.05f, -0.05f), new Vector3(0.08f, 0.45f, 0.26f), DarkFur);
            Cube("RightEar", _head, new Vector3(0.34f, 0.05f, -0.05f), new Vector3(0.08f, 0.45f, 0.26f), DarkFur);
            Cube("Spot", _head, new Vector3(0.15f, 0.2f, 0.29f), new Vector3(0.2f, 0.2f, 0.04f), DarkFur);

            _tail = Pivot("Tail", transform, new Vector3(0f, 1.25f, -0.8f));
            _tail.localRotation = Quaternion.Euler(-45f, 0f, 0f);
            Cube("TailBase", _tail, new Vector3(0f, 0f, -0.22f), new Vector3(0.12f, 0.12f, 0.45f), Fur);
            Cube("TailTip", _tail, new Vector3(0f, 0f, -0.5f), new Vector3(0.13f, 0.13f, 0.12f), Belly);
        }

        private void Update()
        {
            var t = Time.time;
            _tail.localRotation = Quaternion.Euler(-45f, Mathf.Sin(t * _wagSpeed) * _wagAngle, 0f);
            _head.localRotation = Quaternion.Euler(Mathf.Sin(t * 2f) * 4f, Mathf.Sin(t * 0.7f) * 15f, Mathf.Sin(t * 1.3f) * 6f);
        }

        private void OnDestroy()
        {
            foreach (var material in _materials.Values)
                Destroy(material);
        }

        private void Leg(string name, Vector3 position)
        {
            Cube(name, transform, position + new Vector3(0f, 0.35f, 0f), new Vector3(0.22f, 0.7f, 0.22f), Fur);
            Cube(name + "Paw", transform, position + new Vector3(0f, 0.05f, 0.03f), new Vector3(0.26f, 0.1f, 0.3f), Belly);
        }

        private void Eye(string name, float x)
        {
            Cube(name, _head, new Vector3(x, 0.1f, 0.3f), new Vector3(0.14f, 0.14f, 0.02f), White);
            Cube(name + "Pupil", _head, new Vector3(x, 0.08f, 0.315f), new Vector3(0.07f, 0.09f, 0.02f), Black);
        }

        private static Transform Pivot(string name, Transform parent, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            return pivot;
        }

        private void Cube(string name, Transform parent, Vector3 position, Vector3 size, UnityEngine.Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = size;
            cube.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial(color);
        }

        private Material GetMaterial(UnityEngine.Color color)
        {
            if (_materials.TryGetValue(color, out var material))
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            _materials[color] = material;
            return material;
        }
    }
}
