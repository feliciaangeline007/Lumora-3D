using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace CoinConvoy
{
    public sealed class ReleaseUpdater : MonoBehaviour
    {
        private const string Repository = "feliciaangeline007/Lumora-3D";
        private const string LatestReleaseApi = "https://api.github.com/repos/" + Repository + "/releases/latest";
        private const string DownloadIdKey = "Lumora.ReleaseUpdater.DownloadId";
        private const string PendingReleaseTagKey = "Lumora.ReleaseUpdater.PendingReleaseTag";
        private const string ApkMimeType = "application/vnd.android.package-archive";

        private enum UpdateState
        {
            Checking,
            UpToDate,
            UpdateAvailable,
            Downloading,
            ReadyToInstall,
            Error
        }

        [Serializable]
        private sealed class ReleaseInfo
        {
            public string tag_name;
            public string html_url;
            public bool draft;
            public bool prerelease;
            public ReleaseAsset[] assets;
        }

        [Serializable]
        private sealed class ReleaseAsset
        {
            public string name;
            public string browser_download_url;
        }

        private UpdateState _state = UpdateState.Checking;
        private string _releaseTag = string.Empty;
        private string _releasePageUrl = string.Empty;
        private string _apkUrl = string.Empty;
        private string _message = "Memeriksa pembaruan...";
        private long _downloadId;
        private long _downloadBytes;
        private long _downloadSize;
        private bool _awaitingInstallPermission;

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string pendingTag = PlayerPrefs.GetString(PendingReleaseTagKey, string.Empty);
            bool hasPendingDownload =
                long.TryParse(PlayerPrefs.GetString(DownloadIdKey, string.Empty), out _downloadId) &&
                _downloadId > 0;

            if (hasPendingDownload && IsVersionInstalled(pendingTag))
            {
                ClearPendingDownload();
                StartReleaseCheck();
                return;
            }

            if (hasPendingDownload)
            {
                _releaseTag = pendingTag;
                _state = UpdateState.Downloading;
                _message = "Melanjutkan unduhan pembaruan...";
                StartCoroutine(PollDownload());
                return;
            }

            ClearPendingDownload();
            StartReleaseCheck();
#else
            enabled = false;
#endif
        }

        private void OnGUI()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!enabled) return;

            float width = Mathf.Min(420f, Screen.width - 32f);
            float height = _state == UpdateState.UpdateAvailable || _state == UpdateState.ReadyToInstall
                ? 154f
                : 124f;
            Rect panel = new Rect(Screen.width - width - 20f, 20f, width, height);

            Color oldColor = GUI.color;
            GUI.color = new Color(0.035f, 0.075f, 0.09f, 0.94f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.31f, 0.96f, 0.85f, 0.95f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f), Texture2D.whiteTexture);
            GUI.color = oldColor;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 25f), "PEMBARUAN LUMORA", titleStyle);

            GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.9f, 0.91f) }
            };
            GUI.Label(new Rect(panel.x + 16f, panel.y + 38f, panel.width - 32f, 38f), _message, messageStyle);

            if (_state == UpdateState.Downloading)
            {
                float progress = _downloadSize > 0 ? (float)_downloadBytes / _downloadSize : 0f;
                GUI.Label(new Rect(panel.x + 16f, panel.y + 76f, panel.width - 32f, 20f),
                    _downloadSize > 0 ? $"{progress:P0}" : "Mengunduh...", messageStyle);
                Rect progressBar = new Rect(panel.x + 16f, panel.y + 100f, panel.width - 32f, 10f);
                GUI.color = new Color(0.14f, 0.22f, 0.23f, 1f);
                GUI.DrawTexture(progressBar, Texture2D.whiteTexture);
                GUI.color = new Color(0.31f, 0.96f, 0.85f, 1f);
                GUI.DrawTexture(new Rect(progressBar.x, progressBar.y, progressBar.width * progress, progressBar.height),
                    Texture2D.whiteTexture);
                GUI.color = oldColor;
            }
            else if (_state == UpdateState.UpdateAvailable || _state == UpdateState.ReadyToInstall)
            {
                string buttonText = _state == UpdateState.UpdateAvailable ? "UNDUH UPDATE" : "PASANG UPDATE";
                if (GUI.Button(new Rect(panel.x + 16f, panel.y + 91f, panel.width - 32f, 42f), buttonText))
                {
                    if (_state == UpdateState.UpdateAvailable) BeginDownload();
                    else InstallUpdate();
                }
            }
            else if (_state == UpdateState.Error || _state == UpdateState.UpToDate)
            {
                float buttonWidth = _releasePageUrl.Length > 0 ? (panel.width - 42f) * 0.5f : panel.width - 32f;
                if (GUI.Button(new Rect(panel.x + 16f, panel.y + 78f, buttonWidth, 34f), "CEK LAGI"))
                {
                    StartReleaseCheck();
                }

                if (_releasePageUrl.Length > 0 &&
                    GUI.Button(new Rect(panel.x + 26f + buttonWidth, panel.y + 78f, buttonWidth, 34f), "BUKA RILIS"))
                {
                    Application.OpenURL(_releasePageUrl);
                }
            }
