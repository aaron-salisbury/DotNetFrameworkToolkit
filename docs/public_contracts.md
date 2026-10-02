# Results and validation contracts

## Results

| API | Contract |
| --- | --- |
| `ProcessResult<T>.Success(value)` | Successful even when the value is null, false, or itself an exception. Prefer the factory when `T` is `Exception` to make the intended outcome explicit. |
| `ProcessResult<T>.Failure(error)` | Requires a non-null exception and retains that original object. `Value` throws a new `InvalidOperationException` with the original exception as its inner exception; repeated reads preserve the original stack. |
| `ProcessResult<T, TError>` | The enum's zero/default value is reserved for success. `Failure(default)` throws. Every nonzero value is a failure, including undefined enum values and values with wide underlying types. Consumers choose whether undefined values are meaningful. |
| `ValueOrDefault` and `TryGet` | Return the successful value or `default(T)` on failure. `TryGet` reports success independently of the returned value. |
| Boolean conversion | Reports operation success. A null result converts to false; a successful false value converts to true. |
| `LogAndForwardException` | Requires an original exception and a logger. Returns a failure wrapping the original exception with the supplied context. A logging failure is retained in `Error.Data["LoggingException"]` rather than replacing the original diagnostic. |

Use enum results for expected outcomes and exception results only when a boundary deliberately captures an exception. Result construction itself does not log.

## Validation results

`ValidationResult<T>` copies input collections and property-message arrays. Returned property arrays are copies and general messages are read-only. Null or empty property arrays are omitted. Every retained message entry makes the result invalid, even an empty string; consumers should supply useful message text. Null collections mean no messages. The value is retained by reference rather than cloned.

## Observable validation

`ObservableValidator` belongs to its application's UI thread; it provides no concurrent mutation guarantee. Use the protected setters instead of directly mutating its protected collections to preserve notifications.

| Operation | Notification rule |
| --- | --- |
| Replace property errors | Compare ordered message contents. On change, commit the copied errors, raise `ErrorsChangedCore(propertyName)`, then `PropertyChanged("HasErrors")`. Identical contents raise nothing. |
| Replace entity errors | On change, commit the copied errors, raise `ErrorsChangedCore(string.Empty)`, then `PropertyChanged("Error")` and `PropertyChanged("HasErrors")`. Identical contents raise nothing. |
| `HasErrors` notification | Signals changed error contents, even when the boolean value remains true because other errors remain. |
| `RaisePropertyChangedWithValidation(name)` | Validates and sends any error notifications first, then sends the named property notification even when its error contents did not change. |
| `PropertyIsValid(name)` | Requires a real property name. Updates that property's errors and returns whether its errors are empty. A null validation list is rejected without replacing the previous errors. |
| `IsValid()` | Validates properties incrementally and includes existing entity errors in overall validity. It is not an atomic validation transaction; an exception can leave earlier property updates committed. |
| Error queries | Return copies. Null/empty `GetErrors` names query entity errors. Named queries return that property's stored errors; the `IDataErrorInfo` indexer performs validation first. |
| Observer exceptions | Propagate synchronously after the mutation has been committed. Subsequent handlers/notifications may not run. Retrying identical errors does not replay missed notifications. UI event handlers should handle their own failures. |

Validation events follow ordinary .NET event exception behavior. The logging sink's best-effort observer policy is specific to diagnostics.
