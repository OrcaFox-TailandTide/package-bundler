// =============================================================================
//  Package Bundler  -  いつも入れる .unitypackage を 1 つにまとめるエディタ拡張
//                      Bundle the .unitypackage files you always import into one.
// -----------------------------------------------------------------------------
//  置き場所 : Editor フォルダの中（PopTool.uss と同じフォルダ）
//  開き方   : メニュー Tools > Package Bundler
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PackageBundlerTool
{
    // ================================================================== 言語
    public enum PopLang { Ja = 0, En = 1 }

    public class PopL10n
    {
        const string PrefKey = "PopTool.Lang";

        public static PopLang Current
        {
            get { return (PopLang)EditorPrefs.GetInt(PrefKey, (int)PopLang.Ja); }
            set { EditorPrefs.SetInt(PrefKey, (int)value); }
        }

        readonly Dictionary<string, string[]> table;
        readonly List<(TextElement el, string key)> texts = new List<(TextElement, string)>();
        readonly List<(VisualElement el, string key)> tooltips = new List<(VisualElement, string)>();

        public PopL10n(Dictionary<string, string[]> table) { this.table = table; }

        public string Tr(string key)
        {
            string[] v;
            return table.TryGetValue(key, out v) ? v[(int)Current] : key;
        }

        public T Text<T>(T el, string key) where T : TextElement
        {
            texts.Add((el, key));
            el.text = Tr(key);
            return el;
        }

        public T Tip<T>(T el, string key) where T : VisualElement
        {
            tooltips.Add((el, key));
            el.tooltip = Tr(key);
            return el;
        }

        public void Apply()
        {
            foreach (var (el, key) in texts) el.text = Tr(key);
            foreach (var (el, key) in tooltips) el.tooltip = Tr(key);
        }
    }

    internal static class Txt
    {
        public static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
        {
            // ---- 画面
            ["title"]          = new[] { "Package Bundler", "Package Bundler" },
            ["subtitle"]       = new[] { "いつものパッケージを 1 つにまとめる", "Bundle your go-to packages into one" },
            ["tab_build"]      = new[] { "まとめる", "Bundle" },
            ["tab_import"]     = new[] { "インポート", "Import" },
            ["tip_tab_build"]  = new[] { "いつも入れるパッケージを並べて、1 つのパッケージにまとめます", "List the packages you always use and bundle them into one" },
            ["tip_tab_import"] = new[] { "アバターごとに入れる中身をプリセットで選んで、このプロジェクトにインポートします", "Pick contents per avatar with presets and import them into this project" },
            ["sec_packages"]   = new[] { "パッケージ", "Packages" },
            ["sec_settings"]   = new[] { "設定", "Settings" },
            ["sec_preset_make"]= new[] { "プリセット作成", "Create presets" },
            ["log_title"]      = new[] { "前回の結果", "Last result" },

            // まとめる
            ["drop_big"]       = new[] { "ここに .unitypackage か .zip をドラッグ＆ドロップ\nクリックしてファイルを選ぶこともできます", "Drag & drop .unitypackage or .zip files here\nor click to choose a file" },
            ["drop_compact"]   = new[] { "＋ .unitypackage / .zip をドロップ または クリックで追加", "+ Drop .unitypackage / .zip or click to add" },
            ["tip_drop"]       = new[] { "ウィンドウのどこにドロップしても追加できます（BOOTH の zip のままでも OK）。更新版を入れると、差し替えるか聞きます。",
                                         "Drop anywhere in the window to add (BOOTH zip files work too). Dropping a newer version offers to replace the old one." },
            ["missing_suffix"] = new[] { "（ファイルが見つかりません）", " (file not found)" },
            ["newer_label"]    = new[] { "更新版？ {0}", "Newer version? {0}" },
            ["btn_use_newer"]  = new[] { "これに差し替え", "Replace" },
            ["btn_replace"]    = new[] { "差し替え", "Replace" },
            ["tip_enabled"]    = new[] { "まとめに入れる", "Include in the bundle" },
            ["tip_replace"]    = new[] { "別のファイルに差し替え", "Replace with another file" },
            ["tip_remove"]     = new[] { "一覧から外す", "Remove from the list" },
            ["btn_build_new"]  = new[] { "まとめパッケージを作成", "Build bundle" },
            ["btn_build_save"] = new[] { "変更を保存", "Save changes" },
            ["btn_build_done"] = new[] { "保存済み", "Saved" },
            ["tip_drag"]       = new[] { "ドラッグで並べ替え", "Drag to reorder" },
            ["btn_undo_remove"] = new[] { "元に戻す", "Undo" },
            ["tip_undo_remove"] = new[] { "一覧から外す前に戻します。「変更を保存」を押すと外したものは消えます。", "Put it back. Removed items are cleared when you save changes." },
            ["drop_overlay"]   = new[] { "＋ 離して追加", "+ Release to add" },
            ["btn_relocate"]   = new[] { "参照先を変更", "Relink" },
            ["tip_relocate"]   = new[] { "見つからないファイルの参照先を選び直します", "Choose where the missing file is now" },
            ["import_count"]   = new[] { "{0} 個中 {1} 個を入れる", "{1} of {0} packages will be imported" },
            ["badge_done"]     = new[] { "できた！", "Done!" },
            ["st_no_output"]   = new[] { "設定タブで保存先を決めてください", "Set the output path in Settings" },
            ["btn_reveal"]     = new[] { "保存先フォルダを開く", "Open output folder" },

            // プリセット
            ["lbl_preset"]     = new[] { "プリセット", "Preset" },
            ["preset_all"]     = new[] { "すべて", "All" },
            ["tip_preset_add"] = new[] { "新しいプリセットを作る（今のチェックをコピー）", "New preset (copies the current checks)" },
            ["btn_rename"]     = new[] { "名前変更", "Rename" },
            ["tip_preset_del"] = new[] { "このプリセットを削除", "Delete this preset" },
            ["hint_all"]       = new[] { "「すべて」は全部入ります。＋ で自分用のプリセットを作れます。", "\"All\" imports everything. Press + to make your own preset." },
            ["hint_autosave"]  = new[] { "チェックの変更は「{0}」に自動で保存されます。", "Changes are saved to \"{0}\" automatically." },
            ["lbl_new_name"]   = new[] { "新しいプリセットの名前", "New preset name" },
            ["lbl_rename"]     = new[] { "プリセットの名前", "Preset name" },
            ["btn_ok"]         = new[] { "決定", "OK" },
            ["btn_cancel"]     = new[] { "やめる", "Cancel" },
            ["btn_all_on"]     = new[] { "全部オン", "All on" },
            ["btn_all_off"]    = new[] { "全部オフ", "All off" },
            ["no_enabled"]     = new[] { "パッケージがありません。「まとめる」タブで追加してください。", "No packages yet. Add some in the Bundle tab." },
            ["btn_import"]     = new[] { "「{0}」でインポート（{1} 個）", "Import with \"{0}\" ({1})" },

            // 設定
            ["lbl_output"]     = new[] { "まとめの保存先", "Bundle output" },
            ["lbl_watch"]      = new[] { "更新版を探す場所", "Update search folder" },
            ["not_set"]        = new[] { "（未設定）", "(not set)" },
            ["btn_change"]     = new[] { "変更", "Change" },
            ["toggle_self"]    = new[] { "このツール自身もまとめに入れる（新しいプロジェクトでもすぐ使えるように）", "Include this tool in the bundle (ready to use in new projects)" },
            ["toggle_interactive"] = new[] { "インポート前に中身の確認画面を出す", "Show the contents dialog before importing" },

            // ---- ダイアログ
            ["dlg_title"]      = new[] { "Package Bundler", "Package Bundler" },
            ["dlg_ok"]         = new[] { "OK", "OK" },
            ["dlg_cancel"]     = new[] { "キャンセル", "Cancel" },
            ["dlg_btn_remove"] = new[] { "外す", "Remove" },
            ["dlg_self_add"]   = new[] { "まとめパッケージ自身は一覧に追加できません。", "The bundle itself can't be added to the list." },
            ["dlg_other_ver"]  = new[] { "「{0}」は\n「{1}」の別バージョンのようです。\n差し替えますか？", "\"{0}\" looks like another version of\n\"{1}\".\nReplace it?" },
            ["dlg_btn_replace"]= new[] { "差し替える", "Replace" },
            ["dlg_btn_add_sep"]= new[] { "別のパッケージとして追加", "Add as a separate package" },
            ["dlg_missing"]    = new[] { "見つからないファイルがあります。差し替えるか一覧から外してください。\n\n{0}", "Some files are missing. Replace them or remove them from the list.\n\n{0}" },
            ["dlg_out_in_list"]= new[] { "保存先と同じファイルが一覧に入っています。外してから作成してください。", "The output file is in the list. Remove it before building." },
            ["dlg_build_fail"] = new[] { "作成に失敗しました。\n{0}", "Build failed.\n{0}" },
            ["dlg_outdated"]   = new[] { "まとめパッケージが最新ではありません。", "The bundle is not up to date." },
            ["dlg_btn_rebuild"]= new[] { "作り直してからインポート", "Rebuild, then import" },
            ["dlg_btn_anyway"] = new[] { "このままインポート", "Import as is" },
            ["dlg_preset_del"] = new[] { "プリセット「{0}」を削除しますか？", "Delete the preset \"{0}\"?" },
            ["dlg_name_dup"]   = new[] { "同じ名前のプリセットがあります。別の名前にしてください。", "A preset with that name already exists." },
            ["dlg_btn_delete"] = new[] { "削除", "Delete" },
            ["pick_package"]   = new[] { ".unitypackage または .zip を選択", "Select a .unitypackage or .zip" },
            ["zip_none"]       = new[] { "「{0}」の中に .unitypackage が見つかりませんでした。", "No .unitypackage was found in \"{0}\"." },
            ["zip_fail"]       = new[] { "「{0}」を開けませんでした。\n{1}", "Could not open \"{0}\".\n{1}" },
            ["zip_pick_title"] = new[] { "登録するパッケージを選ぶ", "Choose packages to add" },
            ["zip_pick_msg"]   = new[] { "「{0}」には複数の .unitypackage が入っています。登録するものにチェックを付けてください。", "\"{0}\" contains several .unitypackage files. Check the ones to add." },
            ["zip_pick_one"]   = new[] { "「{0}」には複数の .unitypackage が入っています。使うものを 1 つ選んでください。", "\"{0}\" contains several .unitypackage files. Choose one to use." },
            ["btn_add"]        = new[] { "登録する", "Add" },
            ["pick_output"]    = new[] { "まとめパッケージの保存先", "Bundle output path" },
            ["pick_watch"]     = new[] { "更新版を探すフォルダ（ダウンロードフォルダなど）", "Folder to search for updates (e.g. Downloads)" },

            // ---- 進捗・ログ
            ["prog_scan"]      = new[] { "中身を確認中: {0}", "Scanning: {0}" },
            ["prog_write"]     = new[] { "書き出し中: {0}", "Writing: {0}" },
            ["log_done"]       = new[] { "{0} 作成しました（パッケージ {1} 個 → アセット {2} 件（フォルダ含む）、{3:0.0} MB）", "{0} Built ({1} packages -> {2} assets incl. folders, {3:0.0} MB)" },
            ["log_override"]   = new[] { "・{0} の {1} 件を {2} の内容で上書き", "- {1} item(s) from {0} overwritten by {2}" },
            ["log_conflict"]   = new[] { "注意: 同じ場所に別々のファイルがあります（{0} 件）。インポート時に片方の名前が変わることがあります:", "Note: {0} path(s) hold different files. One of them may be renamed on import:" },
            ["log_more"]       = new[] { "    …ほか {0} 件", "    ...and {0} more" },
            ["log_self"]       = new[] { "・Package Bundler 本体も同梱しました", "- Package Bundler itself was included" },
            ["log_failed"]     = new[] { "失敗しました: {0}", "Failed: {0}" },
            ["warn_self_skip"] = new[] { "ツール本体の場所がわからなかったため、同梱をスキップしました。", "Couldn't locate this tool's files, so it was not included." },
            ["warn_cfg_load"]  = new[] { "設定の読み込みに失敗しました: {0}", "Failed to load settings: {0}" },
            ["warn_cfg_save"]  = new[] { "設定の保存に失敗しました: {0}", "Failed to save settings: {0}" },
            ["err_truncated"]  = new[] { "パッケージが途中で切れています", "The package is truncated" },
            ["err_entry_big"]  = new[] { "エントリが大きすぎます", "An entry is too large" },
            ["err_name_long"]  = new[] { "パス名が長すぎます: {0}", "Path is too long: {0}" },
            ["err_file_big"]   = new[] { "ファイルが大きすぎます: {0}", "File is too large: {0}" },
        };

        public static string Tr(string key)
        {
            string[] v;
            return T.TryGetValue(key, out v) ? v[(int)PopL10n.Current] : key;
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(Tr(key), args);
        }
    }

    // ================================================================== 設定
    [Serializable]
    public class BundlerSource
    {
        public string id = "";      // 差し替えても変わらない識別子（プリセット用）
        public string path = "";
        public bool enabled = true; // まとめパッケージに入れるか
        public bool removed;        // ×で外した（保存するまでは「元に戻す」で戻せる）
        public string origin = "";  // zip から取り出したときの元の zip（更新版を探す場所に使う）
        public bool Live { get { return enabled && !removed; } }
    }

    [Serializable]
    public class ImportPreset
    {
        public string id = "";
        public string name = "";
        public List<string> off = new List<string>(); // インポートしない BundlerSource.id
    }

    [Serializable]
    public class BundlerConfig
    {
        public List<BundlerSource> sources = new List<BundlerSource>();
        public List<ImportPreset> presets = new List<ImportPreset>();
        public string outputPath = "";
        public string watchFolder = "";
        public bool includeSelf = true;
        public string lastBuildSignature = "";
        public string lastBuildLog = "";
    }

    public static class BundlerConfigStore
    {
        public static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "UnityPackageBundler", "config.json");
            }
        }

        public static BundlerConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var c = JsonUtility.FromJson<BundlerConfig>(File.ReadAllText(ConfigPath, Encoding.UTF8));
                    if (c != null)
                    {
                        if (c.sources == null) c.sources = new List<BundlerSource>();
                        if (c.presets == null) c.presets = new List<ImportPreset>();
                        foreach (var s in c.sources)
                            if (string.IsNullOrEmpty(s.id)) s.id = Guid.NewGuid().ToString("N");
                        foreach (var p in c.presets)
                        {
                            if (p.off == null) p.off = new List<string>();
                            if (string.IsNullOrEmpty(p.id)) p.id = Guid.NewGuid().ToString("N");
                        }
                        return c;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PackageBundler] " + Txt.F("warn_cfg_load", e.Message));
            }

            var d = new BundlerConfig();
            d.outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "UnityPackageBundler", "MyStarterBundle.unitypackage");
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            d.watchFolder = Directory.Exists(downloads) ? downloads : "";
            return d;
        }

        public static void Save(BundlerConfig c)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, JsonUtility.ToJson(c, true), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PackageBundler] " + Txt.F("warn_cfg_save", e.Message));
            }
        }
    }

    // ================================================================== tar 読み込み
    internal class TarReader
    {
        readonly Stream s;
        readonly byte[] header = new byte[512];
        long remaining, padding;

        public string Name;
        public long Size;
        public char Type;

        public TarReader(Stream s) { this.s = s; }

        public bool Next()
        {
            Skip(remaining + padding);
            remaining = 0; padding = 0;
            string overrideName = null;

            while (true)
            {
                if (!ReadFully(header, 512)) return false;
                if (IsZero(header)) return false;

                string name = Str(header, 0, 100);
                // POSIX ustar ("ustar\0") だけが 345 番地に prefix を持つ。GNU 形式 ("ustar  ") では別の用途
                bool posix = Str(header, 257, 6) == "ustar";
                string prefix = posix ? Str(header, 345, 155) : "";
                long size = ParseSize(header, 124);
                char type = header[156] == 0 ? '0' : (char)header[156];

                remaining = size;
                padding = (512 - size % 512) % 512;

                if (type == 'L' || type == 'x' || type == 'g')
                {
                    byte[] data = ReadData();
                    if (type == 'L') overrideName = Encoding.UTF8.GetString(data).TrimEnd('\0');
                    else if (type == 'x')
                    {
                        var p = PaxPath(data);
                        if (p != null) overrideName = p;
                    }
                    continue;
                }

                Name = overrideName ?? (prefix.Length > 0 ? prefix + "/" + name : name);
                Size = size;
                Type = type;
                return true;
            }
        }

        public byte[] ReadData()
        {
            if (remaining > int.MaxValue) throw new IOException(Txt.Tr("err_entry_big"));
            var buf = new byte[remaining];
            if (!ReadFully(buf, buf.Length)) throw new EndOfStreamException(Txt.Tr("err_truncated"));
            remaining = 0;
            Skip(padding);
            padding = 0;
            return buf;
        }

        public void CopyDataTo(Stream dst)
        {
            var buf = new byte[81920];
            while (remaining > 0)
            {
                int n = s.Read(buf, 0, (int)Math.Min(buf.Length, remaining));
                if (n <= 0) throw new EndOfStreamException(Txt.Tr("err_truncated"));
                dst.Write(buf, 0, n);
                remaining -= n;
            }
            Skip(padding);
            padding = 0;
        }

        void Skip(long count)
        {
            if (count <= 0) return;
            var buf = new byte[8192];
            while (count > 0)
            {
                int n = s.Read(buf, 0, (int)Math.Min(buf.Length, count));
                if (n <= 0) throw new EndOfStreamException(Txt.Tr("err_truncated"));
                count -= n;
            }
        }

        bool ReadFully(byte[] buf, int count)
        {
            int off = 0;
            while (off < count)
            {
                int r = s.Read(buf, off, count - off);
                if (r <= 0) return false;
                off += r;
            }
            return true;
        }

        static bool IsZero(byte[] b)
        {
            for (int i = 0; i < b.Length; i++) if (b[i] != 0) return false;
            return true;
        }

        static string Str(byte[] h, int off, int len)
        {
            int end = off;
            while (end < off + len && h[end] != 0) end++;
            return Encoding.UTF8.GetString(h, off, end - off);
        }

        static long ParseSize(byte[] h, int off)
        {
            if ((h[off] & 0x80) != 0)
            {
                long v = 0;
                for (int i = off + 1; i < off + 12; i++) v = (v << 8) | h[i];
                return v;
            }
            string str = Encoding.ASCII.GetString(h, off, 12).TrimStart(' ', '\0');
            int z = str.IndexOfAny(new[] { '\0', ' ' });
            if (z >= 0) str = str.Substring(0, z);
            return str.Length == 0 ? 0 : Convert.ToInt64(str, 8);
        }

        static string PaxPath(byte[] data)
        {
            int i = 0;
            while (i < data.Length)
            {
                int sp = Array.IndexOf(data, (byte)' ', i);
                if (sp < 0) break;
                int len;
                if (!int.TryParse(Encoding.ASCII.GetString(data, i, sp - i), out len) || len <= 0) break;
                if (i + len > data.Length || i + len - sp - 2 < 0) break;
                string rec = Encoding.UTF8.GetString(data, sp + 1, i + len - sp - 2);
                if (rec.StartsWith("path=")) return rec.Substring(5);
                i += len;
            }
            return null;
        }
    }

    // ================================================================== tar 書き込み
    internal static class TarWriter
    {
        public static void WriteHeader(Stream o, string name, long size)
        {
            WriteHeader(o, name, size, '0', "0000644");
        }

        /// <summary>フォルダ項目（Unity の書き出しと同じく GUID ごとに入れる）</summary>
        public static void WriteDir(Stream o, string guid)
        {
            WriteHeader(o, guid + "/", 0, '5', "0000777");
        }

        static void WriteHeader(Stream o, string name, long size, char type, string mode)
        {
            var h = new byte[512];
            var nb = Encoding.UTF8.GetBytes(name);
            if (nb.Length > 100) throw new IOException(Txt.F("err_name_long", name));
            if (size > 077777777777L) throw new IOException(Txt.F("err_file_big", name));

            Array.Copy(nb, h, nb.Length);
            Put(h, 100, mode);
            Put(h, 108, "0000000");
            Put(h, 116, "0000000");
            Put(h, 124, Convert.ToString(size, 8).PadLeft(11, '0'));
            Put(h, 136, Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 8).PadLeft(11, '0'));
            for (int i = 148; i < 156; i++) h[i] = (byte)' ';
            h[156] = (byte)type;
            Put(h, 257, "ustar  "); // GNU 形式（Unity の書き出しと同じ）。264 は 0 のまま

            int sum = 0;
            for (int i = 0; i < 512; i++) sum += h[i];
            Put(h, 148, Convert.ToString(sum, 8).PadLeft(6, '0'));
            h[154] = 0;
            h[155] = (byte)' ';

            o.Write(h, 0, 512);
        }

        public static void WriteBytes(Stream o, string name, byte[] data)
        {
            WriteHeader(o, name, data.Length);
            o.Write(data, 0, data.Length);
            Pad(o, data.Length);
        }

        public static void Pad(Stream o, long size)
        {
            long p = (512 - size % 512) % 512;
            if (p > 0) o.Write(new byte[p], 0, (int)p);
        }

        public static void Finish(Stream o)
        {
            o.Write(new byte[1024], 0, 1024);
        }

        static void Put(byte[] h, int off, string s)
        {
            var b = Encoding.ASCII.GetBytes(s);
            Array.Copy(b, 0, h, off, b.Length);
        }
    }

    // ================================================================== 合成処理
    public static class Bundler
    {
        public class ExtraAsset
        {
            public string guid;
            public string pathname;
            public byte[] asset; // フォルダなら null
            public byte[] meta;
        }

        static readonly Regex GuidRe = new Regex("^[0-9a-fA-F]{32}$");

        static bool IsFile(char t) { return t == '0' || t == '7'; }

        static bool SplitEntry(string name, out string guid, out string file)
        {
            guid = null; file = null;
            while (name.StartsWith("./")) name = name.Substring(2);
            int slash = name.IndexOf('/');
            if (slash < 0) return false;
            guid = name.Substring(0, slash);
            file = name.Substring(slash + 1);
            if (!GuidRe.IsMatch(guid) || file.Length == 0 || file.Contains("/")) return false;
            guid = guid.ToLowerInvariant();
            return true;
        }

        static Stream OpenPackage(string path)
        {
            return new GZipStream(new BufferedStream(File.OpenRead(path), 1 << 16), CompressionMode.Decompress);
        }

        static string FirstLine(string s)
        {
            int nl = s.IndexOf('\n');
            return (nl >= 0 ? s.Substring(0, nl) : s).Trim();
        }

        /// <summary>sources を順に合成して outputPath に書き出す。後ろのものほど優先。結果ログを返す。</summary>
        public static string Build(IList<string> sources, string outputPath, IList<ExtraAsset> extras, Action<string, float> progress)
        {
            int n = sources.Count;
            var winner = new Dictionary<string, int>();
            var pathOf = new Dictionary<string, string>();
            var hasAsset = new HashSet<string>();
            var overrides = new Dictionary<(int, int), int>();

            // 1 周目: どのパッケージのどのアセットを採用するか決める
            for (int i = 0; i < n; i++)
            {
                if (progress != null) progress(Txt.F("prog_scan", Path.GetFileName(sources[i])), 0.3f * i / n);
                var seen = new HashSet<string>();
                using (var st = OpenPackage(sources[i]))
                {
                    var tr = new TarReader(st);
                    while (tr.Next())
                    {
                        string guid, file;
                        if (!IsFile(tr.Type) || !SplitEntry(tr.Name, out guid, out file)) continue;
                        if (seen.Add(guid))
                        {
                            int prev;
                            if (winner.TryGetValue(guid, out prev) && prev != i)
                            {
                                int c;
                                overrides.TryGetValue((prev, i), out c);
                                overrides[(prev, i)] = c + 1;
                            }
                            winner[guid] = i;
                            hasAsset.Remove(guid);
                        }
                        if (file == "asset") hasAsset.Add(guid);
                        else if (file == "pathname") pathOf[guid] = FirstLine(Encoding.UTF8.GetString(tr.ReadData()));
                    }
                }
            }

            if (extras != null)
            {
                foreach (var x in extras)
                {
                    var g = x.guid.ToLowerInvariant();
                    winner[g] = -1;
                    pathOf[g] = x.pathname;
                    if (x.asset != null) hasAsset.Add(g); else hasAsset.Remove(g);
                }
            }

            var conflicts = winner.Keys
                .Where(g => hasAsset.Contains(g) && pathOf.ContainsKey(g))
                .GroupBy(g => pathOf[g], StringComparer.OrdinalIgnoreCase)
                .Where(gr => gr.Count() > 1)
                .Select(gr => gr.Key)
                .OrderBy(p => p)
                .ToList();

            // 2 周目: 採用したエントリだけを新しいパッケージへコピー
            string tmp = outputPath + ".tmp";
            try
            {
                using (var fs = File.Create(tmp))
                using (var gz = new GZipStream(fs, System.IO.Compression.CompressionLevel.Optimal))
                {
                    var written = new HashSet<string>();
                    for (int i = 0; i < n; i++)
                    {
                        if (progress != null) progress(Txt.F("prog_write", Path.GetFileName(sources[i])), 0.3f + 0.7f * i / n);
                        using (var st = OpenPackage(sources[i]))
                        {
                            var tr = new TarReader(st);
                            while (tr.Next())
                            {
                                string guid, file;
                                if (!IsFile(tr.Type) || !SplitEntry(tr.Name, out guid, out file)) continue;
                                int w;
                                if (!winner.TryGetValue(guid, out w) || w != i) continue;
                                string entry = guid + "/" + file;
                                if (!written.Add(entry)) continue;
                                if (written.Add(guid + "/")) TarWriter.WriteDir(gz, guid);
                                TarWriter.WriteHeader(gz, entry, tr.Size);
                                tr.CopyDataTo(gz);
                                TarWriter.Pad(gz, tr.Size);
                            }
                        }
                    }

                    if (extras != null)
                    {
                        foreach (var x in extras)
                        {
                            var g = x.guid.ToLowerInvariant();
                            TarWriter.WriteDir(gz, g);
                            if (x.asset != null) TarWriter.WriteBytes(gz, g + "/asset", x.asset);
                            TarWriter.WriteBytes(gz, g + "/asset.meta", x.meta);
                            TarWriter.WriteBytes(gz, g + "/pathname", Encoding.UTF8.GetBytes(x.pathname));
                        }
                    }

                    TarWriter.Finish(gz);
                }

                if (File.Exists(outputPath)) File.Delete(outputPath);
                File.Move(tmp, outputPath);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }

            // ログ
            var sb = new StringBuilder();
            long size = new FileInfo(outputPath).Length;
            sb.AppendLine(Txt.F("log_done", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), n, winner.Count, size / 1048576f));
            foreach (var kv in overrides.OrderBy(k => k.Key.Item2).ThenBy(k => k.Key.Item1))
            {
                sb.AppendLine(Txt.F("log_override",
                    Path.GetFileName(sources[kv.Key.Item1]), kv.Value, Path.GetFileName(sources[kv.Key.Item2])));
            }
            if (conflicts.Count > 0)
            {
                sb.AppendLine(Txt.F("log_conflict", conflicts.Count));
                foreach (var c in conflicts.Take(20)) sb.AppendLine("    " + c);
                if (conflicts.Count > 20) sb.AppendLine(Txt.F("log_more", conflicts.Count - 20));
            }
            if (extras != null && extras.Count > 0) sb.AppendLine(Txt.Tr("log_self"));
            return sb.ToString().TrimEnd();
        }
    }

    // ================================================================== バージョン判定
    // ================================================================== zip の中の .unitypackage
    /// <summary>
    /// BOOTH の zip を解凍せずに扱う。zip 内のファイルは「zipのパス::中のパス」という文字列で表す。
    /// 取り出したものは %LOCALAPPDATA%\UnityPackageBundler\FromZip に置き、使われなくなったら消す。
    /// </summary>
    internal static class ZipUtil
    {
        public const string Sep = "::";

        public static bool IsZip(string p) { return p != null && p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && !p.Contains(Sep); }
        public static bool IsRef(string p) { return p != null && p.Contains(Sep); }
        public static string ZipOf(string r) { return r.Substring(0, r.IndexOf(Sep, StringComparison.Ordinal)); }
        public static string EntryOf(string r) { return r.Substring(r.IndexOf(Sep, StringComparison.Ordinal) + Sep.Length); }

        public static string ExtractRoot
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "UnityPackageBundler", "FromZip");
            }
        }

        // BOOTH の zip は日本語名が Shift_JIS のことがある（UTF-8 の印があるものは自動で UTF-8 になる）
        static Encoding NameEncoding()
        {
            try { return Encoding.GetEncoding(932); } catch (Exception) { return null; }
        }

        static ZipArchive Open(Stream fs)
        {
            var enc = NameEncoding();
            return enc != null ? new ZipArchive(fs, ZipArchiveMode.Read, false, enc) : new ZipArchive(fs, ZipArchiveMode.Read);
        }

        // 中身の一覧はファイルの更新日時ごとに覚えておく（ウィンドウを選ぶたびに読み直さない）
        static readonly Dictionary<string, KeyValuePair<DateTime, List<string>>> cache =
            new Dictionary<string, KeyValuePair<DateTime, List<string>>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>zip 内の .unitypackage を「zip::中のパス」の形で返す。開けなければ例外。</summary>
        public static List<string> ListPackages(string zip)
        {
            var t = File.GetLastWriteTimeUtc(zip);
            KeyValuePair<DateTime, List<string>> hit;
            lock (cache) { if (cache.TryGetValue(zip, out hit) && hit.Key == t) return hit.Value; }

            var list = new List<string>();
            using (var fs = File.OpenRead(zip))
            using (var za = Open(fs))
            {
                foreach (var e in za.Entries)
                {
                    if (e.FullName.EndsWith("/")) continue;
                    if (e.FullName.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase)) continue;
                    if (e.Name.StartsWith("._")) continue;
                    if (e.FullName.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                        list.Add(zip + Sep + e.FullName);
                }
            }
            lock (cache) { cache[zip] = new KeyValuePair<DateTime, List<string>>(t, list); }
            return list;
        }

        /// <summary>zip 内の 1 つを取り出して、そのファイルのパスを返す。</summary>
        public static string Extract(string zipRef)
        {
            string zip = ZipOf(zipRef), entryName = EntryOf(zipRef);
            var dir = Path.Combine(ExtractRoot, Safe(Path.GetFileNameWithoutExtension(zip)));
            Directory.CreateDirectory(dir);
            var dst = Path.Combine(dir, Safe(Path.GetFileName(entryName.Replace('\\', '/'))));
            using (var fs = File.OpenRead(zip))
            using (var za = Open(fs))
            {
                var e = za.Entries.FirstOrDefault(x => x.FullName == entryName);
                if (e == null) throw new FileNotFoundException(entryName);
                using (var src = e.Open())
                using (var outFs = File.Create(dst))
                    src.CopyTo(outFs);
            }
            File.SetLastWriteTimeUtc(dst, File.GetLastWriteTimeUtc(zip));
            return dst;
        }

        static string Safe(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        public static bool IsExtracted(string path)
        {
            try { return Path.GetFullPath(path).StartsWith(Path.GetFullPath(ExtractRoot), StringComparison.OrdinalIgnoreCase); }
            catch (Exception) { return false; }
        }

        /// <summary>取り出したファイルのうち、もう一覧にないものを消す。</summary>
        public static void Cleanup(IEnumerable<string> inUse)
        {
            try
            {
                if (!Directory.Exists(ExtractRoot)) return;
                var keep = new HashSet<string>(inUse.Where(p => !string.IsNullOrEmpty(p)).Select(p =>
                {
                    try { return Path.GetFullPath(p); } catch (Exception) { return p; }
                }), StringComparer.OrdinalIgnoreCase);
                foreach (var f in Directory.GetFiles(ExtractRoot, "*.unitypackage", SearchOption.AllDirectories))
                    if (!keep.Contains(Path.GetFullPath(f))) File.Delete(f);
                foreach (var d in Directory.GetDirectories(ExtractRoot))
                    if (!Directory.EnumerateFileSystemEntries(d).Any()) Directory.Delete(d);
            }
            catch (Exception) { /* 消せなくても動作には影響しない */ }
        }

        public static DateTime TimeOf(string p)
        {
            var real = IsRef(p) ? ZipOf(p) : p;
            return File.Exists(real) ? File.GetLastWriteTimeUtc(real) : DateTime.MinValue;
        }
    }

    // ================================================================== zip に複数入っていたときの選択画面
    internal class ZipPickWindow : EditorWindow
    {
        List<string> items;
        bool[] on;
        bool single;
        int chosen;
        string message;
        Action<List<string>> done;
        Vector2 scroll;

        public static void Show(string zip, List<string> refs, bool single, Action<List<string>> done)
        {
            var w = CreateInstance<ZipPickWindow>();
            w.titleContent = new GUIContent(Txt.Tr("zip_pick_title"));
            w.items = refs;
            w.on = new bool[refs.Count];
            w.single = single;
            w.message = Txt.F(single ? "zip_pick_one" : "zip_pick_msg", Path.GetFileName(zip));
            w.done = done;
            w.minSize = new Vector2(420, 160 + 22 * Math.Min(refs.Count, 10));
            w.maxSize = new Vector2(900, 900);
            w.ShowUtility();
        }

        void OnGUI()
        {
            if (items == null) { Close(); return; }
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(4);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < items.Count; i++)
            {
                var label = Path.GetFileName(ZipUtil.EntryOf(items[i]).Replace('\\', '/'));
                if (single)
                {
                    if (EditorGUILayout.ToggleLeft(label, chosen == i)) chosen = i;
                }
                else on[i] = EditorGUILayout.ToggleLeft(label, on[i]);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Txt.Tr("dlg_cancel"), GUILayout.Width(110))) { done = null; Close(); }
                GUI.enabled = single || on.Any(x => x);
                if (GUILayout.Button(Txt.Tr("btn_add"), GUILayout.Width(110)))
                {
                    var picked = single ? new List<string> { items[chosen] }
                                        : items.Where((x, i) => on[i]).ToList();
                    var cb = done; done = null;
                    Close();
                    if (cb != null) cb(picked);
                }
                GUI.enabled = true;
            }
            EditorGUILayout.Space(6);
        }
    }

    internal static class VersionUtil
    {
        static string Clean(string path)
        {
            if (ZipUtil.IsRef(path)) path = ZipUtil.EntryOf(path).Replace('\\', '/');
            var n = Path.GetFileNameWithoutExtension(path ?? "");
            return Regex.Replace(n, @"\s*\(\d+\)$", "");
        }

        public static string Stem(string path)
        {
            var n = Clean(path).ToLowerInvariant();
            n = Regex.Replace(n, @"v?\d+", "");
            n = Regex.Replace(n, @"[\s_\-.\[\]()（）]+", "");
            return n;
        }

        static int[] Version(string path)
        {
            return Regex.Matches(Clean(path), @"\d+").Cast<Match>()
                .Select(m => { int v; return int.TryParse(m.Value, out v) ? v : 0; })
                .ToArray();
        }

        static int Compare(int[] a, int[] b)
        {
            int len = Math.Max(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                int x = i < a.Length ? a[i] : 0;
                int y = i < b.Length ? b[i] : 0;
                if (x != y) return x.CompareTo(y);
            }
            return 0;
        }

        public static bool IsNewer(string candidate, string current)
        {
            var a = Version(candidate);
            var b = Version(current);
            if (a.Length > 0 && b.Length > 0) return Compare(a, b) > 0;
            var ta = ZipUtil.TimeOf(candidate);
            var tb = ZipUtil.TimeOf(current);
            return ta > tb;
        }
    }

    // ================================================================== ウィンドウ
    public class UnityPackageBundler : EditorWindow
    {
        /// <summary>ツールのバージョン（配布パッケージのファイル名・README もここから作る）</summary>
        public const string Version = "0.0.0";

        const string PrefTab = "PackageBundler.Tab";
        const string PrefInteractive = "PackageBundler.InteractiveImport";
        const string PrefShowLog = "PackageBundler.ShowLog";
        static string PrefPreset { get { return "PackageBundler.Preset." + Application.dataPath; } }

        BundlerConfig cfg;
        PopL10n l10n;
        readonly Dictionary<string, string> newer = new Dictionary<string, string>();
        bool interactiveImport = true;
        string selectedPresetId = ""; // "" = すべて（組み込み）
        int currentTab;
        bool showLog;
        bool renaming;
        int dragFrom = -1, dragTo = -1;

        // タブ
        Button[] tabButtons;
        VisualElement[] pages;
        VisualElement[] footers;

        // まとめる
        VisualElement sourcesBox, logSection;
        Label dropZone, logLabel, badge, importCountLabel;
        VisualElement dropOverlay;
        Button buildButton, revealButton, logHeader;

        // プリセット
        DropdownField presetDropdown;
        VisualElement presetChecks, nameRow, allRow;
        Label nameLabel, presetHint;
        TextField nameField;
        Button renameButton, deleteButton, importButton, allOnButton, allOffButton;

        // 設定
        Label outputPathLabel, watchPathLabel;
        Toggle selfToggle, interactiveToggle;

        [MenuItem("Tools/Package Bundler")]
        static void Open()
        {
            var w = GetWindow<UnityPackageBundler>("Package Bundler");
            w.minSize = new Vector2(400, 380);
        }

        void OnEnable()
        {
            cfg = BundlerConfigStore.Load();
            interactiveImport = EditorPrefs.GetBool(PrefInteractive, true);
            selectedPresetId = EditorPrefs.GetString(PrefPreset, "");
            showLog = EditorPrefs.GetBool(PrefShowLog, false);
            EditorApplication.update += PollScan;
        }

        void OnDisable()
        {
            EditorApplication.update -= PollScan;
        }

        void OnFocus()
        {
            // 別の Unity で一覧を変えた場合にも追従する（名前入力中は作り直さない）
            if (renaming || (nameRow != null && nameRow.style.display == DisplayStyle.Flex)) return;
            cfg = BundlerConfigStore.Load();
            if (sourcesBox != null) RefreshAll();
        }

        void Save() { BundlerConfigStore.Save(cfg); }

        /// <summary>VCC（VPM パッケージ）として Packages/ に入っているか</summary>
        bool InstalledAsPackage
        {
            get
            {
                var script = MonoScript.FromScriptableObject(this);
                var path = script != null ? AssetDatabase.GetAssetPath(script) : "";
                return path.StartsWith("Packages/");
            }
        }

        ImportPreset SelectedPreset
        {
            get { return string.IsNullOrEmpty(selectedPresetId) ? null : cfg.presets.FirstOrDefault(p => p.id == selectedPresetId); }
        }

        void SelectPreset(string id)
        {
            selectedPresetId = id ?? "";
            EditorPrefs.SetString(PrefPreset, selectedPresetId);
        }

        // -------------------------------------------------------------- 画面づくり
        void CreateGUI()
        {
            if (cfg == null) cfg = BundlerConfigStore.Load();
            l10n = new PopL10n(Txt.T);

            var root = rootVisualElement;
            var style = LoadStyle();
            if (style != null) root.styleSheets.Add(style);
            root.AddToClassList("pop-root");
            ApplyJapaneseFont(root);

            // ヘッダー
            var header = new VisualElement();
            header.AddToClassList("pop-header");
            var titleCol = new VisualElement();
            titleCol.AddToClassList("pop-title-col");
            var titleRow = Row();
            var title = l10n.Text(new Label(), "title");
            title.AddToClassList("pop-title");
            titleRow.Add(title);
            var ver = new Label("v" + Version);
            ver.AddToClassList("pop-version");
            titleRow.Add(ver);
            var subtitle = l10n.Text(new Label(), "subtitle");
            subtitle.AddToClassList("pop-subtitle");
            titleCol.Add(titleRow);
            titleCol.Add(subtitle);
            header.Add(titleCol);
            header.Add(BuildLangSwitch());
            root.Add(header);

            // タブ
            var bar = new VisualElement();
            bar.AddToClassList("pop-tabs");
            string[] tabKeys = { "tab_build", "tab_import" };
            string[] tipKeys = { "tip_tab_build", "tip_tab_import" };
            tabButtons = new Button[tabKeys.Length];
            for (int i = 0; i < tabKeys.Length; i++)
            {
                int idx = i;
                var b = l10n.Tip(l10n.Text(new Button(() => SelectTab(idx)), tabKeys[i]), tipKeys[i]);
                b.AddToClassList("pop-tab");
                bar.Add(b);
                tabButtons[i] = b;
            }
            root.Add(bar);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("pop-scroll");
            root.Add(scroll);

            pages = new[] { BuildPageBundle(), BuildPagePresets() };
            foreach (var p in pages) scroll.Add(p);

            footers = new[] { BuildFooterBundle(), BuildFooterPresets() };
            foreach (var f in footers) root.Add(f);

            // 一番下: 前回の結果（開閉できる）
            root.Add(BuildLogSection());

            // ウィンドウのどこにドロップしても追加できる。ドラッグ中は全体を半透明で覆う
            dropOverlay = new VisualElement();
            dropOverlay.AddToClassList("pop-drop-overlay");
            dropOverlay.pickingMode = PickingMode.Ignore;
            var dropPill = l10n.Text(new Label(), "drop_overlay");
            dropPill.AddToClassList("pop-drop-overlay-pill");
            dropPill.pickingMode = PickingMode.Ignore;
            dropOverlay.Add(dropPill);
            dropOverlay.style.display = DisplayStyle.None;
            root.Add(dropOverlay);
            RegisterDrop(root);

            SelectTab(Mathf.Clamp(EditorPrefs.GetInt(PrefTab, 0), 0, 1));
            RefreshAll();
        }

        void SelectTab(int i)
        {
            currentTab = i;
            EditorPrefs.SetInt(PrefTab, i);
            for (int j = 0; j < pages.Length; j++)
            {
                pages[j].style.display = j == i ? DisplayStyle.Flex : DisplayStyle.None;
                footers[j].style.display = j == i ? DisplayStyle.Flex : DisplayStyle.None;
                tabButtons[j].EnableInClassList("pop-tab--active", j == i);
            }
            if (logSection != null) DrawLog();
        }

        // ---- まとめる
        VisualElement BuildPageBundle()
        {
            var page = new VisualElement();
            var card = NewCard(page);
            var titleRow = Row();
            titleRow.AddToClassList("pop-card-title-row");
            var title = l10n.Text(new Label(), "sec_packages");
            title.AddToClassList("pop-card-title");
            titleRow.Add(title);
            card.Add(titleRow);

            sourcesBox = new VisualElement();
            sourcesBox.AddToClassList("pop-list");
            card.Add(sourcesBox);

            dropZone = l10n.Tip(new Label(), "tip_drop");
            dropZone.AddToClassList("pop-drop");
            dropZone.RegisterCallback<ClickEvent>(e =>
            {
                var p = PickPackage(null);
                if (p != null) AddFile(p);
            });
            card.Add(dropZone);

            selfToggle = ToggleRow(card, "toggle_self", cfg.includeSelf, v => { cfg.includeSelf = v; Save(); RefreshAll(); });
            selfToggle.parent.AddToClassList("pop-option-row");
            // VCC で入れた場合はツール本体を VCC が管理するので、まとめに同梱しない
            if (InstalledAsPackage) selfToggle.parent.style.display = DisplayStyle.None;

            AddSettingsCard(page);
            return page;
        }

        VisualElement BuildLogSection()
        {
            logSection = new VisualElement();
            logSection.AddToClassList("pop-log-section");
            logHeader = new Button(() =>
            {
                showLog = !showLog;
                EditorPrefs.SetBool(PrefShowLog, showLog);
                DrawLog();
            });
            logHeader.AddToClassList("pop-log-header");
            logSection.Add(logHeader);
            logLabel = new Label();
            logLabel.AddToClassList("pop-log");
            logSection.Add(logLabel);
            return logSection;
        }

        void DrawLog()
        {
            bool has = !string.IsNullOrEmpty(cfg.lastBuildLog);
            logSection.style.display = currentTab == 0 && has ? DisplayStyle.Flex : DisplayStyle.None;
            logHeader.text = (showLog ? "－  " : "＋  ") + Txt.Tr("log_title");
            logLabel.text = cfg.lastBuildLog ?? "";
            logLabel.style.display = showLog ? DisplayStyle.Flex : DisplayStyle.None;
        }

        VisualElement BuildFooterBundle()
        {
            var footer = new VisualElement();
            footer.AddToClassList("pop-footer");


            buildButton = new Button(() => DoBuild());
            buildButton.AddToClassList("pop-btn");
            footer.Add(buildButton);

            badge = l10n.Text(new Label(), "badge_done");
            badge.AddToClassList("pop-badge");
            badge.style.display = DisplayStyle.None;
            footer.Add(badge);

            var linkRow = Row();
            linkRow.AddToClassList("pop-link-row");
            revealButton = l10n.Text(new Button(() => EditorUtility.RevealInFinder(cfg.outputPath)), "btn_reveal");
            revealButton.AddToClassList("pop-link");
            linkRow.Add(revealButton);
            footer.Add(linkRow);
            return footer;
        }

        // ---- プリセット
        VisualElement BuildPagePresets()
        {
            var page = new VisualElement();
            var card = NewCard(page);
            var title = l10n.Text(new Label(), "sec_preset_make");
            title.AddToClassList("pop-card-title");
            card.Add(title);

            var pick = Row();
            var lbl = l10n.Text(new Label(), "lbl_preset");
            lbl.AddToClassList("pop-inline-label");
            pick.Add(lbl);
            presetDropdown = new DropdownField();
            presetDropdown.AddToClassList("pop-grow");
            presetDropdown.RegisterValueChangedCallback(e =>
            {
                int i = presetDropdown.index;
                SelectPreset(i <= 0 ? "" : cfg.presets[i - 1].id);
                HideNameRow();
                RefreshAll();
            });
            pick.Add(presetDropdown);
            var add = IconButton("＋", "tip_preset_add", () => ShowNameRow(false));
            add.AddToClassList("pop-icon-btn--sub");
            pick.Add(add);
            renameButton = SmallButton("btn_rename", () => ShowNameRow(true));
            pick.Add(renameButton);
            deleteButton = IconButton("×", "tip_preset_del", DeletePreset);
            deleteButton.AddToClassList("pop-icon-btn--danger");
            pick.Add(deleteButton);
            card.Add(pick);

            // 名前入力（新規・名前変更のときだけ出す）
            nameRow = new VisualElement();
            nameRow.AddToClassList("pop-name-box");
            nameLabel = new Label();
            nameLabel.AddToClassList("pop-field-label");
            nameRow.Add(nameLabel);
            var nr = Row();
            nameField = new TextField();
            nameField.AddToClassList("pop-grow");
            nameField.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) ConfirmName();
                else if (e.keyCode == KeyCode.Escape) HideNameRow();
            });
            nr.Add(nameField);
            var ok = SmallButton("btn_ok", ConfirmName);
            ok.AddToClassList("pop-btn-sm--main");
            nr.Add(ok);
            nr.Add(SmallButton("btn_cancel", HideNameRow));
            nameRow.Add(nr);
            nameRow.style.display = DisplayStyle.None;
            card.Add(nameRow);

            presetHint = new Label();
            presetHint.AddToClassList("pop-note");
            card.Add(presetHint);

            allRow = Row();
            allRow.AddToClassList("pop-all-row");
            allOnButton = SmallButton("btn_all_on", () => SetAll(true));
            allOffButton = SmallButton("btn_all_off", () => SetAll(false));
            allRow.Add(allOnButton);
            allRow.Add(allOffButton);
            card.Add(allRow);

            importCountLabel = new Label();
            importCountLabel.AddToClassList("pop-count");
            card.Add(importCountLabel);

            presetChecks = new VisualElement();
            presetChecks.AddToClassList("pop-list");
            card.Add(presetChecks);
            return page;
        }

        VisualElement BuildFooterPresets()
        {
            var footer = new VisualElement();
            footer.AddToClassList("pop-footer");
            interactiveToggle = ToggleRow(footer, "toggle_interactive", interactiveImport, v =>
            {
                interactiveImport = v;
                EditorPrefs.SetBool(PrefInteractive, v);
            });
            importButton = new Button(ImportToProject);
            importButton.AddToClassList("pop-btn");
            footer.Add(importButton);
            return footer;
        }

        // ---- 設定
        void AddSettingsCard(VisualElement page)
        {
            var card = NewCard(page);
            var title = l10n.Text(new Label(), "sec_settings");
            title.AddToClassList("pop-card-title");
            card.Add(title);
            outputPathLabel = PathRow(card, "lbl_output", () =>
            {
                var dir = string.IsNullOrEmpty(cfg.outputPath) ? "" : Path.GetDirectoryName(cfg.outputPath);
                var name = string.IsNullOrEmpty(cfg.outputPath) ? "MyStarterBundle" : Path.GetFileNameWithoutExtension(cfg.outputPath);
                var p = EditorUtility.SaveFilePanel(Txt.Tr("pick_output"), dir, name, "unitypackage");
                if (!string.IsNullOrEmpty(p)) { cfg.outputPath = Norm(p); Save(); RefreshAll(); }
            });
            watchPathLabel = PathRow(card, "lbl_watch", () =>
            {
                var p = EditorUtility.OpenFolderPanel(Txt.Tr("pick_watch"), cfg.watchFolder, "");
                if (!string.IsNullOrEmpty(p)) { cfg.watchFolder = Norm(p); Save(); RefreshAll(); }
            });

        }

        VisualElement BuildLangSwitch()
        {
            var box = new VisualElement();
            box.AddToClassList("pop-lang");
            var ja = new Button { text = "日本語" };
            var en = new Button { text = "EN" };
            ja.AddToClassList("pop-lang-btn");
            en.AddToClassList("pop-lang-btn");

            void Mark()
            {
                ja.EnableInClassList("pop-lang-btn--active", PopL10n.Current == PopLang.Ja);
                en.EnableInClassList("pop-lang-btn--active", PopL10n.Current == PopLang.En);
            }
            void Switch(PopLang lang)
            {
                PopL10n.Current = lang;
                Mark();
                l10n.Apply();
                RefreshAll();
            }
            ja.clicked += () => Switch(PopLang.Ja);
            en.clicked += () => Switch(PopLang.En);

            box.Add(ja);
            box.Add(en);
            Mark();
            return box;
        }

        StyleSheet LoadStyle()
        {
            var script = MonoScript.FromScriptableObject(this);
            var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
            if (string.IsNullOrEmpty(dir)) return null;
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(Path.Combine(dir, "PopTool.uss").Replace('\\', '/'));
        }

        // ---- 部品
        static VisualElement NewCard(VisualElement parent)
        {
            var card = new VisualElement();
            card.AddToClassList("pop-card");
            parent.Add(card);
            return card;
        }

        static VisualElement Row()
        {
            var r = new VisualElement();
            r.AddToClassList("pop-row");
            return r;
        }

        Button SmallButton(string key, Action onClick)
        {
            var b = l10n.Text(new Button(onClick), key);
            b.AddToClassList("pop-btn-sm");
            return b;
        }

        Button IconButton(string text, string tipKey, Action onClick)
        {
            var b = l10n.Tip(new Button(onClick) { text = text }, tipKey);
            b.AddToClassList("pop-icon-btn");
            return b;
        }

        Label PathRow(VisualElement parent, string labelKey, Action onChange)
        {
            var lbl = l10n.Text(new Label(), labelKey);
            lbl.AddToClassList("pop-field-label");
            parent.Add(lbl);
            var row = Row();
            var path = new Label();
            path.AddToClassList("pop-path");
            row.Add(path);
            row.Add(SmallButton("btn_change", onChange));
            parent.Add(row);
            return path;
        }

        Toggle ToggleRow(VisualElement parent, string key, bool value, Action<bool> onChange)
        {
            var row = Row();
            row.AddToClassList("pop-toggle-row");
            var t = new Toggle { value = value };
            t.RegisterValueChangedCallback(e => onChange(e.newValue));
            var lbl = l10n.Text(new Label(), key);
            lbl.AddToClassList("pop-toggle-label");
            lbl.RegisterCallback<ClickEvent>(e => t.value = !t.value);
            row.Add(t);
            row.Add(lbl);
            parent.Add(row);
            return t;
        }

        // -------------------------------------------------------------- 表示の更新
        void RefreshAll()
        {
            if (sourcesBox == null) return;
            if (!string.IsNullOrEmpty(selectedPresetId) && SelectedPreset == null) SelectPreset("");
            RefreshCandidates();
            DrawSources();
            DrawStatus();
            DrawPresets();
            DrawLog();

            outputPathLabel.text = string.IsNullOrEmpty(cfg.outputPath) ? Txt.Tr("not_set") : Disp(cfg.outputPath);
            outputPathLabel.tooltip = Disp(cfg.outputPath);
            watchPathLabel.text = string.IsNullOrEmpty(cfg.watchFolder) ? Txt.Tr("not_set") : Disp(cfg.watchFolder);
            watchPathLabel.tooltip = Disp(cfg.watchFolder);
            selfToggle.SetValueWithoutNotify(cfg.includeSelf);
            interactiveToggle.SetValueWithoutNotify(interactiveImport);
        }

        void DrawSources()
        {
            sourcesBox.Clear();
            bool empty = cfg.sources.Count == 0;
            dropZone.text = Txt.Tr(empty ? "drop_big" : "drop_compact");
            dropZone.EnableInClassList("pop-drop--big", empty);

            for (int i = 0; i < cfg.sources.Count; i++)
            {
                int idx = i;
                var s = cfg.sources[i];
                bool exists = File.Exists(s.path);

                var item = new VisualElement();
                item.AddToClassList("pop-item");
                if (!s.enabled) item.AddToClassList("pop-item--off");

                if (s.removed)
                {
                    item.AddToClassList("pop-item--removed");
                    var rrow = Row();
                    var rname = new Label(Path.GetFileNameWithoutExtension(s.path)) { tooltip = Disp(s.path) };
                    rname.AddToClassList("pop-item-name");
                    rrow.Add(rname);
                    var undo = new Button(() => UndoRemove(idx)) { text = Txt.Tr("btn_undo_remove"), tooltip = Txt.Tr("tip_undo_remove") };
                    undo.AddToClassList("pop-btn-sm");
                    undo.AddToClassList("pop-btn-sm--undo");
                    rrow.Add(undo);
                    item.Add(rrow);
                    sourcesBox.Add(item);
                    continue;
                }

                var row = Row();
                var handle = new Label("≡") { tooltip = Txt.Tr("tip_drag") };
                handle.AddToClassList("pop-handle");
                RegisterRowDrag(handle, idx);
                row.Add(handle);
                var t = new Toggle { value = s.enabled, tooltip = Txt.Tr("tip_enabled") };
                t.RegisterValueChangedCallback(e => { s.enabled = e.newValue; Save(); RefreshAll(); });
                row.Add(t);

                var name = new Label(Path.GetFileNameWithoutExtension(s.path) + (exists ? "" : Txt.Tr("missing_suffix")));
                name.tooltip = Disp(s.path);
                name.AddToClassList("pop-item-name");
                if (!exists) name.AddToClassList("pop-item-name--missing");
                row.Add(name);

                // 見つからないファイルは「場所を選び直す」、あるファイルは「差し替え」
                var rep = new Button(() =>
                {
                    var p = PickPackage(s.path);
                    if (p != null) ResolveOne(p, real => SetSourcePath(s, real, ZipUtil.IsZip(p) ? Norm(p) : ""));
                })
                {
                    text = Txt.Tr(exists ? "btn_replace" : "btn_relocate"),
                    tooltip = Txt.Tr(exists ? "tip_replace" : "tip_relocate"),
                };
                rep.AddToClassList("pop-btn-sm");
                if (!exists) rep.AddToClassList("pop-btn-sm--warn");
                row.Add(rep);

                var del = new Button(() => RemoveSource(idx)) { text = "×", tooltip = Txt.Tr("tip_remove") };
                del.AddToClassList("pop-icon-btn");
                del.AddToClassList("pop-icon-btn--danger");
                row.Add(del);
                item.Add(row);

                string cand;
                if (newer.TryGetValue(s.path, out cand))
                {
                    var nrow = Row();
                    nrow.AddToClassList("pop-newer");
                    var nl = new Label(Txt.F("newer_label", Path.GetFileNameWithoutExtension(ZipUtil.IsRef(cand) ? ZipUtil.EntryOf(cand) : cand)));
                    nl.tooltip = Disp(ZipUtil.IsRef(cand) ? ZipUtil.ZipOf(cand) : cand);
                    nl.AddToClassList("pop-newer-label");
                    nrow.Add(nl);
                    var use = new Button(() => ResolveOne(cand, real => SetSourcePath(s, real, ZipUtil.IsRef(cand) ? ZipUtil.ZipOf(cand) : ""))) { text = Txt.Tr("btn_use_newer") };
                    use.AddToClassList("pop-btn-sm");
                    use.AddToClassList("pop-btn-sm--sub");
                    nrow.Add(use);
                    item.Add(nrow);
                }
                sourcesBox.Add(item);
            }

        }

        void DrawStatus()
        {
            // 作成ボタンが状態を表す: 初回は「作成」、変更があれば「変更を保存」、保存済みなら押せない
            bool built = !string.IsNullOrEmpty(cfg.outputPath) && File.Exists(cfg.outputPath);
            // 外した行が残っている間は「変更を保存」のまま（保存で一覧から消える）
            bool pendingRemove = cfg.sources.Any(x => x.removed);
            bool upToDate = built && cfg.lastBuildSignature == Signature() && !pendingRemove;
            bool canBuild = (cfg.sources.Any(x => x.Live) || pendingRemove) && !string.IsNullOrEmpty(cfg.outputPath);

            buildButton.text = Txt.Tr(!built ? "btn_build_new" : upToDate ? "btn_build_done" : "btn_build_save");
            buildButton.SetEnabled(canBuild && !upToDate);
            buildButton.EnableInClassList("pop-btn--done", upToDate);
            buildButton.tooltip = string.IsNullOrEmpty(cfg.outputPath) ? Txt.Tr("st_no_output") : "";
            revealButton.style.display = File.Exists(cfg.outputPath) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void DrawPresets()
        {
            var preset = SelectedPreset;
            bool isAll = preset == null;

            // プルダウン
            var choices = new List<string> { Txt.Tr("preset_all") };
            choices.AddRange(cfg.presets.Select(p => p.name));
            presetDropdown.choices = choices;
            presetDropdown.SetValueWithoutNotify(isAll ? choices[0] : preset.name);

            renameButton.SetEnabled(!isAll);
            deleteButton.SetEnabled(!isAll);
            allOnButton.SetEnabled(!isAll);
            allOffButton.SetEnabled(!isAll);
            allRow.style.display = isAll ? DisplayStyle.None : DisplayStyle.Flex;
            presetHint.text = isAll ? Txt.Tr("hint_all") : Txt.F("hint_autosave", preset.name);

            // チェック一覧
            presetChecks.Clear();
            var enabledSources = cfg.sources.Where(x => x.Live).ToList();
            if (enabledSources.Count == 0)
            {
                var none = new Label(Txt.Tr("no_enabled"));
                none.AddToClassList("pop-note");
                presetChecks.Add(none);
            }
            foreach (var s in enabledSources)
            {
                var src = s;
                var row = Row();
                row.AddToClassList("pop-check-row");
                bool on = isAll || !preset.off.Contains(src.id);
                var t = new Toggle { value = on };
                t.SetEnabled(!isAll);
                t.RegisterValueChangedCallback(e =>
                {
                    var p = SelectedPreset;
                    if (p == null) return;
                    if (e.newValue) p.off.Remove(src.id);
                    else if (!p.off.Contains(src.id)) p.off.Add(src.id);
                    Save();
                    UpdateImportButton();
                    row.EnableInClassList("pop-check-row--off", !e.newValue);
                });
                var lbl = new Label(Path.GetFileNameWithoutExtension(src.path));
                lbl.tooltip = Disp(src.path);
                lbl.AddToClassList("pop-toggle-label");
                if (!isAll) lbl.RegisterCallback<ClickEvent>(e => t.value = !t.value);
                row.Add(t);
                row.Add(lbl);
                row.EnableInClassList("pop-check-row--off", !on);
                presetChecks.Add(row);
            }
            UpdateImportButton();
        }

        void UpdateImportButton()
        {
            var preset = SelectedPreset;
            int count = SelectedSources().Count;
            importCountLabel.text = Txt.F("import_count", cfg.sources.Count(x => x.Live), count);
            importButton.text = Txt.F("btn_import", preset == null ? Txt.Tr("preset_all") : preset.name, count);
            importButton.SetEnabled(count > 0 && !string.IsNullOrEmpty(cfg.outputPath));
        }

        List<BundlerSource> SelectedSources()
        {
            var preset = SelectedPreset;
            return cfg.sources.Where(s => s.Live && (preset == null || !preset.off.Contains(s.id))).ToList();
        }

        void ShowBadge()
        {
            badge.RemoveFromClassList("pop-badge--show");
            badge.style.display = DisplayStyle.Flex;
            badge.schedule.Execute(() => badge.AddToClassList("pop-badge--show")).StartingIn(20);
            badge.schedule.Execute(() => badge.RemoveFromClassList("pop-badge--show")).StartingIn(2500);
            badge.schedule.Execute(() => badge.style.display = DisplayStyle.None).StartingIn(2800);
        }

        // -------------------------------------------------------------- プリセット操作
        void ShowNameRow(bool rename)
        {
            renaming = rename;
            var preset = SelectedPreset;
            if (rename && preset == null) return;
            nameLabel.text = Txt.Tr(rename ? "lbl_rename" : "lbl_new_name");
            nameField.SetValueWithoutNotify(rename ? preset.name : "");
            nameRow.style.display = DisplayStyle.Flex;
            nameField.schedule.Execute(() => nameField.Focus()).StartingIn(10);
        }

        void HideNameRow()
        {
            renaming = false;
            if (nameRow != null) nameRow.style.display = DisplayStyle.None;
        }

        void ConfirmName()
        {
            string name = (nameField.value ?? "").Trim();
            if (name.Length == 0) return;
            var current = SelectedPreset;
            bool dup = name == Txt.T["preset_all"][0] || name == Txt.T["preset_all"][1] ||
                       cfg.presets.Any(p => p.name == name && !(renaming && p == current));
            if (dup)
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.Tr("dlg_name_dup"), Txt.Tr("dlg_ok"));
                return;
            }

            if (renaming && current != null)
            {
                current.name = name;
            }
            else
            {
                var p = new ImportPreset { id = Guid.NewGuid().ToString("N"), name = name };
                if (current != null) p.off = new List<string>(current.off); // 今のチェックをコピー
                cfg.presets.Add(p);
                SelectPreset(p.id);
            }
            Save();
            HideNameRow();
            RefreshAll();
        }

        void DeletePreset()
        {
            var p = SelectedPreset;
            if (p == null) return;
            if (!EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.F("dlg_preset_del", p.name),
                    Txt.Tr("dlg_btn_delete"), Txt.Tr("dlg_cancel"))) return;
            cfg.presets.Remove(p);
            SelectPreset("");
            Save();
            HideNameRow();
            RefreshAll();
        }

        void SetAll(bool on)
        {
            var p = SelectedPreset;
            if (p == null) return;
            p.off.Clear();
            if (!on) p.off.AddRange(cfg.sources.Where(s => s.Live).Select(s => s.id));
            Save();
            RefreshAll();
        }

        // -------------------------------------------------------------- 行のドラッグ並べ替え
        void RegisterRowDrag(VisualElement handle, int index)
        {
            handle.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                dragFrom = index;
                dragTo = index;
                handle.CapturePointer(e.pointerId);
                sourcesBox[index].AddToClassList("pop-item--dragging");
                e.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (dragFrom < 0 || !handle.HasPointerCapture(e.pointerId)) return;
                int count = sourcesBox.childCount;
                int target = count;
                for (int i = 0; i < count; i++)
                {
                    if (e.position.y < sourcesBox[i].worldBound.center.y) { target = i; break; }
                }
                dragTo = target;
                for (int i = 0; i < count; i++)
                {
                    sourcesBox[i].EnableInClassList("pop-item--insert-before", i == target);
                    sourcesBox[i].EnableInClassList("pop-item--insert-after", target == count && i == count - 1);
                }
            });
            handle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (dragFrom < 0) return;
                int from = dragFrom, to = dragTo;
                dragFrom = -1;
                handle.ReleasePointer(e.pointerId);
                FinishRowDrag(from, to);
            });
            handle.RegisterCallback<PointerCaptureOutEvent>(e =>
            {
                if (dragFrom >= 0) { dragFrom = -1; RefreshAll(); }
            });
        }

        void FinishRowDrag(int from, int to)
        {
            if (to > from) to--;
            if (from >= 0 && to >= 0 && to < cfg.sources.Count && to != from)
            {
                var item = cfg.sources[from];
                cfg.sources.RemoveAt(from);
                cfg.sources.Insert(to, item);
                Save();
            }
            RefreshAll();
        }

        // -------------------------------------------------------------- パッケージ操作
        void RemoveSource(int idx)
        {
            if (idx < 0 || idx >= cfg.sources.Count) return;
            cfg.sources[idx].removed = true;   // 保存するまでは一覧に半透明で残す
            Save();
            RefreshAll();
        }

        void UndoRemove(int idx)
        {
            if (idx < 0 || idx >= cfg.sources.Count) return;
            cfg.sources[idx].removed = false;
            Save();
            RefreshAll();
        }

        /// <summary>保存したときに、外した行を一覧とプリセットから本当に消す。</summary>
        void PurgeRemoved()
        {
            var gone = new HashSet<string>(cfg.sources.Where(x => x.removed).Select(x => x.id));
            if (gone.Count == 0) return;
            cfg.sources.RemoveAll(x => x.removed);
            foreach (var p in cfg.presets) p.off.RemoveAll(id => gone.Contains(id));
            Save();
            CleanupExtracted();
        }

        void RegisterDrop(VisualElement area)
        {
            area.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (DroppedPackages().Length == 0) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                ShowDropOverlay(true);
            });
            area.RegisterCallback<DragLeaveEvent>(e =>
            {
                // 子要素の間を移動しただけでは消さない。ウィンドウの外に出たら消す
                if (!area.worldBound.Contains(e.mousePosition)) ShowDropOverlay(false);
            });
            area.RegisterCallback<DragExitedEvent>(e => ShowDropOverlay(false));
            area.RegisterCallback<DragPerformEvent>(e =>
            {
                ShowDropOverlay(false);
                var files = DroppedPackages();
                if (files.Length == 0) return;
                DragAndDrop.AcceptDrag();
                if (currentTab != 0) SelectTab(0);
                foreach (var f in files) AddFile(f);
            });
        }

        void ShowDropOverlay(bool on)
        {
            dropOverlay.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (on) dropOverlay.BringToFront();
        }

        static string[] DroppedPackages()
        {
            return (DragAndDrop.paths ?? new string[0])
                .Where(p => p.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase) || ZipUtil.IsZip(p))
                .ToArray();
        }

        /// <summary>.unitypackage はそのまま、zip は中の .unitypackage を取り出して登録する。</summary>
        void AddFile(string path)
        {
            if (!ZipUtil.IsZip(path)) { AddSource(path); return; }
            List<string> refs;
            if (!TryListZip(path, out refs)) return;
            if (refs.Count == 1) { AddExtracted(refs, path); return; }
            ZipPickWindow.Show(path, refs, false, picked => AddExtracted(picked, path));
        }

        void AddExtracted(List<string> refs, string zip)
        {
            foreach (var r in refs)
            {
                string real;
                if (!TryExtract(r, out real)) continue;
                AddSource(real, Norm(zip));
            }
        }

        /// <summary>1 つだけ使う場面（差し替え・更新版）。zip なら取り出してから渡す。</summary>
        void ResolveOne(string path, Action<string> use)
        {
            if (ZipUtil.IsRef(path))
            {
                string real;
                if (TryExtract(path, out real)) use(real);
                return;
            }
            if (!ZipUtil.IsZip(path)) { use(path); return; }
            List<string> refs;
            if (!TryListZip(path, out refs)) return;
            Action<List<string>> pick = picked =>
            {
                string real;
                if (picked.Count > 0 && TryExtract(picked[0], out real)) use(real);
            };
            if (refs.Count == 1) pick(refs);
            else ZipPickWindow.Show(path, refs, true, pick);
        }

        void SetSourcePath(BundlerSource s, string real, string origin)
        {
            s.path = Norm(real);
            s.origin = origin ?? "";
            Save();
            CleanupExtracted();
            RefreshAll();
        }

        bool TryListZip(string zip, out List<string> refs)
        {
            refs = null;
            try { refs = ZipUtil.ListPackages(zip); }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.F("zip_fail", Path.GetFileName(zip), e.Message), Txt.Tr("dlg_ok"));
                return false;
            }
            if (refs.Count == 0)
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.F("zip_none", Path.GetFileName(zip)), Txt.Tr("dlg_ok"));
                return false;
            }
            return true;
        }

        static bool TryExtract(string zipRef, out string real)
        {
            real = null;
            try { real = ZipUtil.Extract(zipRef); return true; }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.F("zip_fail", Path.GetFileName(ZipUtil.ZipOf(zipRef)), e.Message), Txt.Tr("dlg_ok"));
                return false;
            }
        }

        void CleanupExtracted()
        {
            ZipUtil.Cleanup(cfg.sources.Select(x => x.path));
        }

        void AddSource(string path, string origin = "")
        {
            path = Norm(path);
            var already = cfg.sources.FirstOrDefault(s => SamePath(s.path, path));
            if (already != null)
            {
                if (already.removed) { already.removed = false; Save(); RefreshAll(); }
                return;
            }
            if (SamePath(path, cfg.outputPath))
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.Tr("dlg_self_add"), Txt.Tr("dlg_ok"));
                return;
            }

            var stem = VersionUtil.Stem(path);
            var same = stem.Length >= 2 ? cfg.sources.FirstOrDefault(s => !s.removed && VersionUtil.Stem(s.path) == stem) : null;
            if (same != null && EditorUtility.DisplayDialog(Txt.Tr("dlg_title"),
                    Txt.F("dlg_other_ver", Path.GetFileName(path), Path.GetFileName(same.path)),
                    Txt.Tr("dlg_btn_replace"), Txt.Tr("dlg_btn_add_sep")))
            {
                same.path = path;
                same.origin = origin;
            }
            else
            {
                cfg.sources.Add(new BundlerSource { id = Guid.NewGuid().ToString("N"), path = path, origin = origin });
            }
            Save();
            CleanupExtracted();
            RefreshAll();
        }

        bool DoBuild()
        {
            // 中身は保存済みと同じで、外した行を片付けるだけなら作り直さない
            bool built = !string.IsNullOrEmpty(cfg.outputPath) && File.Exists(cfg.outputPath);
            if ((built && cfg.lastBuildSignature == Signature()) || !cfg.sources.Any(x => x.Live))
            {
                PurgeRemoved();
                RefreshAll();
                ShowBadge();
                return true;
            }

            // 上にあるほど優先 = 合成では最後に重ねる
            var srcs = cfg.sources.Where(s => s.Live).Select(s => s.path).Reverse().ToList();
            bool ok = BuildTo(srcs, cfg.outputPath, true);
            if (ok) PurgeRemoved();
            RefreshAll();
            if (ok) ShowBadge();
            return ok;
        }

        /// <summary>srcs を合成して outPath に書き出す。record=true のときは「まとめ」として状態を記録。</summary>
        bool BuildTo(List<string> srcs, string outPath, bool record)
        {
            if (srcs.Count == 0) return false;

            var missing = srcs.Where(p => !File.Exists(p)).ToList();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"),
                    Txt.F("dlg_missing", string.Join("\n", missing.Select(Path.GetFileName).ToArray())), Txt.Tr("dlg_ok"));
                return false;
            }
            if (string.IsNullOrEmpty(outPath))
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.Tr("st_no_output"), Txt.Tr("dlg_ok"));
                return false;
            }
            string outFull = Norm(outPath);
            if (srcs.Any(p => SamePath(p, outFull)))
            {
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.Tr("dlg_out_in_list"), Txt.Tr("dlg_ok"));
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outFull));
                var extras = cfg.includeSelf && !InstalledAsPackage ? GetSelfAssets() : null;
                var log = Bundler.Build(srcs, outFull, extras,
                    (msg, p) => EditorUtility.DisplayProgressBar(Txt.Tr("dlg_title"), msg, p));
                if (record)
                {
                    cfg.lastBuildLog = log;
                    cfg.lastBuildSignature = Signature();
                    Save();
                }
                Debug.Log("[PackageBundler]\n" + log);
                return true;
            }
            catch (Exception e)
            {
                if (record)
                {
                    cfg.lastBuildLog = Txt.F("log_failed", e.Message);
                    Save();
                }
                Debug.LogException(e);
                EditorUtility.DisplayDialog(Txt.Tr("dlg_title"), Txt.F("dlg_build_fail", e.Message), Txt.Tr("dlg_ok"));
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void ImportToProject()
        {
            var enabledSources = cfg.sources.Where(s => s.Live).ToList();
            var selected = SelectedSources();
            if (selected.Count == 0) return;

            if (selected.Count < enabledSources.Count)
            {
                // 一部だけ: 選んだものだけで一時パッケージを作ってインポート
                var tmp = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/PackageBundler_Selected.unitypackage"));
                if (!BuildTo(selected.Select(s => s.path).Reverse().ToList(), tmp, false)) return;
                AssetDatabase.ImportPackage(tmp, interactiveImport);
                return;
            }

            bool upToDate = File.Exists(cfg.outputPath) && cfg.lastBuildSignature == Signature();
            if (!upToDate)
            {
                int r = EditorUtility.DisplayDialogComplex(Txt.Tr("dlg_title"),
                    Txt.Tr("dlg_outdated"),
                    Txt.Tr("dlg_btn_rebuild"), Txt.Tr("dlg_cancel"),
                    File.Exists(cfg.outputPath) ? Txt.Tr("dlg_btn_anyway") : "");
                if (r == 1) return;
                if (r == 0 && !DoBuild()) return;
            }
            if (!File.Exists(cfg.outputPath)) return;
            AssetDatabase.ImportPackage(cfg.outputPath, interactiveImport);
        }

        /// <summary>このツールのフォルダ（スクリプトと USS）を、親フォルダごと同梱用に集める</summary>
        List<Bundler.ExtraAsset> GetSelfAssets()
        {
            var list = new List<Bundler.ExtraAsset>();
            var script = MonoScript.FromScriptableObject(this);
            var path = script != null ? AssetDatabase.GetAssetPath(script) : null;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
            {
                Debug.LogWarning("[PackageBundler] " + Txt.Tr("warn_self_skip"));
                return list;
            }

            var toolDir = (Path.GetDirectoryName(path) ?? "").Replace('\\', '/');
            var folders = new List<string>();
            var dir = toolDir;
            while (!string.IsNullOrEmpty(dir) && dir != "Assets")
            {
                folders.Insert(0, dir);
                dir = (Path.GetDirectoryName(dir) ?? "").Replace('\\', '/');
            }
            foreach (var f in folders) list.Add(MakeExtra(f, false));

            foreach (var file in Directory.GetFiles(Path.GetFullPath(toolDir)))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(file + ".meta")) continue;
                list.Add(MakeExtra(toolDir + "/" + Path.GetFileName(file), true));
            }
            return list;
        }

        static Bundler.ExtraAsset MakeExtra(string assetPath, bool isFile)
        {
            var full = Path.GetFullPath(assetPath);
            var x = new Bundler.ExtraAsset();
            x.guid = AssetDatabase.AssetPathToGUID(assetPath);
            x.pathname = assetPath;
            x.asset = isFile ? File.ReadAllBytes(full) : null;
            x.meta = File.ReadAllBytes(full + ".meta");
            return x;
        }

        string Signature()
        {
            var sb = new StringBuilder();
            foreach (var s in cfg.sources.Where(x => x.Live))
            {
                sb.Append(s.path).Append('|');
                if (File.Exists(s.path))
                {
                    var fi = new FileInfo(s.path);
                    sb.Append(fi.Length).Append('|').Append(fi.LastWriteTimeUtc.Ticks);
                }
                sb.Append('\n');
            }
            sb.Append(cfg.includeSelf);
            return sb.ToString();
        }

        // -------------------------------------------------------------- 更新版の検出
        // 探す場所には zip がたくさんあることがあるので、裏で探して見つかったら表示を更新する
        int scanGen;
        readonly object scanLock = new object();
        Dictionary<string, string> scanResult;
        int scanResultGen;

        void RefreshCandidates()
        {
            if (cfg == null) return;
            int gen = ++scanGen;
            var sources = cfg.sources.Where(x => !x.removed).Select(x => new[] { x.path, x.origin ?? "" }).ToList();
            var listed = new HashSet<string>(cfg.sources.Select(x => Norm(x.path)), StringComparer.OrdinalIgnoreCase);
            listed.Add(Norm(cfg.outputPath));
            foreach (var x in cfg.sources) if (!string.IsNullOrEmpty(x.origin)) listed.Add(Norm(x.origin));
            var watch = cfg.watchFolder;

            System.Threading.Tasks.Task.Run(() =>
            {
                Dictionary<string, string> res;
                try { res = FindNewer(sources, listed, watch); }
                catch (Exception) { return; }
                lock (scanLock) { scanResult = res; scanResultGen = gen; }
            });
        }

        void PollScan()
        {
            Dictionary<string, string> res;
            lock (scanLock)
            {
                if (scanResult == null) return;
                if (scanResultGen != scanGen) { scanResult = null; return; }   // 古い結果は捨てる
                res = scanResult;
                scanResult = null;
            }
            bool same = res.Count == newer.Count && res.All(kv => { string v; return newer.TryGetValue(kv.Key, out v) && v == kv.Value; });
            if (same) return;
            newer.Clear();
            foreach (var kv in res) newer[kv.Key] = kv.Value;
            if (sourcesBox != null && dragFrom < 0) DrawSources();
        }

        static Dictionary<string, string> FindNewer(List<string[]> sources, HashSet<string> listed, string watch)
        {
            var result = new Dictionary<string, string>();
            var cache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var src in sources)
            {
                string path = src[0], origin = src[1];
                var stem = VersionUtil.Stem(path);
                if (stem.Length < 2) continue;

                var dirs = new List<KeyValuePair<string, int>>();
                var own = SafeDir(string.IsNullOrEmpty(origin) ? path : origin);
                if (!string.IsNullOrEmpty(own)) dirs.Add(new KeyValuePair<string, int>(own, 0));
                if (!string.IsNullOrEmpty(watch)) dirs.Add(new KeyValuePair<string, int>(watch, 2));

                string best = null;
                foreach (var d in dirs)
                {
                    string key = d.Key + "|" + d.Value;
                    List<string> files;
                    if (!cache.TryGetValue(key, out files))
                    {
                        files = new List<string>();
                        ListPackages(d.Key, d.Value, files);
                        cache[key] = files;
                    }
                    foreach (var f in files)
                    {
                        if (listed.Contains(Norm(f))) continue;
                        if (!string.IsNullOrEmpty(origin) && ZipUtil.IsRef(f) && SamePath(ZipUtil.ZipOf(f), origin)) continue;
                        if (VersionUtil.Stem(f) != stem) continue;
                        if (!VersionUtil.IsNewer(f, path)) continue;
                        if (best == null || VersionUtil.IsNewer(f, best)) best = f;
                    }
                }
                if (best != null) result[path] = Norm(best);
            }
            return result;
        }

        static void ListPackages(string dir, int depth, List<string> result)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                result.AddRange(Directory.GetFiles(dir, "*.unitypackage", SearchOption.TopDirectoryOnly));
                foreach (var zip in Directory.GetFiles(dir, "*.zip", SearchOption.TopDirectoryOnly))
                {
                    try { result.AddRange(ZipUtil.ListPackages(zip)); }
                    catch (Exception) { /* 壊れた zip などは無視 */ }
                }
                if (depth > 0)
                    foreach (var sub in Directory.GetDirectories(dir))
                        ListPackages(sub, depth - 1, result);
            }
            catch (Exception) { /* アクセスできないフォルダは無視 */ }
        }

        // -------------------------------------------------------------- ユーティリティ
        static string PickPackage(string current)
        {
            var dir = string.IsNullOrEmpty(current) ? "" : (SafeDir(current) ?? "");
            var p = EditorUtility.OpenFilePanelWithFilters(Txt.Tr("pick_package"), dir,
                new[] { "Unity Package / zip", "unitypackage,zip" });
            return string.IsNullOrEmpty(p) ? null : Norm(p);
        }

        /// <summary>画面表示用のパス。UI Toolkit は "\r" などを制御文字として解釈してしまうので / に置き換える</summary>
        static string Disp(string p)
        {
            return string.IsNullOrEmpty(p) ? "" : p.Replace('\\', '/');
        }

        // ---- 日本語フォント
        // Unity 標準フォントには漢字がなく、一部が中国語フォントで代替表示されてしまうため、
        // Windows / Mac の日本語 UI フォントを明示的に使う
        static Font cachedFont;
        static bool fontTried;

        // Windows の日本語フォントのファイル名（上から順に試す）
        static readonly string[] JaFontFiles =
        {
            "meiryo.ttc", "yugothm.ttc", "yugothr.ttc", "biz-udgothicr.ttc", "msgothic.ttc",
        };

        static void ApplyJapaneseFont(VisualElement root)
        {
            try
            {
                if (!fontTried)
                {
                    fontTried = true;
                    cachedFont = LoadJapaneseFont();
                }
                // 読み込めたときだけ使う（失敗したら Unity 標準フォントのまま。文字が消えないように）
                if (cachedFont != null) root.style.unityFontDefinition = FontDefinition.FromFont(cachedFont);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PackageBundler] font: " + e.Message);
            }
        }

        static Font LoadJapaneseFont()
        {
            var paths = Font.GetPathsToOSFonts();
            foreach (var file in JaFontFiles)
            {
                var path = paths.FirstOrDefault(x => string.Equals(Path.GetFileName(x), file, StringComparison.OrdinalIgnoreCase));
                if (path == null) continue;
                // OS フォントの「名前」ではなく「ファイル」から作ると、UI Toolkit でも使えるフォントデータ付きになる
                var font = new Font(path);
                if (UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(font, 90) ==
                    UnityEngine.TextCore.LowLevel.FontEngineError.Success)
                {
                    font.hideFlags = HideFlags.HideAndDontSave;
                    return font;
                }
                DestroyImmediate(font);
            }
            return null;
        }

        static string Norm(string p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            if (ZipUtil.IsRef(p)) return p;   // zip の中を指す文字列はそのまま
            try { return Path.GetFullPath(p); } catch { return p; }
        }

        static bool SamePath(string a, string b)
        {
            return string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);
        }

        static string SafeDir(string p)
        {
            try { return string.IsNullOrEmpty(p) ? null : Path.GetDirectoryName(p); } catch { return null; }
        }
    }
}
