using Microsoft.EntityFrameworkCore;

public class User
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}