#endif
        }

        private void StartReleaseCheck()
        {
            if (_state == UpdateState.Checking || _state == UpdateState.Downloading) return;
            _state = UpdateState.Checking;
            _message = "Memeriksa versi terbaru di GitHub...";
            StartCoroutine(CheckLatestRelease());
        }

        private IEnumerator CheckLatestRelease()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(LatestReleaseApi))
            {
                request.timeout = 20;
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetError("Tidak bisa memeriksa pembaruan. Periksa koneksi internet lalu coba lagi.",
                        $"HTTP {request.responseCode}: {request.error}");
                    yield break;
                }

                ReleaseInfo release;
                try
                {
                    release = JsonUtility.FromJson<ReleaseInfo>(request.downloadHandler.text);
                }
                catch (ArgumentException exception)
                {
                    SetError("Data rilis GitHub tidak dapat dibaca.", exception.Message);
                    yield break;
                }

                if (release == null || release.draft || release.prerelease ||
                    string.IsNullOrWhiteSpace(release.tag_name))
                {
                    SetError("Rilis stabil terbaru tidak ditemukan di GitHub.", "Respons rilis tidak valid.");
                    yield break;
                }

                _releaseTag = release.tag_name;
                _releasePageUrl = release.html_url ?? string.Empty;

                if (!TryParseVersion(Application.version, out Version installedVersion) ||
                    !TryParseVersion(release.tag_name, out Version releaseVersion))
                {
                    SetError("Format versi tidak valid. Gunakan versi seperti 1.2.3 pada aplikasi dan tag rilis.",
                        $"Versi aplikasi: {Application.version}; tag: {release.tag_name}");
                    yield break;
                }

                if (releaseVersion <= installedVersion)
                {
                    _state = UpdateState.UpToDate;
                    _message = $"Versi {Application.version} sudah yang terbaru.";
                    yield break;
                }

                ReleaseAsset apk = FindApk(release.assets);
                if (apk == null || !Uri.TryCreate(apk.browser_download_url, UriKind.Absolute, out Uri apkUri) ||
                    apkUri.Scheme != Uri.UriSchemeHttps)
                {
                    SetError($"Rilis {release.tag_name} belum memiliki APK Android yang bisa diunduh.",
                        "Tambahkan satu file .apk ke aset GitHub Release.");
                    yield break;
                }

                _apkUrl = apk.browser_download_url;
                _state = UpdateState.UpdateAvailable;
                _message = $"Versi {_releaseTag} tersedia (sekarang {Application.version}).";
            }
        }

        private static ReleaseAsset FindApk(ReleaseAsset[] assets)
        {
            if (assets == null) return null;
            foreach (ReleaseAsset asset in assets)
            {
                if (asset != null && !string.IsNullOrWhiteSpace(asset.name) &&
                    asset.name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }

            return null;
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string trimmed = value.Trim();
            if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed.Substring(1);

            Match match = Regex.Match(trimmed, @"^\d+(?:\.\d+){1,3}");
            return match.Success && Version.TryParse(match.Value, out version);
        }

        private static bool IsVersionInstalled(string releaseTag)
        {
            return TryParseVersion(Application.version, out Version installedVersion) &&
                   TryParseVersion(releaseTag, out Version pendingVersion) &&
                   installedVersion >= pendingVersion;
        }

        private static string SafeFilePart(string value)
        {
            char[] characters = value.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (!char.IsLetterOrDigit(characters[i]) && characters[i] != '.' && characters[i] != '-')
                {
                    characters[i] = '_';
                }
            }

            return new string(characters);
        }

        private void BeginDownload()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_state != UpdateState.UpdateAvailable) return;

            try
            {
                using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
                using (AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", _apkUrl))
                using (AndroidJavaObject activity = GetCurrentActivity())
                using (AndroidJavaObject request = new AndroidJavaObject("android.app.DownloadManager$Request", uri))
                using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
                {
                    request.Call("setTitle", $"Lumora {_releaseTag}");
                    request.Call("setDescription", "Mengunduh pembaruan Lumora");
                    request.Call("setMimeType", ApkMimeType);
                    request.Call("setNotificationVisibility", 1);
                    request.Call("setDestinationInExternalFilesDir", activity,
                        environment.GetStatic<string>("DIRECTORY_DOWNLOADS"),
                        $"Lumora-{SafeFilePart(_releaseTag)}-{Guid.NewGuid():N}.apk");

                    using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "download"))
                    {
                        if (manager == null)
                        {
                            SetError("Layanan unduhan Android tidak tersedia.", "DownloadManager tidak ditemukan.");
                            return;
                        }

                        _downloadId = manager.Call<long>("enqueue", request);
                    }
                }

                PlayerPrefs.SetString(DownloadIdKey, _downloadId.ToString());
                PlayerPrefs.SetString(PendingReleaseTagKey, _releaseTag);
                PlayerPrefs.Save();
                _state = UpdateState.Downloading;
                _message = "Unduhan pembaruan dimulai...";
                StartCoroutine(PollDownload());
            }
            catch (AndroidJavaException exception)
            {
                SetError("Unduhan tidak dapat dimulai di perangkat ini.", exception.Message);
            }
