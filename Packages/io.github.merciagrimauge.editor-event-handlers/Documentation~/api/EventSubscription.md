# `EventSubscription`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

購読の状態・直近結果・解除トークンです。

## 定義

```csharp
public sealed class EventSubscription : IDisposable
```

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public string Id { get; }](#member-1bb8bf4ed03d) | 登録時に保存した識別子です。 |
| [public Type EventType { get; }](#member-ad6c4d89651d) | ハンドラーへ渡す通知の型です。All または Any 購読では CompositeEvent です。 |
| [public SubscriptionMode Mode { get; }](#member-2a926cc77565) | 登録時に保存した条件の組み合わせ方です。 |
| [public IReadOnlyList&lt;Type&gt; ConditionTypes { get; }](#member-27f4f9333f99) | 指定順に並んだ、変更できない条件の通知型一覧です。 |
| [public TimeSpan TimeLimit { get; }](#member-856f94e40421) | ユーザーが設定した現在の期限です。各呼び出しは開始時の値を保持します。 |
| [public bool IsDisposed { get; }](#member-df1809a6b779) | 購読が明示的に解除されているかどうかです。 |
| [public bool IsEnabled { get; }](#member-336e62e8fb71) | ユーザー設定と異常時の方針に基づき、現在この購読が有効かどうかです。 |
| [public bool IsActive { get; }](#member-68eeaaf5b414) | 有効かつ未解除で、必要な全条件が有効かどうかです。Editor のメインスレッドで取得してください。 |
| [public string DisabledReason { get; }](#member-b64ca99fd124) | 無効化の理由です。有効な購読の初期値は空文字列です。 |
| [public HandlerResult LastResult { get; }](#member-9aba67b54815) | 直近の終了結果です。初回呼び出し前は Unspecified です。 |
| [public TimeSpan LastDuration { get; }](#member-ea8f72146cc0) | 直近の呼び出し時間です。変更の確定・復元にかかった時間は含みません。初回使用前はゼロです。 |
| [public void Dispose()](#member-f6917fdae8ca) | この購読を解除して登録枠を解放します。繰り返し呼び出しても何もしません。 |

<a id="member-1bb8bf4ed03d"></a>

## `Id`

```csharp
public string Id { get; }
```

登録時に保存した識別子です。

<a id="member-ad6c4d89651d"></a>

## `EventType`

```csharp
public Type EventType { get; }
```

ハンドラーへ渡す通知の型です。All または Any 購読では CompositeEvent です。

<a id="member-2a926cc77565"></a>

## `Mode`

```csharp
public SubscriptionMode Mode { get; }
```

登録時に保存した条件の組み合わせ方です。

<a id="member-27f4f9333f99"></a>

## `ConditionTypes`

```csharp
public IReadOnlyList<Type> ConditionTypes { get; }
```

指定順に並んだ、変更できない条件の通知型一覧です。

<a id="member-856f94e40421"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

ユーザーが設定した現在の期限です。各呼び出しは開始時の値を保持します。

<a id="member-df1809a6b779"></a>

## `IsDisposed`

```csharp
public bool IsDisposed { get; }
```

購読が明示的に解除されているかどうかです。

<a id="member-336e62e8fb71"></a>

## `IsEnabled`

```csharp
public bool IsEnabled { get; }
```

ユーザー設定と異常時の方針に基づき、現在この購読が有効かどうかです。

<a id="member-68eeaaf5b414"></a>

## `IsActive`

```csharp
public bool IsActive { get; }
```

有効かつ未解除で、必要な全条件が有効かどうかです。Editor のメインスレッドで取得してください。

<a id="member-b64ca99fd124"></a>

## `DisabledReason`

```csharp
public string DisabledReason { get; }
```

無効化の理由です。有効な購読の初期値は空文字列です。

<a id="member-9aba67b54815"></a>

## `LastResult`

```csharp
public HandlerResult LastResult { get; }
```

直近の終了結果です。初回呼び出し前は Unspecified です。

<a id="member-ea8f72146cc0"></a>

## `LastDuration`

```csharp
public TimeSpan LastDuration { get; }
```

直近の呼び出し時間です。変更の確定・復元にかかった時間は含みません。初回使用前はゼロです。

<a id="member-f6917fdae8ca"></a>

## `Dispose`

```csharp
public void Dispose()
```

この購読を解除して登録枠を解放します。繰り返し呼び出しても何もしません。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

## 使用上の注意

公開コンストラクターはありません。


`void Dispose()` は購読を解除し枠を解放します。メインスレッド専用、別スレッドならInvalidOperationException、再Disposeは何もしません。

管理側が実装と登録を強参照します。トークンを手放しただけでは解除されません。登録はEditorドメイン内の寿命を持ち、InitializeOnLoad等で必要に応じて再登録します。バッチ開始時の登録構成で評価し、新規登録は次バッチから対象です。解除・無効化は後続処理へ反映します。依存条件が解除・無効化・別登録に置き換えられた場合、その条件の保存済み結果は使用しません。Anyでも未選択の依存条件の失効は当該配送を止めます。過去通知は再送しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
