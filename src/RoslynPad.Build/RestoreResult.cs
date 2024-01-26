namespace RoslynPad.Build;

internal class RestoreResult
{
    public static RestoreResult SuccessResult { get; } = new RestoreResult(success: true, errors: null);

    public static RestoreResult FromErrors(string[] errors) => new(success: false, errors);

    private RestoreResult(bool success, string[]? errors)
    {
        Success = success;
        Errors = errors ?? [];
    }

    public bool Success { get; }
    public string[] Errors { get; }
}
