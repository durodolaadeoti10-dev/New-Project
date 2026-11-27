using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");

// PASTE YOUR SUPABASE SESSION POOLER CONNECTION STRING HERE:
// Read the connection string automatically from appsettings.json
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// 1. CALCULATE & SAVE TO SUPABASE  
app.MapPost("/api/calculate", async (StudentInput student) =>
{
    if (string.IsNullOrWhiteSpace(student.FullName) || string.IsNullOrWhiteSpace(student.MatricNumber))
        return Results.BadRequest(new { error = "Name and Matric Number are required." });

    if (student.FirstScore < 0 || student.FirstScore > 100 || student.SecondScore < 0 || student.SecondScore > 100)
        return Results.BadRequest(new { error = "Scores must be between 0 and 100." });

    string departmentName = student.Department switch
    {
        1 => "Mechanical Engineering",
        2 => "Computer Science",
        3 => "Electrical Engineering",
        4 => "Civil Engineering",
        _ => "Unknown Department"
    };

    double totalScore = student.FirstScore + student.SecondScore;
    double averageScore = totalScore / 2.0;

    string grade, remark;
    if (averageScore >= 70) { grade = "A"; remark = "Excellent"; }
    else if (averageScore >= 60) { grade = "B"; remark = "Very Good"; }
    else if (averageScore >= 50) { grade = "C"; remark = "Good"; }
    else if (averageScore >= 45) { grade = "D"; remark = "Fair"; }
    else { grade = "F"; remark = "Fail"; }

    string status = averageScore >= 45 ? "PASS" : "FAIL";

    // Save to Supabase PostgreSQL
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    string sql = @"INSERT INTO student_results 
        (full_name, matric_number, department, university, first_score, second_score, total_score, average_score, grade, remark, status)
        VALUES (@fullName, @matric, @dept, @uni, @first, @second, @total, @avg, @grade, @remark, @status)";

    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("fullName", student.FullName);
    cmd.Parameters.AddWithValue("matric", student.MatricNumber);
    cmd.Parameters.AddWithValue("dept", departmentName);
    cmd.Parameters.AddWithValue("uni", student.University);
    cmd.Parameters.AddWithValue("first", student.FirstScore);
    cmd.Parameters.AddWithValue("second", student.SecondScore);
    cmd.Parameters.AddWithValue("total", totalScore);
    cmd.Parameters.AddWithValue("avg", averageScore);
    cmd.Parameters.AddWithValue("grade", grade);
    cmd.Parameters.AddWithValue("remark", remark);
    cmd.Parameters.AddWithValue("status", status);

    // await 
var insertedId = await cmd.ExecuteScalarAsync();

    return Results.Ok(new
    {
        fullName = student.FullName,
        matricNumber = student.MatricNumber,
        department = departmentName,
        university = student.University,
        totalScore,
        averageScore,
        grade,
        remark,
        status
    });
});

// 2. GET ALL SAVED RESULTS (For your Dashboard page!)
app.MapGet("/api/results", async () =>
{
    var results = new List<object>();

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    string sql = "SELECT id, full_name, matric_number, department, university, total_score, average_score, grade, status, created_at FROM student_results ORDER BY id DESC";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        results.Add(new
        {
            id = reader.GetInt32(0),
            fullName = reader.GetString(1),
            matricNumber = reader.GetString(2),
            department = reader.GetString(3),
            university = reader.GetString(4),
            totalScore = reader.GetDouble(5),
            averageScore = reader.GetDouble(6),
            grade = reader.GetString(7),
            status = reader.GetString(8),
            createdAt = reader.GetDateTime(9)
        });
    }

    return Results.Ok(results);
});

app.Run();

public record StudentInput(
    string FullName,
    string MatricNumber,
    int Department,
    string University,
    double FirstScore,
    double SecondScore
);  