namespace FikaForecast.Application.DTOs;

/// <summary>
/// Reports progress during a sequential batch comparison run.
/// </summary>
/// <param name="CompletedModels">Number of models finished so far.</param>
/// <param name="TotalModels">Total number of models in this batch.</param>
/// <param name="CurrentModelName">Display name of the model currently running (null when done).</param>
public record BatchProgress(int CompletedModels, int TotalModels, string? CurrentModelName);
