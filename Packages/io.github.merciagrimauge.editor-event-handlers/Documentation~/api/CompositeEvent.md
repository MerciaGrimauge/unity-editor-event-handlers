# `CompositeEvent`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

AND/ORで選択した条件の通知を型ごとに取得です。

## 定義

```csharp
public sealed class CompositeEvent
```

The collection is immutable; contained notification objects retain their original references. Any exposes only its first matching result.

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public EditorChange Change { get; }](#member-413da24e39dd) | The input change shared by every evaluated condition in this combination. |
| [public IReadOnlyList&lt;Type&gt; RequiredTypes { get; }](#member-386c537bfaec) | Immutable required notification types in the subscription's declaration order. |
| [public bool TryGet&lt;TEvent&gt;(out TEvent notification)](#member-be21240ee1d1) | Reads a selected notification by its exact declared type. |

<a id="member-413da24e39dd"></a>

## `Change`

```csharp
public EditorChange Change { get; }
```

The input change shared by every evaluated condition in this combination.

<a id="member-386c537bfaec"></a>

## `RequiredTypes`

```csharp
public IReadOnlyList<Type> RequiredTypes { get; }
```

Immutable required notification types in the subscription's declaration order.

<a id="member-be21240ee1d1"></a>

## `TryGet<T>`

```csharp
public bool TryGet<TEvent>(out TEvent notification)
```

Reads a selected notification by its exact declared type.

### 型パラメーター

| 名前 | 説明 |
|---|---|
| `TEvent` | Exact condition notification type; derived or assignable types are not substituted. |

### 引数

| 名前 | 説明 |
|---|---|
| `notification` | Saved result when present, otherwise the type's default value. |

### 戻り値

Whether the combination exposes a notification of this exact type.

## 使用上の注意

複合購読へ渡すsealed classです。公開コンストラクターはありません。


Allでは全条件の通知を取得できます。Anyでは登録した一覧の先頭から最初に一致した1型だけを取得でき、同じ入力で他の条件も一致していても含めません。Anyの編集範囲は選択された条件のRootです。Allは全条件のRootが同一の場合だけ発火し、異なるRootを合成して編集範囲を広げません。

すべての依存条件が登録済みかつ有効であることが両モードの前提です。条件の不在・無効化・異常は「不一致」としてORで迂回せず、その購読を待機させます。入力種類を購読しない条件プロバイダーの結果は、その入力では不一致です。

必要な条件プロバイダーは入力ごとに各1回評価し、単一購読・複合購読で結果を共有します。全バッチの条件プロバイダー評価後にイベントハンドラーを実行します。ORの短絡は保存済み結果を選ぶ処理にだけ適用し、最初の一致が見つかった後も必要な条件プロバイダーの評価を省略しません。通知内のUnity参照は現在の対象であり、完全な状態コピーではありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
