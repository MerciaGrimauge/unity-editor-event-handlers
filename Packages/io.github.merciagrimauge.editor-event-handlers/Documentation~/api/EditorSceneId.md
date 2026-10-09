# `EditorSceneId`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

Unityのバージョン差を吸収する一時的なシーン識別子です。

## 定義

```csharp
public readonly struct EditorSceneId : IEquatable<EditorSceneId>
```

Unity のセッションやドメインをまたいで保存しないでください。比較時はシーンの存在や読み込み状態を確認しません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public bool IsValid { get; }](#member-c16f2994c0cc) | 識別子が既定値以外かどうかです。シーンがすでに存在しない、または読み込み済みではない場合があります。 |
| [public static EditorSceneId FromScene(UnityEngine.SceneManagement.Scene scene)](#member-31afd7b80345) | Editor のメインスレッドでシーンの識別子を取得します。無効なシーンなら default を返します。 |
| [public bool Equals(EditorSceneId other)](#member-002f54545c97) | シーンの読み込み状態を確認せず、識別子全体を比較します。どのスレッドでも比較できます。 |
| [public override bool Equals(object obj)](#member-c8ee11ffdb9d) | System.Object.Equals(object) をオーバーライドします。 |
| [public override int GetHashCode()](#member-497956f98383) | 一時的なコレクションに使うハッシュ値です。一意または永続的なシーン識別子ではありません。 |
| [public static bool operator ==(EditorSceneId left, EditorSceneId right)](#member-7c888896ea70) | シーンの存在や読み込み状態を確認せず、識別子が等しいか比較します。 |
| [public static bool operator !=(EditorSceneId left, EditorSceneId right)](#member-0b5ae3601b50) | シーンの存在や読み込み状態を確認せず、識別子が異なるか比較します。 |

<a id="member-c16f2994c0cc"></a>

## `IsValid`

```csharp
public bool IsValid { get; }
```

識別子が既定値以外かどうかです。シーンがすでに存在しない、または読み込み済みではない場合があります。

<a id="member-31afd7b80345"></a>

## `FromScene`

```csharp
public static EditorSceneId FromScene(UnityEngine.SceneManagement.Scene scene)
```

Editor のメインスレッドでシーンの識別子を取得します。無効なシーンなら default を返します。

### 引数

| 名前 | 説明 |
|---|---|
| `scene` | 識別子を取得するシーンです。 |

### 戻り値

一時的な識別子です。無効なシーンなら default です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

<a id="member-002f54545c97"></a>

## `Equals`

```csharp
public bool Equals(EditorSceneId other)
```

シーンの読み込み状態を確認せず、識別子全体を比較します。どのスレッドでも比較できます。

### 引数

| 名前 | 説明 |
|---|---|
| `other` | 比較対象の識別子です。 |

### 戻り値

両方の識別子が等しいかどうかです。

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

一時的なコレクションに使うハッシュ値です。一意または永続的なシーン識別子ではありません。

<a id="member-7c888896ea70"></a>

## op_Equality

```csharp
public static bool operator ==(EditorSceneId left, EditorSceneId right)
```

シーンの存在や読み込み状態を確認せず、識別子が等しいか比較します。

<a id="member-0b5ae3601b50"></a>

## op_Inequality

```csharp
public static bool operator !=(EditorSceneId left, EditorSceneId right)
```

シーンの存在や読み込み状態を確認せず、識別子が異なるか比較します。

## 使用上の注意

`readonly struct : IEquatable<EditorSceneId>`。一時的なシーン識別子です。


Resolveや生のhandleを読むAPIはありません。IsValid・比較・ハッシュ値の取得にはメインスレッド制約はありません。永続保存には使用しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
