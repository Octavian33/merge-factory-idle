using System.Collections.Generic;
using UnityEngine;

public class WorkerBoard : MonoBehaviour
{
    public readonly List<WorkerUnit> Workers = new();

    private readonly List<Vector3> slotPositions = new();
    private bool[] slotUsed;
    private Sprite workerSprite;
    private ParticleSystem mergeFx;

    public int WorkerCount => Workers.Count;

    public void BuildBoard(int columns, int rows, float spacing)
    {
        slotUsed = new bool[columns * rows];
        slotPositions.Clear();

        var startX = -(columns - 1) * spacing * 0.5f;
        var startY = 2.7f;
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                slotPositions.Add(new Vector3(startX + x * spacing, startY - y * spacing, 0));
            }
        }

        workerSprite = CreateWorkerSprite();
        mergeFx = null;
    }

    public bool HasEmptySlot()
    {
        for (var i = 0; i < slotUsed.Length; i++)
        {
            if (!slotUsed[i]) return true;
        }
        return false;
    }

    public void SpawnAtFirstEmpty(int level)
    {
        for (var i = 0; i < slotUsed.Length; i++)
        {
            if (!slotUsed[i])
            {
                SpawnWorker(level, i);
                return;
            }
        }
    }

    public void RestoreWorkers(List<WorkerSaveData> saved)
    {
        if (saved == null || saved.Count == 0)
        {
            SpawnAtFirstEmpty(1);
            SpawnAtFirstEmpty(1);
            return;
        }

        var spawnedAny = false;
        for (var i = 0; i < saved.Count; i++)
        {
            var slot = saved[i].slotIndex;
            var level = Mathf.Max(1, saved[i].level);
            if (slot >= 0 && slot < slotUsed.Length && !slotUsed[slot])
            {
                SpawnWorker(level, slot);
                spawnedAny = true;
            }
        }

        if (!spawnedAny)
        {
            SpawnAtFirstEmpty(1);
            SpawnAtFirstEmpty(1);
        }
    }

    public void HandleDrop(WorkerUnit dragged)
    {
        var targetSlot = FindClosestSlot(dragged.transform.position);
        var mergeTarget = FindMergeTarget(dragged);

        if (mergeTarget != null)
        {
            Merge(dragged, mergeTarget);
            return;
        }

        if (targetSlot >= 0 && !slotUsed[targetSlot])
        {
            slotUsed[dragged.SlotIndex] = false;
            dragged.SlotIndex = targetSlot;
            slotUsed[targetSlot] = true;
        }

        dragged.SetDropTarget(slotPositions[dragged.SlotIndex]);
    }

    public List<WorkerSaveData> ExportWorkers()
    {
        var list = new List<WorkerSaveData>(Workers.Count);
        for (var i = 0; i < Workers.Count; i++)
        {
            list.Add(new WorkerSaveData { slotIndex = Workers[i].SlotIndex, level = Workers[i].Level });
        }
        return list;
    }

    public int GetHighestLevel()
    {
        var best = 1;
        for (var i = 0; i < Workers.Count; i++)
        {
            if (Workers[i].Level > best) best = Workers[i].Level;
        }
        return best;
    }

    public void ClearBoard()
    {
        for (var i = Workers.Count - 1; i >= 0; i--)
        {
            Destroy(Workers[i].gameObject);
        }

        Workers.Clear();
        for (var i = 0; i < slotUsed.Length; i++) slotUsed[i] = false;
    }

    private WorkerUnit SpawnWorker(int level, int slot)
    {
        var go = new GameObject();
        var unit = go.AddComponent<WorkerUnit>();
        go.transform.position = slotPositions[slot];
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10;
        unit.Initialize(level, slot, workerSprite);
        unit.SetDropTarget(slotPositions[slot]);
        StartCoroutine(SpawnPop(go.transform));

        Workers.Add(unit);
        slotUsed[slot] = true;
        return unit;
    }

    private WorkerUnit FindMergeTarget(WorkerUnit source)
    {
        for (var i = 0; i < Workers.Count; i++)
        {
            var candidate = Workers[i];
            if (candidate == source || candidate.Level != source.Level) continue;

            if (Vector3.Distance(source.transform.position, candidate.transform.position) < 0.75f)
            {
                return candidate;
            }
        }

        return null;
    }

    private void Merge(WorkerUnit a, WorkerUnit b)
    {
        var spawnSlot = b.SlotIndex;
        var nextLevel = b.Level + 1;
        var gm = GameManager.Instance;

        slotUsed[a.SlotIndex] = false;
        slotUsed[b.SlotIndex] = false;
        Workers.Remove(a);
        Workers.Remove(b);

        if (mergeFx != null)
        {
            var p = mergeFx.transform;
            p.position = b.transform.position;
            mergeFx.Play();
        }

        Destroy(a.gameObject);
        Destroy(b.gameObject);
        var mergedUnit = SpawnWorker(nextLevel, spawnSlot);
        if (gm != null)
        {
            gm.NotifyMerged(nextLevel);
        }

        if (gm != null && gm.Economy != null)
        {
            gm.Economy.AddCoins(nextLevel * 3, true, slotPositions[spawnSlot]);
        }
        if (gm != null && gm.Hud != null)
        {
            gm.Hud.SpawnFloatingCoin(slotPositions[spawnSlot] + Vector3.up * 0.5f, "Merge!");
            gm.Hud.SpawnFloatingCoin(slotPositions[spawnSlot] + Vector3.up * 0.86f, $"L{nextLevel}!");
            gm.Hud.ShowToast($"Merged to L{nextLevel}");
        }
        StartCoroutine(MergeEmphasisRoutine(mergedUnit.transform));
    }

    private int FindClosestSlot(Vector3 pos)
    {
        var idx = -1;
        var best = float.MaxValue;
        for (var i = 0; i < slotPositions.Count; i++)
        {
            var dist = Vector3.SqrMagnitude(slotPositions[i] - pos);
            if (dist < best)
            {
                best = dist;
                idx = i;
            }
        }

        return idx;
    }

    private Sprite CreateWorkerSprite()
    {
        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var cols = new Color[size * size];
        for (var i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        var outline = new Color(0.1f, 0.13f, 0.2f, 1f);
        var helmet = new Color(1f, 0.82f, 0.2f, 1f);
        var skin = new Color(0.98f, 0.82f, 0.66f, 1f);
        var vest = new Color(0.3f, 0.7f, 1f, 1f);
        var shirt = new Color(0.18f, 0.38f, 0.72f, 1f);

        FillRect(cols, size, 27, 15, 42, 8, shirt);
        FillRect(cols, size, 24, 23, 48, 26, vest);
        FillRect(cols, size, 34, 32, 28, 6, new Color(1f, 1f, 1f, 0.85f));

        FillCircle(cols, size, 48, 60, 15, skin);
        FillRect(cols, size, 30, 70, 36, 8, helmet);
        FillRect(cols, size, 27, 64, 42, 6, helmet);

        FillRect(cols, size, 40, 60, 4, 2, outline);
        FillRect(cols, size, 52, 60, 4, 2, outline);
        FillRect(cols, size, 44, 53, 8, 2, outline);

        // stronger dark outline around non-transparent pixels for readability on bright backgrounds
        var copy = (Color[])cols.Clone();
        for (var y = 1; y < size - 1; y++)
        {
            for (var x = 1; x < size - 1; x++)
            {
                var idx = y * size + x;
                if (copy[idx].a <= 0f)
                {
                    continue;
                }

                for (var oy = -1; oy <= 1; oy++)
                {
                    for (var ox = -1; ox <= 1; ox++)
                    {
                        var nIdx = (y + oy) * size + (x + ox);
                        if (copy[nIdx].a <= 0f)
                        {
                            cols[nIdx] = new Color(outline.r, outline.g, outline.b, 1f);
                        }
                    }
                }
            }
        }

        // tiny top highlight for more depth without changing style
        FillRect(cols, size, 30, 40, 36, 3, new Color(1f, 1f, 1f, 0.26f));

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), 96f);
    }

    private void FillRect(Color[] cols, int width, int x, int y, int w, int h, Color color)
    {
        for (var yy = y; yy < y + h; yy++)
        {
            for (var xx = x; xx < x + w; xx++)
            {
                if (xx < 0 || yy < 0 || xx >= width || yy >= width)
                {
                    continue;
                }

                cols[yy * width + xx] = color;
            }
        }
    }

    private void FillCircle(Color[] cols, int width, int cx, int cy, int radius, Color color)
    {
        var r2 = radius * radius;
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            for (var x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || y < 0 || x >= width || y >= width)
                {
                    continue;
                }

                var dx = x - cx;
                var dy = y - cy;
                if (dx * dx + dy * dy <= r2)
                {
                    cols[y * width + x] = color;
                }
            }
        }
    }

    private System.Collections.IEnumerator SpawnPop(Transform t)
    {
        if (t == null) yield break;
        var target = t.localScale;
        t.localScale = target * 0.6f;
        var elapsed = 0f;
        while (elapsed < 0.12f)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(t.localScale, target * 1.08f, 0.55f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(t.localScale, target, 0.45f);
            yield return null;
        }
        if (t == null) yield break;
        t.localScale = target;
    }

    private System.Collections.IEnumerator MergeEmphasisRoutine(Transform t)
    {
        if (t == null) yield break;
        var baseScale = t.localScale;
        var peak = baseScale * 1.2f;
        var elapsed = 0f;

        while (elapsed < 0.12f)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(baseScale, peak, Mathf.SmoothStep(0f, 1f, elapsed / 0.12f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(peak, baseScale, Mathf.SmoothStep(0f, 1f, elapsed / 0.15f));
            yield return null;
        }

        if (t == null) yield break;
        t.localScale = baseScale;
    }
}
