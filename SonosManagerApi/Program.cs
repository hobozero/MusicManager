using SonosManagerApi.Application;
using SonosManagerApi.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://192.168.0.31:3000", "http://192.168.0.122:90")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.Configure<NAS>(builder.Configuration.GetSection("NAS"));


builder.Services
    .AddScoped<FileAdapter>()
    .AddScoped<SonosAdapter>()
    .AddScoped<PlaylistAdpater>()
    .AddSingleton<SonosDiscovery>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.UseCors("AllowReactApp");
app.MapControllers();
app.Run();
