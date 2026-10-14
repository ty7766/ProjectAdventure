using System.IO;
using System.Xml;
using UnityEditor.Android;

/// <summary>
/// Android 빌드 시 unityLibrary 매니페스트에 VIBRATE 권한을 넣는다.
/// (Unity는 Handheld.Vibrate 사용 시에만 자동으로 넣는데, 햅틱은 JNI로 Vibrator를 직접 호출하므로 감지되지 않는다)
/// </summary>
public class AndroidVibratePermission : IPostGenerateGradleAndroidProject
{
    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    private const string Permission = "android.permission.VIBRATE";

    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
        {
            UnityEngine.Debug.LogWarning($"[AndroidVibratePermission] 매니페스트를 찾을 수 없습니다: {manifestPath}");
            return;
        }

        var document = new XmlDocument();
        document.Load(manifestPath);
        XmlElement manifest = document.DocumentElement;
        if (manifest == null)
        {
            return;
        }

        foreach (XmlNode node in manifest.SelectNodes("uses-permission"))
        {
            if (node is XmlElement element && element.GetAttribute("name", AndroidNamespace) == Permission)
            {
                return;
            }
        }

        XmlElement permission = document.CreateElement("uses-permission");
        permission.SetAttribute("name", AndroidNamespace, Permission);
        manifest.PrependChild(permission);
        document.Save(manifestPath);
    }
}
