# `ConditionRegistration`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

条件登録の状態・解除トークンです。

## 定義

```csharp
public sealed class ConditionRegistration : IDisposable
```

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public string Id { get; }](#member-2f04af916893) | 登録時に保存した識別子です。 |
| [public Type EventType { get; }](#member-42254476742f) | この条件が生成する通知の型です。 |
| [public EditorChangeKind Changes { get; }](#member-fcaf429032a1) | 登録時に保存した変更の種類です。 |
| [public TimeSpan TimeLimit { get; }](#member-30d93ae2400a) | ユーザーが設定した現在の期限です。各呼び出しは開始時の値を保持します。 |
| [public bool IsDisposed { get; }](#member-777c2705164a) | 条件登録が明示的に解除されているかどうかです。 |
| [public bool IsEnabled { get; }](#member-c18aee63ed88) | ユーザー設定と異常時の方針に基づき、現在この条件が有効かどうかです。 |
| [public string DisabledReason { get; }](#member-cb89bf583b82) | 無効化の理由です。有効な条件の初期値は空文字列です。 |
| [public TimeSpan LastDuration { get; }](#member-733c4dff35b9) | 直近の評価時間です。初回評価前はゼロです。 |
| [public void Dispose()](#member-158e3eb82bbe) | この条件を解除して登録枠を解放します。購読者は解除しません。繰り返し呼び出しても何もしません。 |

<a id="member-2f04af916893"></a>

## `Id`

```csharp
public string Id { get; }
```

登録時に保存した識別子です。

<a id="member-42254476742f"></a>

## `EventType`

```csharp
public Type EventType { get; }
```

この条件が生成する通知の型です。

<a id="member-fcaf429032a1"></a>

## `Changes`

```csharp
public EditorChangeKind Changes { get; }
```

登録時に保存した変更の種類です。

<a id="member-30d93ae2400a"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

ユーザーが設定した現在の期限です。各呼び出しは開始時の値を保持します。

<a id="member-777c2705164a"></a>

## `IsDisposed`

```csharp
public bool IsDisposed { get; }
```

条件登録が明示的に解除されているかどうかです。

<a id="member-c18aee63ed88"></a>

## `IsEnabled`

```csharp
public bool IsEnabled { get; }
```

ユーザー設定と異常時の方針に基づき、現在この条件が有効かどうかです。

<a id="member-cb89bf583b82"></a>

## `DisabledReason`

```csharp
public string DisabledReason { get; }
```

無効化の理由です。有効な条件の初期値は空文字列です。

<a id="member-733c4dff35b9"></a>

## `LastDuration`

```csharp
public TimeSpan LastDuration { get; }
```

直近の評価時間です。初回評価前はゼロです。

<a id="member-158e3eb82bbe"></a>

## `Dispose`

```csharp
public void Dispose()
```

この条件を解除して登録枠を解放します。購読者は解除しません。繰り返し呼び出しても何もしません。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合です。 |

## 使用上の注意

公開コンストラクターはありません。`Id:string`、`EventType:Type`、`Changes:EditorChangeKind`、`TimeLimit:TimeSpan`、`IsDisposed:bool`、`IsEnabled:bool`、`DisabledReason:string`、`LastDuration:TimeSpan` を読み取れます。LastDurationは初回判定前はゼロです。

`void Dispose()` は条件を解除して枠を解放します。メインスレッド専用で、別スレッドならInvalidOperationExceptionです。同じトークンの再Disposeは何もしません。対応する購読を解除せず、待機へ移します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