#endif
        }

        private IEnumerator PollDownload()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            while (true)
            {
                if (!TryReadDownload(out int status, out long received, out long total, out string error))
                {
                    FailDownload(error);
                    yield break;
                }

                _downloadBytes = received;
                _downloadSize = total;

                if (status == 8)
                {
                    _state = UpdateState.ReadyToInstall;
                    _message = $"Unduhan versi {_releaseTag} selesai. Siap dipasang.";
                    yield break;
                }

                if (status == 16)
                {
                    FailDownload("Android gagal mengunduh APK. Periksa koneksi dan ruang penyimpanan.");
                    yield break;
                }

                _message = total > 0
                    ? $"Mengunduh versi {_releaseTag}: {received / 1048576f:F1} / {total / 1048576f:F1} MB"
                    : $"Mengunduh versi {_releaseTag}...";
                yield return new WaitForSecondsRealtime(1f);
            }
#else
            yield break;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private bool TryReadDownload(out int status, out long received, out long total, out string error)
        {
            status = 0;
            received = 0;
            total = 0;
            error = string.Empty;

            try
            {
                using (AndroidJavaObject activity = GetCurrentActivity())
                using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "download"))
                using (AndroidJavaObject query = new AndroidJavaObject("android.app.DownloadManager$Query"))
                {
                    query.Call("setFilterById", new long[] { _downloadId });
                    using (AndroidJavaObject cursor = manager.Call<AndroidJavaObject>("query", query))
                    {
                        if (cursor == null)
                        {
                            error = "Unduhan tidak ditemukan pada Download Manager Android.";
                            return false;
                        }

                        try
                        {
                            if (!cursor.Call<bool>("moveToFirst"))
                            {
                                error = "Unduhan tidak ditemukan pada Download Manager Android.";
                                return false;
                            }

                            status = ReadLongColumn(cursor, "status", out long statusValue)
                                ? (int)statusValue
                                : 0;
                            ReadLongColumn(cursor, "bytes_so_far", out received);
                            ReadLongColumn(cursor, "total_size", out total);
                            if (status > 0) return true;

                            error = "Download Manager Android mengembalikan status unduhan yang tidak valid.";
                            return false;
                        }
                        finally
                        {
                            cursor.Call("close");
                        }
                    }
                }
            }
            catch (AndroidJavaException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static bool ReadLongColumn(AndroidJavaObject cursor, string name, out long value)
        {
            int index = cursor.Call<int>("getColumnIndex", name);
            if (index < 0)
            {
                value = 0;
                return false;
            }

            value = cursor.Call<long>("getLong", index);
            return true;
        }

        private static AndroidJavaObject GetCurrentActivity()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
#endif

        private void InstallUpdate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_state != UpdateState.ReadyToInstall) return;

            try
            {
                using (AndroidJavaObject activity = GetCurrentActivity())
                using (AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    int sdkVersion = versionClass.GetStatic<int>("SDK_INT");
                    if (sdkVersion >= 26 && !packageManager.Call<bool>("canRequestPackageInstalls"))
                    {
                        if (!_awaitingInstallPermission) OpenInstallPermissionSettings(activity);
                        _awaitingInstallPermission = true;
                        _message = "Izinkan pemasangan aplikasi dari Lumora di Pengaturan Android.";
                        return;
                    }
                }

                LaunchPackageInstaller();
            }
            catch (AndroidJavaException exception)
            {
                SetError("Installer Android tidak dapat dibuka.", exception.Message);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OpenInstallPermissionSettings(AndroidJavaObject activity)
        {
            using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
            using (AndroidJavaObject packageUri =
                   uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
            using (AndroidJavaObject intent = new AndroidJavaObject(
                       "android.content.Intent", "android.settings.MANAGE_UNKNOWN_APP_SOURCES", packageUri))
            {
                activity.Call("startActivity", intent);
            }
        }

        private void LaunchPackageInstaller()
        {
            using (AndroidJavaObject activity = GetCurrentActivity())
            using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "download"))
            using (AndroidJavaObject apkUri = manager.Call<AndroidJavaObject>("getUriForDownloadedFile", _downloadId))
            {
                if (apkUri == null)
                {
                    SetError("APK unduhan tidak ditemukan. Silakan unduh kembali.", "Download URI tidak tersedia.");
                    return;
                }

                using (AndroidJavaObject intent =
                       new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW"))
                {
                    intent.Call("setDataAndType", apkUri, ApkMimeType);
                    intent.Call("addFlags", 0x10000000);
                    intent.Call("addFlags", 0x00000001);
                    activity.Call("startActivity", intent);
                }
            }
        }
#endif

        private void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!hasFocus || !_awaitingInstallPermission) return;
            _awaitingInstallPermission = false;
            try
            {
                using (AndroidJavaObject activity = GetCurrentActivity())
                using (AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    int sdkVersion = versionClass.GetStatic<int>("SDK_INT");
                    if (sdkVersion < 26 || packageManager.Call<bool>("canRequestPackageInstalls"))
                    {
                        LaunchPackageInstaller();
                    }
                    else
                    {
                        _message = "Izin belum aktif. Aktifkan izinnya, lalu tekan PASANG UPDATE.";
                    }
                }
            }
            catch (AndroidJavaException exception)
            {
                SetError("Status izin pemasangan tidak dapat diperiksa.", exception.Message);
            }
#endif
        }

        private void FailDownload(string reason)
        {
            ClearPendingDownload();
            SetError(reason, reason);
        }

        private static void ClearPendingDownload()
        {
            PlayerPrefs.DeleteKey(DownloadIdKey);
            PlayerPrefs.DeleteKey(PendingReleaseTagKey);
            PlayerPrefs.Save();
        }

        private void SetError(string message, string detail)
        {
            _state = UpdateState.Error;
            _message = message;
            Debug.LogWarning($"[Lumora updater] {message} {detail}", this);
        }
    }
}
