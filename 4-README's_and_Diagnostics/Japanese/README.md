# PoE2 Route AutoSplitter

**Path of Exile 2 のキャンペーン・スピードラン**向けのセットアップツール兼 LiveSplit オートスプリッターです。

現在のリリース: **v3.0.0 Release Candidate**

PoE2 Route AutoSplitter には、以下の既定ルートおよびカスタムルートが用意されています。

* 探索 / エリア完了
* Boss Rush
* 探索 + Boss Rush の組み合わせ
* Campaign Any%
* Campaign 100%
* キャンペーン必須ボスのみ
* 0.5 Pinnacle ボス
* Temple of Chaos
* Trial of the Sekhemas
* ユーザー定義のカスタムルート
* Maps

付属の **PoE2RouteSetup** アプリケーションが、セットアップの大部分を処理します。

ポーズメニューを開いたときに、ゲームと LiveSplit タイマーを同期して一時停止できます。
LiveSplit の Game Time を使用すると、ロード時間を除外し、該当オプションが有効な場合は手動ポーズ時間も除外できます。

スクリーンショット: https://imgur.com/a/VgiRn6o

---
# ランポリシー

できるだけ特定のルールセットに依存しない設計にしています。プレイヤーは、自分のラン規則や使用するトリガーをかなり自由に選択できます。

Riverbank から新規キャラクターで開始する場合、キャラクターが目覚めてから The Wounded Man と会話するまでの短い時間は意図的に計測しません。これにより、実際のラン開始前に設定の修正、「skip tutorial」の選択、その他の調整を行う時間を確保できます。The Wounded Man と会話した後、最後の導入セリフでランタイムが開始されます。

Zone-Transition-Start は、キャラクターがあらかじめ指定されたゾーンへ入った瞬間に有効になります。動的ルートでは、別のゾーンから開始していた場合でも、その特定ゾーンへ入った時点で初めてタイマーと追跡が開始されます。

ゲームが長いため、GameTimeWatcher を用意しています。これは Pause Game メニューまたはマイクロトランザクションメニューが開いている間、LiveSplit の Game Time を停止するための補助プログラムです。休憩や、画面から離れて対応する必要がある状況を想定しています。キャラクターを操作できるその他のメニューではタイマーは停止しません。ゲーム内カットシーン中もインベントリを操作でき、ラン最適化に利用できるためタイマーは進みます。現在、タイマーが停止するのはロード画面、ポーズメニュー、マイクロトランザクションショップのみです。

---

# ダウンロード

ダウンロードは[こちら](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags)から行えます。

または

この GitHub リポジトリの **Releases** セクションから最新版をダウンロードしてください。

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

ほとんどのユーザーにはインストーラーを推奨します。

インストーラーを使用しないユーザー向けにポータブル ZIP が提供される場合もあります。その場合は PowerShell で `\Setup-UI[Configuration]\Build.ps1` を実行し、`RouteSetup.exe` を生成する必要があります。

---

# クイックスタート

## 1. PoE2 Route AutoSplitter をインストール

次を実行します。

`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`

インストーラーの指示に従ってください。

インストール後、次を開きます。

**PoE2 Route AutoSplitter**

ルートセットアップアプリケーションが起動します。

---

## 2. ルートを選択

Setup アプリケーションには既定ルートの一覧があります。

実行したいルートを選択してください。

例:

* Campaign Any%
* Campaign 100%
* 必須ボスのみ
* 探索ルート
* Boss Rush ルート
* 探索 + Boss Rush の組み合わせ

**Custom Route** を選択して独自のルートを作成することもできます。

---

## 3. LiveSplit セットアップを生成

ルートを選択したら Generate ボタンをクリックします。

必要なファイルが次のディレクトリに生成されます。

`LiveSplit Target`

このフォルダーには、選択したルートで LiveSplit が必要とするファイルが格納されます。

新しいセットアップを生成するたびに **LiveSplit Target** の内容は置き換えられます。

---

# LiveSplit のセットアップ

LiveSplit では次の 2 項目を設定する必要があります。

1. スプリットファイル (`.lss`)
2. Scriptable Auto Splitter (`.asl`)

## スプリットファイルを読み込む

生成された **LiveSplit Target** フォルダー内の `.lss` ファイルを探し、LiveSplit で開きます。

LiveSplit から手動で読み込む場合は次を使用します。

**File → Open Splits → From File**

生成された `.lss` ファイルを選択してください。

---

## Scriptable Auto Splitter を追加

オートスプリッタースクリプトは LiveSplit のレイアウトへ手動で追加する必要があります。

LiveSplit で:

1. LiveSplit を右クリックします。
2. **Edit Layout** を選択します。
3. **+** ボタンをクリックします。
4. 次を選択します。

   **Control → Scriptable Auto Splitter**

5. 新しい **Scriptable Auto Splitter** コンポーネントを選択します。
6. **LiveSplit Target** 内の `.asl` ファイルを指定します。
7. レイアウトを保存します。

