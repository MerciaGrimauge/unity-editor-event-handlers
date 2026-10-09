# `EditorObjectId`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

Unityのバージョン差を吸収する一時的なオブジェクト識別子です。

## 定義

```csharp
public readonly struct EditorObjectId : IEquatable<EditorObjectId>
```

破棄後に識別子が再利用される場合があります。Unity のセッションやドメインをまたいで保持しないでください。比較時はオブジェクトへの参照を取得しません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public bool IsValid { get; }](#member-11181cc8ed0a) | 識別子が既定値以外かどうかです。オブジェクトはすでに破棄されている場合があります。 |
| [public static EditorObjectId FromObject(UnityEngine.Object target)](#member-285913dd9269) | Editor のメインスレッドで存続しているオブジェクトの識別子を取得します。null または破棄済みなら default を返します。 |
| [public UnityEngine.Object Resolve()](#member-1af2b5a19510) | Editor のメインスレッドで識別子からオブジェクトへの参照を取得します。取得できない場合は null を返します。 |
| [public bool Equals(EditorObjectId other)](#member-2cfa42bfd42b) | Unity オブジェクトへの参照を取得せず、識別子全体を比較します。どのスレッドでも比較できます。 |
| [public override bool Equals(object obj)](#member-18e238138d9e) | System.Object.Equals(object) をオーバーライドします。 |
| [public override int GetHashCode()](#member-54bf7150b4e2) | 一時的なコレクションに使うハッシュ値です。一意または永続的なオブジェクト識別子ではありません。 |
| [public static bool operator ==(EditorObjectId left, EditorObjectId right)](#member-2f859b10fc4b) | オブジェクトへの参照を取得せず、識別子が等しいか比較します。 |
| [public static bool operator !=(EditorObjectId left, EditorObjectId right)](#member-6dca5bfc42f1) | オブジェクトへの参照を取得せず、識別子が異なるか比較します。 |

<a id="member-11181cc8ed0a"></a>

## `IsValid`

```csharp
public bool IsValid { get; }
```

識別子が既定値以外かどうかです。オブジェクトはすでに破棄されている場合があります。

<a id="member-285913dd9269"></a>

## `FromObject`

```csharp
public static EditorObjectId FromObject(UnityEngine.Object target)
```

Editor のメインスレッドで存続しているオブジェクトの識別子を取得します。null または破棄済みなら default を返します。

### 引数

| 名前 | 説明 |
|---|---|
| `target` | 識別子を取得するオブジェクトです。 |

### 戻り値

一時的な識別子です。null または破棄済みのオブジェクトなら default です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

<a id="member-1af2b5a19510"></a>

## `Resolve`

```csharp
public UnityEngine.Object Resolve()
```

Editor のメインスレッドで識別子からオブジェクトへの参照を取得します。取得できない場合は null を返します。

### 戻り値

現在の Unity オブジェクトです。識別子が default または参照を取得できない場合は null です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

<a id="member-2cfa42bfd42b"></a>

## `Equals`

```csharp
public bool Equals(EditorObjectId other)
```

Unity オブジェクトへの参照を取得せず、識別子全体を比較します。どのスレッドでも比較できます。

### 引数

| 名前 | 説明 |
|---|---|
| `other` | 比較対象の識別子です。 |

### 戻り値

両方の識別子が等しいかどうかです。

<a id="member-18e238138d9e"></a>

## `Equals`

```csharp
public override bool Equals(object obj)
```

System.Object.Equals(object)をオーバーライドし、同じ識別子かを比較します。

<a id="member-54bf7150b4e2"></a>

## `GetHashCode`

```csharp
public override int GetHashCode()
```

一時的なコレクションに使うハッシュ値です。一意または永続的なオブジェクト識別子ではありません。

<a id="member-2f859b10fc4b"></a>

## op_Equality

```csharp
public static bool operator ==(EditorObjectId left, EditorObjectId right)
```

オブジェクトへの参照を取得せず、識別子が等しいか比較します。

<a id="member-6dca5bfc42f1"></a>

## op_Inequality

```csharp
public static bool operator !=(EditorObjectId left, EditorObjectId right)
```

オブジェクトへの参照を取得せず、識別子が異なるか比較します。

## 使用上の注意

`readonly struct : IEquatable<EditorObjectId>`。Unityのバージョン差を吸収する一時的なオブジェクト識別子です。生の数値、数値変換、シリアライズAPIは公開しません。


IsValid・比較・ハッシュ値の取得にはメインスレッド制約はありません。Unityセッションやドメインをまたいで保存しないでください。破棄後に識別子が再利用される可能性もあるため、IsValidのみで参照の存続を判断しないでください。

## 使用例

次のコードは条件の`TryMatch`内に置く断片です。現在の対象と通知時の識別子・シーンを比較します。

```csharp
var target = context.Change.GameObject;
if (target == null)
{
    match = default;
    return false;
}
bool sameObject = EditorObjectId.FromObject(target) == context.Change.ObjectId;
bool sameScene = EditorSceneId.FromScene(target.scene) == context.Change.SceneId;
```

比較・ハッシュ値の取得・`IsValid`は対象を解決しません。現在の存続を調べる場合は`Resolve()`の結果を確認します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
