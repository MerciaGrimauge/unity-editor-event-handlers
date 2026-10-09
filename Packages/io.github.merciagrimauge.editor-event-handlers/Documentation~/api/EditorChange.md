# `EditorChange`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

Unity変更の種類・識別子・現在の対象参照です。

## 定義

```csharp
public readonly struct EditorChange : IEquatable<EditorChange>
```

識別子から取得する Unity 参照は現在の状態の確認用で、null の場合があります。変更を起こしたユーザーを識別する情報ではありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public EditorChangeKind Kind { get; }](#member-8f625f1983eb) | 入力通知の変更の種類です。 |
| [public EditorObjectId ObjectId { get; }](#member-2ad54d145ab4) | 通知対象の一時的な識別子です。 |
| [public EditorSceneId SceneId { get; }](#member-f5e32bc32e9b) | 通知時点のシーン識別子です。親変更の場合は新しい所属シーンです。 |
| [public EditorSceneId PreviousSceneId { get; }](#member-c28d6fd26eca) | 親変更前の所属シーンです。破棄の場合は SceneId と同じ値、それ以外は default です。 |
| [public EditorObjectId PreviousParentId { get; }](#member-5aaee8265f2f) | 親変更前の親、または破棄直前の親です。それ以外は default です。 |
| [public EditorObjectId NewParentId { get; }](#member-b46e44022508) | 親変更時の新しい親です。それ以外は default です。 |
| [public UnityEngine.Object Target { get; }](#member-74b60158dc49) | Editor のメインスレッドで現在の対象への参照を取得します。取得できない場合は null です。 |
| [public GameObject GameObject { get; }](#member-b26a07e929b0) | Editor のメインスレッドで対象の GameObject、または対象 Component が属する GameObject を取得します。取得できない場合は null です。 |
| [public GameObject PreviousParent { get; }](#member-2376e7f1650f) | Editor のメインスレッドで変更前の親の現在の GameObject を取得します。取得できない場合は null です。 |
| [public GameObject NewParent { get; }](#member-6b7440a7d128) | Editor のメインスレッドで新しい親の現在の GameObject を取得します。取得できない場合は null です。 |
| [public bool Equals(EditorChange other)](#member-720334c64a6e) | Unity オブジェクトへの参照を取得せず、変更の種類と保存した全識別子を比較します。 |
| [public override bool Equals(object obj)](#member-7dad5a325d0e) | System.Object.Equals(object) をオーバーライドします。 |
| [public override int GetHashCode()](#member-9df9da68d15f) | 一時的なコレクションに使う、変更の種類と全識別子のハッシュ値です。 |

<a id="member-8f625f1983eb"></a>

## `Kind`

```csharp
public EditorChangeKind Kind { get; }
```

入力通知の変更の種類です。

<a id="member-2ad54d145ab4"></a>

## `ObjectId`

```csharp
public EditorObjectId ObjectId { get; }
```

通知対象の一時的な識別子です。

<a id="member-f5e32bc32e9b"></a>

## `SceneId`

```csharp
public EditorSceneId SceneId { get; }
```

通知時点のシーン識別子です。親変更の場合は新しい所属シーンです。

<a id="member-c28d6fd26eca"></a>

## `PreviousSceneId`

```csharp
public EditorSceneId PreviousSceneId { get; }
```

親変更前の所属シーンです。破棄の場合は SceneId と同じ値、それ以外は default です。

<a id="member-5aaee8265f2f"></a>

## `PreviousParentId`

```csharp
public EditorObjectId PreviousParentId { get; }
```

親変更前の親、または破棄直前の親です。それ以外は default です。

<a id="member-b46e44022508"></a>

## `NewParentId`

```csharp
public EditorObjectId NewParentId { get; }
```

親変更時の新しい親です。それ以外は default です。

<a id="member-74b60158dc49"></a>

## `Target`

```csharp
public UnityEngine.Object Target { get; }
```

Editor のメインスレッドで現在の対象への参照を取得します。取得できない場合は null です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

<a id="member-b26a07e929b0"></a>

## `GameObject`

```csharp
public GameObject GameObject { get; }
```

Editor のメインスレッドで対象の GameObject、または対象 Component が属する GameObject を取得します。取得できない場合は null です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

<a id="member-2376e7f1650f"></a>

## `PreviousParent`

```csharp
public GameObject PreviousParent { get; }
```

Editor のメインスレッドで変更前の親の現在の GameObject を取得します。取得できない場合は null です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

<a id="member-6b7440a7d128"></a>

## `NewParent`

```csharp
public GameObject NewParent { get; }
```

Editor のメインスレッドで新しい親の現在の GameObject を取得します。取得できない場合は null です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

<a id="member-720334c64a6e"></a>

## `Equals`

```csharp
public bool Equals(EditorChange other)
```

Unity オブジェクトへの参照を取得せず、変更の種類と保存した全識別子を比較します。

### 引数

| 名前 | 説明 |
|---|---|
| `other` | 比較対象の変更です。 |

### 戻り値

保存した全フィールドが等しいかどうかです。

<a id="member-7dad5a325d0e"></a>

## `Equals`

```csharp
public override bool Equals(object obj)
```

System.Object.Equals(object)をオーバーライドし、同じ識別子かを比較します。

<a id="member-9df9da68d15f"></a>

## `GetHashCode`

```csharp
public override int GetHashCode()
```

一時的なコレクションに使う、変更の種類と全識別子のハッシュ値です。

## 使用上の注意

`readonly struct : IEquatable<EditorChange>`。管理側が作成する変更のスナップショットです。公開コンストラクターはありません。


Target/GameObject/親の解決はメインスレッド専用です。通知時の種類・IDは保存されますが、対象のプロパティを凍結しません。参照先の状態は判定・実行時点のものです。Destroyedの対象自身を通常は解決できません。

`Equals(EditorChange)`、`Equals(object)`、`GetHashCode()` はKindと全識別子を比較します。これが同じ更新までの重複排除の単位です。異なる親遷移は別の入力です。defaultの変更は有効な入力通知ではありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
