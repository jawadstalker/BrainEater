#if UNITY_EDITOR
using System.IO;
using System.Xml;
using UnityEditor.Android;

/// <summary>
/// Registers the neurolia-braineater:// URL scheme on the main activity of the
/// generated Android project, so Neurolia can detect that Brain Eater is
/// installed (Linking.canOpenURL) without a hand-written AndroidManifest.
/// </summary>
public class NeuroliaManifestPatcher : IPostGenerateGradleAndroidProject
{
    private const string AndroidNs = "http://schemas.android.com/apk/res/android";
    private const string Scheme = "neurolia-braineater";

    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // `path` is the unityLibrary module; the launcher module sits next to it.
        string root = Directory.GetParent(path).FullName;
        string[] manifests =
        {
            Path.Combine(path, "src", "main", "AndroidManifest.xml"),
            Path.Combine(root, "launcher", "src", "main", "AndroidManifest.xml"),
        };

        foreach (string manifest in manifests)
        {
            if (File.Exists(manifest) && Patch(manifest))
            {
                return;
            }
        }

        UnityEngine.Debug.LogWarning(
            "NeuroliaManifestPatcher: no launcher activity found; the neurolia-braineater:// scheme was not added.");
    }

    private static bool Patch(string file)
    {
        var doc = new XmlDocument();
        doc.Load(file);

        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("android", AndroidNs);

        XmlNode launcher = doc.SelectSingleNode(
            "//activity[intent-filter/category[@android:name='android.intent.category.LAUNCHER']]", ns);

        if (launcher == null)
        {
            return false;
        }

        if (launcher.SelectSingleNode($"intent-filter/data[@android:scheme='{Scheme}']", ns) != null)
        {
            return true;
        }

        XmlElement filter = doc.CreateElement("intent-filter");

        filter.AppendChild(Named(doc, "action", "android.intent.action.VIEW"));
        filter.AppendChild(Named(doc, "category", "android.intent.category.DEFAULT"));
        filter.AppendChild(Named(doc, "category", "android.intent.category.BROWSABLE"));

        XmlElement data = doc.CreateElement("data");
        data.SetAttribute("scheme", AndroidNs, Scheme);
        filter.AppendChild(data);

        launcher.AppendChild(filter);
        doc.Save(file);
        return true;
    }

    private static XmlElement Named(XmlDocument doc, string tag, string value)
    {
        XmlElement element = doc.CreateElement(tag);
        element.SetAttribute("name", AndroidNs, value);
        return element;
    }
}
#endif
