using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Sends the results of the psychophysical tasks (Size Discrimination, Shape
/// Stroop) to the Neurolia app, which pays one token for every passed task.
///
/// Results are queued in PlayerPrefs and sent in one deep link:
///   neuroliaclean://task-result?d=&lt;url-encoded JSON&gt;
/// Contract: docs/braineater-tasks.md in the Neurolia repo.
/// </summary>
public static class NeuroliaBridge
{
    public const string TaskShapeSize = "shape-size";
    public const string TaskStroop = "stroop";

    private const string QueueKey = "neurolia_task_queue_v1";
    private const string LinkBase = "neuroliaclean://task-result?d=";
    private const int MaxQueue = 40;
    private const int MaxPerLink = 20;
    // Neurolia rejects payloads over 6000 characters (before URL-encoding).
    private const int MaxPayloadLength = 5500;

    [Serializable]
    private class Item
    {
        public string id;
        public string task;
        public bool passed;
        public string at;
        public int correct;
        public int incorrect;
    }

    [Serializable]
    private class Queue
    {
        public List<Item> items = new List<Item>();
    }

    /// <summary>When true, the queue is sent when a level ends (see LevelTransition).</summary>
    public static bool AutoSendOnLevelEnd = true;

    /// <summary>Adds a finished task to the queue. Does not leave the game.</summary>
    public static void ReportTask(string task, bool passed, int correct, int incorrect)
    {
        Queue queue = Load();

        queue.items.Add(new Item
        {
            id = Guid.NewGuid().ToString("N"),
            task = task,
            passed = passed,
            at = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            correct = Mathf.Clamp(correct, 0, 100),
            incorrect = Mathf.Clamp(incorrect, 0, 100),
        });

        if (queue.items.Count > MaxQueue)
        {
            queue.items.RemoveRange(0, queue.items.Count - MaxQueue);
        }

        Save(queue);
    }

    public static int PendingCount()
    {
        return Load().items.Count;
    }

    /// <summary>
    /// Opens Neurolia with the queued results. The queue is cleared only after
    /// the link was opened, so a failed attempt keeps the results for later.
    /// </summary>
    public static bool Flush()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Queue queue = Load();

        if (queue.items.Count == 0)
        {
            return false;
        }

        int count = Mathf.Min(queue.items.Count, MaxPerLink);
        string json = BuildJson(queue.items, count);

        while (json.Length > MaxPayloadLength && count > 1)
        {
            count--;
            json = BuildJson(queue.items, count);
        }

        try
        {
            Application.OpenURL(LinkBase + Uri.EscapeDataString(json));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Neurolia link failed: " + e.Message);
            return false;
        }

        queue.items.RemoveRange(0, count);
        Save(queue);
        return true;
#else
        // Neurolia only exists on the phone; keep the queue while testing in the editor.
        return false;
#endif
    }

    private static string BuildJson(List<Item> items, int count)
    {
        var sb = new StringBuilder();
        sb.Append("{\"v\":1,\"app\":\"braineater\",\"results\":[");

        for (int i = 0; i < count; i++)
        {
            Item item = items[i];

            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append("{\"id\":\"").Append(item.id)
              .Append("\",\"task\":\"").Append(item.task)
              .Append("\",\"passed\":").Append(item.passed ? "true" : "false")
              .Append(",\"at\":\"").Append(item.at)
              .Append("\",\"correct\":").Append(item.correct)
              .Append(",\"incorrect\":").Append(item.incorrect)
              .Append('}');
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private static Queue Load()
    {
        try
        {
            string raw = PlayerPrefs.GetString(QueueKey, "");

            if (!string.IsNullOrEmpty(raw))
            {
                Queue queue = JsonUtility.FromJson<Queue>(raw);

                if (queue != null && queue.items != null)
                {
                    return queue;
                }
            }
        }
        catch (Exception)
        {
            // Corrupt data: start with an empty queue.
        }

        return new Queue();
    }

    private static void Save(Queue queue)
    {
        PlayerPrefs.SetString(QueueKey, JsonUtility.ToJson(queue));
        PlayerPrefs.Save();
    }
}
