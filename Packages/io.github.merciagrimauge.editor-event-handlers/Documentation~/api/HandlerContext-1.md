# `HandlerContext<TEvent>`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

型付き通知と、基底型から継承する編集APIです。

## 定義

```csharp
public sealed class HandlerContext<TEvent> : HandlerContext
```

基底型: [HandlerContext](HandlerContext.md)。編集API・期限・Rootは基底型から継承します。

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public TEvent Event { get; }](#member-117f893247d0) | 購読者間で共有する条件の判定結果です。Unity 参照は状態の確認に使います。 |

<a id="member-117f893247d0"></a>

## `Event`

```csharp
public TEvent Event { get; }
```

購読者間で共有する条件の判定結果です。Unity 参照は状態の確認に使います。

## 使用上の注意

`Event`は条件が生成した型付き通知です。通知値は他の購読者と共有し、含まれるUnity参照は現在状態を指します。使用前に必要な存続・階層・Mesh等を確認してください。

編集API・Root・期限は[HandlerContext](HandlerContext.md)から継承します。判定終了後の通知を完全な状態コピーとして扱わず、コンテキストは実行後に再利用しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
