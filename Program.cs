using Microsoft.EntityFrameworkCore;
using Raceday_API__prog_part_2_.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();