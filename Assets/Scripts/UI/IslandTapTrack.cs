using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// World track: soil / tree gesture / tonic flying to stickman.
    /// </summary>
    public class IslandTapTrack : MonoBehaviour
    {
        Transform follow;
        Transform destHint;
        GameObject tile;
        TextMesh label;
        float hideAt;
        bool sticky;
        bool airMode; // don't snap Y to ground
        bool gestureBig;
        bool labelOnly;
        Color trackColor = new Color(0.35f, 1f, 0.4f);

        public static IslandTapTrack Ensure()
        {
            var t = FindFirstObjectByType<IslandTapTrack>();
            if (t != null) return t;
            var go = new GameObject("IslandTapTrack");
            return go.AddComponent<IslandTapTrack>();
        }

        public static void ShowTapHere(Vector3 worldPos, string text = "TAP HERE")
        {
            var t = Ensure();
            t.gestureBig = true;
            t.Begin(worldPos, text, sticky: true, follow: null, dest: null,
                color: new Color(0.35f, 1f, 0.4f), air: false);
            EmptyIslandCameraFocus.FocusSmooth(worldPos + Vector3.up * 0.4f, 12f, 1.5f);
        }

        public static void ShowTapHere(Transform followTarget, string text = "TAP HERE")
        {
            var t = Ensure();
            Vector3 p = followTarget != null ? followTarget.position : Vector3.zero;
            t.gestureBig = true;
            t.Begin(p, text, sticky: true, follow: followTarget, dest: null,
                color: new Color(0.35f, 1f, 0.4f), air: false);
            if (followTarget != null)
                EmptyIslandCameraFocus.FocusSmooth(followTarget.position + Vector3.up * 0.4f, 12f, 1.5f);
        }

        /// <summary>
        /// Gesture teach — no world text at all now (it was cluttering the
        /// screen). The fruit itself glows/pulses; the gesture name ("DOUBLE
        /// TAP", "HOLD", "SWIPE") only appears in the bottom coach bubble.
        /// </summary>
        public static void ShowGestureOnTree(Transform treeOrFruit, string gestureName)
        {
            if (treeOrFruit == null) return;
            FruitGlowHighlight.Show(treeOrFruit);
        }

        /// <summary>Tonic flying tree → stickman — track follows the heal ball.</summary>
        public static void FollowHealToStickman(Transform tonic, Transform stickman)
        {
            if (tonic == null) return;
            var t = Ensure();
            t.Begin(tonic.position, "HEAL →", sticky: true, follow: tonic, dest: stickman,
                color: new Color(1f, 0.85f, 0.25f), air: true);
            if (stickman != null)
                EmptyIslandCameraFocus.FocusSmooth(stickman.position + Vector3.up * 0.8f, 12f, 1.1f);
        }

        public static void ShowPlaced(Vector3 worldPos, string what)
        {
            var t = Ensure();
            t.Begin(worldPos, "PLACED: " + what, sticky: false, follow: null, dest: null,
                color: new Color(1f, 0.85f, 0.25f), air: false);
            t.hideAt = Time.unscaledTime + 4.5f;
            EmptyIslandCoachBar.SetTip("Placed " + what + " — Edit to move on tiles");
            // Slow track to where the object landed
            EmptyIslandCameraFocus.FocusSmooth(worldPos + Vector3.up * 0.55f, 11.5f, 1.45f);
        }

        public static void Hide()
        {
            var t = FindFirstObjectByType<IslandTapTrack>();
            if (t != null) t.Clear();
            FruitGlowHighlight.HideAll();
        }

        /// <summary>Just a floating label above `follow` — no ground tile/ring.</summary>
        void BeginLabelOnly(Transform followTarget, string text)
        {
            sticky = true;
            follow = followTarget;
            destHint = null;
            airMode = true; // skip ground snap — we're floating above the fruit
            labelOnly = true;
            hideAt = float.MaxValue;
            if (tile == null) BuildTile();
            tile.SetActive(true);
            SetFootRingVisible(false);
            // Anchor above the fruit's head, not on top of it — the fruit
            // itself already glows (FruitGlowHighlight), so the label just
            // needs to sit clear of that so both are readable at once.
            tile.transform.position = followTarget.position + Vector3.up * 0.4f;
            if (label != null)
            {
                label.text = text;
                label.characterSize = 0.09f;
                label.fontSize = 52;
                label.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            }
        }

        void SetFootRingVisible(bool visible)
        {
            var foot = tile.transform.Find("Foot");
            var ring = tile.transform.Find("Ring");
            if (foot != null) foot.gameObject.SetActive(visible);
            if (ring != null) ring.gameObject.SetActive(visible);
        }

        void Begin(Vector3 pos, string text, bool sticky, Transform follow, Transform dest,
            Color color, bool air)
        {
            this.sticky = sticky;
            this.follow = follow;
            destHint = dest;
            airMode = air;
            labelOnly = false;
            trackColor = color;
            hideAt = sticky ? float.MaxValue : Time.unscaledTime + 4.5f;
            if (tile == null) BuildTile();
            SetFootRingVisible(true);
            tile.SetActive(true);
            PlaceAt(pos, color);
            if (label != null)
            {
                label.text = text;
                if (gestureBig)
                {
                    label.characterSize = 0.09f;
                    label.fontSize = 52;
                    label.transform.localPosition = new Vector3(0f, 2.1f, 0f);
                }
                else
                {
                    label.characterSize = 0.065f;
                    label.fontSize = 44;
                    label.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                }
            }
        }

        void BuildTile()
        {
            tile = new GameObject("TrackTile");
            tile.transform.SetParent(transform, false);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Foot";
            quad.transform.SetParent(tile.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            quad.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            Object.Destroy(quad.GetComponent<Collider>());
            var mr = quad.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.35f, 1f, 0.4f, 0.55f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.color);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", mat.color * 0.8f);
            mr.sharedMaterial = mat;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(tile.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            ring.transform.localScale = new Vector3(1.35f, 0.02f, 1.35f);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var labelGo = new GameObject("Lbl");
            labelGo.transform.SetParent(tile.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            label = labelGo.AddComponent<TextMesh>();
            label.fontSize = 44;
            label.characterSize = 0.065f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            labelGo.AddComponent<TreeNameBillboard>();
        }

        void PlaceAt(Vector3 pos, Color color)
        {
            if (!airMode)
            {
                pos = EditModeManager.SnapToCoCTile(pos);
                pos.y = 0f;
            }
            tile.transform.position = pos;
            ApplyColor(color);
        }

        void ApplyColor(Color color)
        {
            var mrs = tile.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < mrs.Length; i++)
            {
                var m = mrs[i].material;
                var c = color;
                c.a = 0.55f;
                m.color = c;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 0.85f);
            }
        }

        void Clear()
        {
            follow = null;
            destHint = null;
            sticky = false;
            airMode = false;
            gestureBig = false;
            if (labelOnly) SetFootRingVisible(true);
            labelOnly = false;
            if (tile != null) tile.SetActive(false);
            FruitGlowHighlight.HideAll();
        }

        void LateUpdate()
        {
            if (tile == null || !tile.activeSelf) return;
            if (!sticky && Time.unscaledTime >= hideAt)
            {
                Clear();
                return;
            }

            if (follow != null)
            {
                if (labelOnly)
                {
                    tile.transform.position = follow.position + Vector3.up * 0.4f;
                }
                else
                {
                    Vector3 p = follow.position;
                    if (airMode)
                    {
                        // Marker under the flying tonic, label points to stickman
                        p.y = Mathf.Max(0.05f, p.y - 0.35f);
                        tile.transform.position = p;
                        ApplyColor(trackColor);
                        if (destHint != null && label != null)
                            label.text = "→ HEAL";
                    }
                    else
                    {
                        PlaceAt(p, trackColor);
                    }
                }
            }

            float s = (gestureBig ? 1.4f : 1f) * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f);
            tile.transform.localScale = Vector3.one * s;
        }
    }
}