生成ファイルを移動した場合、または別の ASL ファイルを使うセットアップへ切り替えた場合のみ、このパスを変更する必要があります。

> PoE2 Route AutoSplitter は LiveSplit レイアウトを生成したり置き換えたり**しません**。

レイアウトはユーザー自身が管理します。

---

# Boss Rush のセットアップ

ボスを追跡するルートでは、付属の **BossWatcher** を使用します。

BossWatcher はゲーム画面からボス名を読み取り、ボスイベントをオートスプリッターへ送信します。

選択したルートで BossWatcher が必要な場合、PoE2 Route Setup 内の次のボタンを使用します。

**Start BossWatcher**

コンソールウィンドウが表示されます。

通常使用時、BossWatcher は以下のような必要なイベントのみを表示します。

* ボスを検出
* ボスを撃破
* 戦闘時間

例:

`[21:42:18] Encountered: The Executioner`

`[21:43:07] Defeated: The Executioner | Fight time: 49.213 s`

ラン中に BossWatcher コンソールを操作する必要はありません。

スピードラン中は開いたままにしてください。

---

# 探索ルート

探索ルートは、キャラクターが Path of Exile 2 の特定エリアへ入ったことを検出します。

探索のみのルートでは BossWatcher は**不要**です。

オートスプリッターは Path of Exile 2 のエリア遷移情報を自動的に読み取ります。

---

# 探索 + Boss Rush

組み合わせルートでは次の両方を追跡します。

* エリア完了
* ボス撃破

手順:

1. 生成された `.lss` を読み込みます。
2. Scriptable Auto Splitter に生成された `.asl` を指定します。
3. PoE2 Route Setup から BossWatcher を起動します。
4. ランを開始します。

エリア目標とボス目標の両方が同じルートで処理されます。

---

# カスタムルート

PoE2 Route Setup で **Custom Route** を選択すると、独自ルートを作成できます。

以下を追加できます。

* エリア
* ボス
* エリアとボスの両方

必要な目標を追加し、希望する順序に並べます。

完了したらセットアップを生成します。

**LiveSplit Target** 内に次が作成されます。

* `.lss`
* `.asl`
* ルート設定

上記と同じ LiveSplit 手順で読み込んでください。

---

# Trials

Trial of the Sekhemas と Temple of Chaos 用です。

開始条件は Trial 本体へ初めて入った時点です。準備を行うロビーは追跡対象ではありません。

終了条件は 2 種類あります。

1. どの深さまで進むかを選択し、指定した深さのボスを倒すと Trial が正常終了します。Trial を完了できなかった場合は失敗ランとなり、手動で再スタートする必要があります。

2. Trial から退出した時点で完了とします。Trial アリーナからの退出を終了条件にしたい場合に使用します。この場合、戦利品、キャッシュ、商人、Ascendancy 選択もラン時間に含まれます。

---

# Vaal Ruins

遷移処理の都合上、ロビーは境界ゾーンとして扱われます。Map からコンソールルームへ入ることは、その Map のサブエリアへ入るのではなく Map から退出したものとして処理されます。

Vaal Ruins は現在も開発中です。

---

# Maps

Hideout などの Map ハブにいる間の Map 準備時間は計測しません。Map に入るとタイマーが自動で開始し、エリアボス撃破後の最初の退出でスプリットします。ボス撃破前に Map を退出した場合、タイマーは継続します。そのため、ボスを急いで倒して Map を退出し、同じ Map に再入場して追加コンテンツを、停止したタイマーで処理することができます。（代替ポリシーは下記。）

Map ランには複数の終了条件があります。

* 固定回数の Map
* 最初の死亡まで（Deathless Run）
* 手動終了
* 指定した Pinnacle Boss の撃破

死亡追跡には 3 つの選択肢があります。
* 死亡を追跡しない
* 最初の死亡のみ
* 死亡回数を追跡

「最初の死亡」または死亡追跡を選択した場合、ゲーム内に表示されているキャラクター名を正確に入力する必要があります。クライアントログからキャラクターの死亡を判定するためです。

一時停止ポリシーは 2 種類あります。

* ボス撃破を Map 完了イベントとし、撃破後の最初の退出でスプリットを終了します。PoE2 の Map 完了ポリシーに近い方式です。
* 代替ポリシー: タイマーが停止するのはロード画面、手動ポーズ、マイクロトランザクションメニュー（有効な場合）のみです。Map 準備、インベントリ整理、戦利品確認など、それ以外の時間はタイマーが進みます。

# ルートの切り替え

別のルートへ切り替えるには:

1. PoE2 Route Setup を開きます。
2. 新しいルートを選択します。
3. セットアップを再生成します。
4. 新しい `.lss` を LiveSplit で開きます。
5. Scriptable Auto Splitter が **LiveSplit Target** 内の `.asl` を参照していることを確認します。
6. 新しいルートでボス検出が必要なら BossWatcher を起動します。

以前の **LiveSplit Target** の内容は置き換えられます。

---

# ランの開始

セットアップ完了後:

