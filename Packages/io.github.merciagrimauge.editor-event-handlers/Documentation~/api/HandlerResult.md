# `HandlerResult`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

成功・スキップ・失敗・キャンセルの終了結果です。

## 定義

```csharp
public readonly struct HandlerResult
```

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public HandlerStatus Status { get; }](#member-03c844cc7f93) | 明示的な終了状態です。既定値は Unspecified です。 |
| [public string Message { get; }](#member-c9c6eea44821) | 省略可能な説明です。生成メソッドは null を空文字列に変換します。 |
| [public static HandlerResult Success()](#member-913735074221) | 正常終了を報告します。実行後の確認をすべて通過した場合に、記録した変更を確定します。 |
| [public static HandlerResult Skip(string reason = null)](#member-7c9e0dd7a368) | 処理を行わなかったことを報告します。記録対象の編集後にスキップすると失敗として扱います。 |
| [public static HandlerResult Failure(string reason)](#member-63096106b7e3) | 失敗を報告します。ディスパッチャーは記録した変更の復元を試み、購読を無効化します。 |
| [public static HandlerResult Cancel(string reason = null)](#member-69cac07e885f) | キャンセルを報告します。ディスパッチャーは記録した変更の復元を試み、購読を無効化します。 |

<a id="member-03c844cc7f93"></a>

## `Status`

```csharp
public HandlerStatus Status { get; }
```

明示的な終了状態です。既定値は Unspecified です。

<a id="member-c9c6eea44821"></a>

## `Message`

```csharp
public string Message { get; }
```

省略可能な説明です。生成メソッドは null を空文字列に変換します。

<a id="member-913735074221"></a>

## `Success`

```csharp
public static HandlerResult Success()
```

正常終了を報告します。実行後の確認をすべて通過した場合に、記録した変更を確定します。

### 戻り値

成功を表す結果です。

<a id="member-7c9e0dd7a368"></a>

## `Skip`

```csharp
public static HandlerResult Skip(string reason = null)
```

処理を行わなかったことを報告します。記録対象の編集後にスキップすると失敗として扱います。

### 引数

| 名前 | 説明 |
|---|---|
| `reason` | 省略可能な説明です。 |

### 戻り値

スキップを表す結果です。

<a id="member-63096106b7e3"></a>

## `Failure`

```csharp
public static HandlerResult Failure(string reason)
```

失敗を報告します。ディスパッチャーは記録した変更の復元を試み、購読を無効化します。

### 引数

| 名前 | 説明 |
|---|---|
| `reason` | 失敗理由です。null は空文字列に変換します。 |

### 戻り値

失敗を表す結果です。

<a id="member-69cac07e885f"></a>

## `Cancel`

```csharp
public static HandlerResult Cancel(string reason = null)
```

キャンセルを報告します。ディスパッチャーは記録した変更の復元を試み、購読を無効化します。

### 引数

| 名前 | 説明 |
|---|---|
| `reason` | 省略可能な説明です。 |

### 戻り値

キャンセルを表す結果です。

## 使用上の注意

HandlerStatusは `Unspecified = 0, Succeeded = 1, Skipped = 2, Failed = 3, Cancelled = 4` です。HandlerResultは読み取り専用の構造体で、公開プロパティは `HandlerStatus Status`、`string Message` です。各生成メソッドは、理由が null の場合に空文字列へ変換します。


ハンドラーの例外・期限超過はFailureへ変換し、`OperationCanceledException` はCancelledへ変換します。復元が成功すれば次の購読を実行できます。復元・確定の例外、または実行後のRoot喪失では全体を即時停止します。

Successを返しても、実行後にRootが失われていればLastResultをFailureへ変更し、そのイベントハンドラーを異常無効として保存します。Context外で破壊されたRootの復元を意味しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
