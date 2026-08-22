// Tasks.Api/Requests/AssignTaskRequest.cs
namespace Tasks.Api.Requests;

public sealed record AssignTaskRequest(Guid AssigneeUserId);