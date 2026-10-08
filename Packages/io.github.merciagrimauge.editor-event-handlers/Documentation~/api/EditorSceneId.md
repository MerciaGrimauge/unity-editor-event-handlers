# `EditorSceneId`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

Unityのバージョン差を吸収する一時的なシーン識別子です。

## 定義

```csharp
public readonly struct EditorSceneId : IEquatable<EditorSceneId>
```

Do not persist across Unity sessions or domains. Comparisons do not check scene existence or loading.

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public bool IsValid { get; }](#member-c16f2994c0cc) | Whether this is a non-default identity; the scene may no longer exist or be loaded. |
| [public static EditorSceneId FromScene(UnityEngine.SceneManagement.Scene scene)](#member-31afd7b80345) | Captures a scene's identity on the editor main thread. Invalid scenes return default. |
| [public bool Equals(EditorSceneId other)](#member-002f54545c97) | Compares complete identities without checking scene loading; any thread may compare. |
| [public override bool Equals(object obj)](#member-c8ee11ffdb9d) | Overrides System.Object.Equals(object). |
| [public override int GetHashCode()](#member-497956f98383) | Hash for temporary collections; not a unique or persistent scene identifier. |
| [public static bool operator ==(EditorSceneId left, EditorSceneId right)](#member-7c888896ea70) | Compares identities for equality without checking scene existence or loading. |
| [public static bool operator !=(EditorSceneId left, EditorSceneId right)](#member-0b5ae3601b50) | Compares identities for inequality without checking scene existence or loading. |

<a id="member-c16f2994c0cc"></a>

## `IsValid`

```csharp
public bool IsValid { get; }
```

Whether this is a non-default identity; the scene may no longer exist or be loaded.

<a id="member-31afd7b80345"></a>

## `FromScene`

```csharp
public static EditorSceneId FromScene(UnityEngine.SceneManagement.Scene scene)
```

Captures a scene's identity on the editor main thread. Invalid scenes return default.

### 引数

| 名前 | 説明 |
|---|---|
| `scene` | Scene to identify. |

### 戻り値

Temporary identity, or default for an invalid scene.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

<a id="member-002f54545c97"></a>

## `Equals`

```csharp
public bool Equals(EditorSceneId other)
```

Compares complete identities without checking scene loading; any thread may compare.

### 引数

| 名前 | 説明 |
|---|---|
| `other` | Identity to compare. |

### 戻り値

Whether both identities are equal.

<a id="member-c8ee11ffdb9d"></a>

## `Equals`

```csharp
public override bool Equals(object obj)
```

System.Object.Equals(object)をオーバーライドし、同じ識別子かを比較します。

<a id="member-497956f98383"></a>

## `GetHashCode`

```csharp
public override int GetHashCode()
```

Hash for temporary collections; not a unique or persistent scene identifier.

<a id="member-7c888896ea70"></a>

## op_Equality

```csharp
public static bool operator ==(EditorSceneId left, EditorSceneId right)
```

Compares identities for equality without checking scene existence or loading.

<a id="member-0b5ae3601b50"></a>

## op_Inequality

```csharp
public static bool operator !=(EditorSceneId left, EditorSceneId right)
```

Compares identities for inequality without checking scene existence or loading.

## 使用上の注意

`readonly struct : IEquatable<EditorSceneId>`。一時的なシーン識別子です。


Resolveや生のhandleを読むAPIはありません。IsValid・比較・hashにはメインスレッド制約はありません。永続保存には使用しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