1. Path of Exile 2 を起動します。
2. LiveSplit を起動します。
3. ルートの `.lss` を読み込みます。
4. Scriptable Auto Splitter が正しい `.asl` を使用していることを確認します。
5. ボスを含むルートなら BossWatcher を起動します。
6. ランを開始します。

設定したルート目標はオートスプリッターが自動的に処理します。

---

# 更新

新しいバージョンが公開された場合:

1. **GitHub Releases** から最新インストーラーをダウンロードします。
2. インストーラーを実行します。
3. PoE2 Route Setup を開きます。
4. ルートを再生成します。

個人の LiveSplit レイアウトを置き換える必要はありません。

---

# トラブルシューティング

## ボスでスプリットしない

次を確認してください。

* BossWatcher が実行中である。
* PoE2 Route Setup から BossWatcher を起動した。
* 選択したルートにボス目標が含まれている。
* LiveSplit の Scriptable Auto Splitter が生成された `.asl` を参照している。

---

## エリアでスプリットしない

次を確認してください。

* Path of Exile 2 が実行中である。
* LiveSplit の Scriptable Auto Splitter が正しい `.asl` を参照している。
* 正しい探索ルートを生成した。
* 正しい `.lss` を読み込んでいる。

---

## LiveSplit が間違った splits を開く

次から `.lss` を直接開いてください。

`LiveSplit Target`

または:

**File → Open Splits → From File**

---

## ルートを変更したら動作しなくなった

新しいルートを再生成し、次を確認してください。

* 正しい `.lss` が読み込まれている。
* Scriptable Auto Splitter が **LiveSplit Target** 内の現在の `.asl` を参照している。

---

## BossWatcher がエラーを表示する

BossWatcher を閉じ、PoE2 Route Setup の **Start BossWatcher** ボタンから再起動してください。

問題が続く場合は、問題報告時に表示されたエラーを添付してください。

---
## BossWatcher が早すぎるスプリット、またはプレイヤー死亡時にスプリットした

BossWatcher はボスのヘルスバーが画面から消えたことを記録します。これはさまざまな理由で起こり得るため、そのスプリットが正しいかどうかはユーザーが判断してください。基本的にはボスが死亡したと仮定してスプリットします。ボスを倒していないのにスプリットした場合は、split undo を使うと以前の状態に戻り、現在の時間からボスへ再挑戦できます。Split undo のホットキーは LiveSplit の設定にあります。

---

# LiveSplit 用に生成されるファイル

選択したルートに応じて **LiveSplit Target** には以下が含まれます。

### `.lss`

LiveSplit のスプリット一覧です。

### `.asl`

LiveSplit の Scriptable Auto Splitter コンポーネントで使用するオートスプリッタースクリプトです。

### ルート / 設定ファイル

選択したルートに含まれるエリアやボスをオートスプリッターへ伝えます。

### ボスイベントファイル

BossWatcher およびボス対応オートスプリッターが使用します。

内容を理解している場合を除き、これらを手動編集しないでください。

通常は **PoE2 Route Setup** から生成してください。

---

# 重要

PoE2 Route AutoSplitter は個人の LiveSplit レイアウトを制御したり置き換えたり**しません**。

以下はユーザー自身が管理します。

* タイマーの外観
* スプリットの色
* フォント
* ウィンドウサイズ
* 比較設定
* その他の LiveSplit コンポーネント

PoE2 Route AutoSplitter が提供するのは、ルートのスプリットとオートスプリッター設定のみです。

---

# 問題を報告する場合

次の情報を含めてください。

* PoE2 Route AutoSplitter のバージョン
* 使用しているルート / モード
* BossWatcher が実行されていたか
* 期待していた動作
* 実際に起きた動作
* PoE2 Route Setup、BossWatcher、LiveSplit が表示したエラーメッセージ

これらの情報があると、問題を再現して修正しやすくなります。

---

# パッケージ検証と診断

リリース / ランタイムファイルを検証する SHA-256 マニフェストは次にあります。

`3 - verification files`

セットアップ検証マニフェスト、各ランの SHA-256 マニフェスト、監査ログ、読みやすいラン概要もここに保存されます。これらは `LiveSplit Target` の外に保持されるため、新しいルートを生成しても以前のラン監査ファイルは削除されません。

SetupUI、BossWatcher、GameTimeWatcher の診断ログは次へ集約されます。

`4-README's_and_Diagnostics\Diagnostics`

診断用 PNG キャプチャは次へ保存されます。

`4-README's_and_Diagnostics\Diagnostics\images`

---

# 現在のメジャーバージョン

**PoE2 Route AutoSplitter 3.x**

Version 3 では、SetupUI とゲーム言語の多言語対応、確認済みのボス / エリア名のローカライズ、Campaign / Trials / Vaal Ruins / Maps の拡張ポリシー、診断・検証ファイルの一元管理、さらに標準 16:9・ウルトラワイド・スーパーウルトラワイドのゲームクライアントに対応する高さ基準の BossWatcher 自動キャプチャジオメトリが追加されています。
