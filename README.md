# Package Bundler

いつも入れる `.unitypackage` を 1 つにまとめる Unity エディタ拡張です（VRChat / Unity 2022.3.22f1）。
A Unity editor extension that bundles the `.unitypackage` files you always import into one (VRChat / Unity 2022.3.22f1).

## VCC に追加 / Add to VCC

ページを開いて「Add to VCC」を押してください。
Open the page below and press "Add to VCC".

https://orcafox-tailandtide.github.io/package-bundler/

VCC の「Settings → Packages → Add Repository」に次の URL を入れても追加できます。
You can also add this URL in VCC: Settings → Packages → Add Repository.

```
https://orcafox-tailandtide.github.io/package-bundler/index.json
```

## 使い方 / Usage

1. メニュー「Tools > Package Bundler」を開く / Open "Tools > Package Bundler"
2. 「まとめる」タブで .unitypackage をドラッグ＆ドロップ / Drag & drop .unitypackage files in the "Bundle" tab
3. 「まとめパッケージを作成／更新」 / Press "Build / Update bundle"
4. 「インポート」タブでプリセットを選んでインポート / Pick a preset in the "Import" tab and import

## 注意 / Notes

- .unitypackage 版と VCC 版は、どちらか片方だけ入れてください。VCC 版を入れると、.unitypackage 版のフォルダ（Assets/PackageBundler）は自動で取り除かれます。
  Install either the .unitypackage version or the VCC version. Installing the VCC version removes the old Assets/PackageBundler folder automatically.

---

Made by OrcaFox (Tail＆Tide)
