namespace Hefi.Api.Dtos;


// request payload for creating a new workout record.
public record WorkoutCreate(
    string WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    DateTime? PerformedAt
);


// represents a persisted workout record associated with a user.

public record WorkoutDto(
    int Id,
    int UserId,
    string WorkoutType,
    int DurationMinutes,
    int CaloriesBurned,
    DateTime PerformedAt
);
