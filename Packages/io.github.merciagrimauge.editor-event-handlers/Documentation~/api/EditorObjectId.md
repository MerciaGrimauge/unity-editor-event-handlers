# `EditorObjectId`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

Unityのバージョン差を吸収する一時的なオブジェクト識別子です。

## 定義

```csharp
public readonly struct EditorObjectId : IEquatable<EditorObjectId>
```

Identities may be reused after destruction. Do not retain across Unity sessions or domains. Comparisons do not resolve objects.

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public bool IsValid { get; }](#member-11181cc8ed0a) | Whether this is a non-default identity; the object may already have been destroyed. |
| [public static EditorObjectId FromObject(UnityEngine.Object target)](#member-285913dd9269) | Captures a live object's identity on the editor main thread. Null or destroyed objects return default. |
| [public UnityEngine.Object Resolve()](#member-1af2b5a19510) | Resolves the identity on the editor main thread. Returns null when it cannot be resolved. |
| [public bool Equals(EditorObjectId other)](#member-2cfa42bfd42b) | Compares complete identities without resolving Unity objects; any thread may compare. |
| [public override bool Equals(object obj)](#member-18e238138d9e) | Overrides System.Object.Equals(object). |
| [public override int GetHashCode()](#member-54bf7150b4e2) | Hash for temporary collections; not a unique or persistent object identifier. |
| [public static bool operator ==(EditorObjectId left, EditorObjectId right)](#member-2f859b10fc4b) | Compares identities for equality without resolving objects. |
| [public static bool operator !=(EditorObjectId left, EditorObjectId right)](#member-6dca5bfc42f1) | Compares identities for inequality without resolving objects. |

<a id="member-11181cc8ed0a"></a>

## `IsValid`

```csharp
public bool IsValid { get; }
```

Whether this is a non-default identity; the object may already have been destroyed.

<a id="member-285913dd9269"></a>

## `FromObject`

```csharp
public static EditorObjectId FromObject(UnityEngine.Object target)
```

Captures a live object's identity on the editor main thread. Null or destroyed objects return default.

### 引数

| 名前 | 説明 |
|---|---|
| `target` | Object to identify. |

### 戻り値

Temporary identity, or default for null or a destroyed object.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

<a id="member-1af2b5a19510"></a>

## `Resolve`

```csharp
public UnityEngine.Object Resolve()
```

Resolves the identity on the editor main thread. Returns null when it cannot be resolved.

### 戻り値

Current Unity object, or null for default or an unavailable identity.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

<a id="member-2cfa42bfd42b"></a>

## `Equals`

```csharp
public bool Equals(EditorObjectId other)
```

Compares complete identities without resolving Unity objects; any thread may compare.

### 引数

| 名前 | 説明 |
|---|---|
| `other` | Identity to compare. |

### 戻り値

Whether both identities are equal.

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

Hash for temporary collections; not a unique or persistent object identifier.

<a id="member-2f859b10fc4b"></a>

## op_Equality

```csharp
public static bool operator ==(EditorObjectId left, EditorObjectId right)
```

Compares identities for equality without resolving objects.

<a id="member-6dca5bfc42f1"></a>

## op_Inequality

```csharp
public static bool operator !=(EditorObjectId left, EditorObjectId right)
```

Compares identities for inequality without resolving objects.

## 使用上の注意

`readonly struct : IEquatable<EditorObjectId>`。Unityのバージョン差を吸収する一時的なオブジェクト識別子です。生の数値、数値変換、シリアライズAPIは公開しません。


IsValid・比較・hashにはメインスレッド制約はありません。Unityセッションやドメインをまたいで保存しないでください。破棄後に識別子が再利用される可能性もあるため、IsValidのみで参照の存続を判断しないでください。

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

比較・hash・`IsValid`は対象を解決しません。現在の存続を調べる場合は`Resolve()`の結果を確認します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
