namespace BuildCleaner.Startup.Result;

public static class ResultExtensions
{
    /// <summary>
    /// Chains the result to the next operation if successful, otherwise propagates the error.
    /// </summary>
    /// <typeparam name="TSource">The type of the value in the input result.</typeparam>
    /// <typeparam name="TResult">The type of the value in the output result.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="func">The function to apply if the result is successful.</param>
    /// <returns>A new result containing either the next value or the original error.</returns>
    public static IResult<TResult> Bind<TSource, TResult>(this IResult<TSource> result, Func<TSource, IResult<TResult>> func) =>
        result.IsSuccess ? func(result.Value) : Results.Fail<TResult>(result.Error);

    /// <summary>
    /// Executes an action if the result is a failure, then returns the original result.
    /// </summary>
    /// <typeparam name="T">The type of the value in the result.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="action">The action to execute on failure.</param>
    /// <returns>The original result.</returns>
    public static IResult<T> OnFailure<T>(this IResult<T> result, Action<string> action)
    {
        if (!result.IsSuccess)
        {
            action(result.Error);
        }
        
        return result;
    }

    /// <summary>
    /// Matches on the result, executing either the success or failure function.
    /// </summary>
    /// <typeparam name="T">The type of the value in the result.</typeparam>
    /// <typeparam name="TOut">The return type of the match functions.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="onSuccess">Function to execute if successful.</param>
    /// <param name="onFailure">Function to execute if failed.</param>
    /// <returns>The result of the executed function.</returns>
    public static TOut Match<T, TOut>(this IResult<T> result, Func<T, TOut> onSuccess, Func<string, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);

    /// <summary>
    /// Asynchronously matches on the result, executing either the success or failure function.
    /// </summary>
    /// <typeparam name="T">The type of the value in the result.</typeparam>
    /// <typeparam name="TOut">The return type of the match functions.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="onSuccess">Async function to execute if successful.</param>
    /// <param name="onFailure">Async function to execute if failed.</param>
    /// <returns>A task representing the result of the executed function.</returns>
    public static Task<TOut> MatchAsync<T, TOut>(this IResult<T> result, Func<T, Task<TOut>> onSuccess, Func<string, Task<TOut>> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
}
