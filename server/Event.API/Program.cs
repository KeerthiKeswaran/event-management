using Microsoft.EntityFrameworkCore;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;
using Event.Data.Repositories;
using Event.Contracts.IServices;
using Event.Business.Services;
using Event.Business.Helpers;
using Event.API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddOpenApi();

// Configure JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"] ?? "EventPlatform",
        ValidAudience = jwtSection["Audience"] ?? "EventPlatformUsers",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Register DbContext
builder.Services.AddDbContext<EventDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ITermsAndConditionsRepository, TermsAndConditionsRepository>();
builder.Services.AddScoped<IBookingPaymentRepository, BookingPaymentRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();
builder.Services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IOrganizerPayoutRepository, OrganizerPayoutRepository>();
builder.Services.AddScoped<IOrganizerUpfrontPaymentRepository, OrganizerUpfrontPaymentRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped<IRegionRepository, RegionRepository>();
builder.Services.AddScoped<IAdminActionRepository, AdminActionRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Register Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<IUserAuthService, UserAuthService>();
builder.Services.AddScoped<IDeptAuthService, DeptAuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IVirtualMeetingService, VirtualMeetingService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IWaitlistRepository, WaitlistRepository>();
builder.Services.AddScoped<IWaitlistService, WaitlistService>();
builder.Services.AddHostedService<Event.Business.Services.BackgroundService>();
builder.Services.AddHostedService<Event.Business.Services.PayoutBackgroundService>();

var app = builder.Build();

app.UseCors("AllowClient");

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable Static Files so wwwroot can be served
app.UseStaticFiles();

// Serve the assets folder containing user QR codes under /assets route
var currentDir = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
string assetsPath = currentDir.EndsWith("Event.API") 
    ? Path.GetFullPath(Path.Combine(currentDir, "..", "Event.Business", "assets")) 
    : Path.GetFullPath(Path.Combine(currentDir, "Event.Business", "assets"));

if (!Directory.Exists(assetsPath))
{
    Directory.CreateDirectory(assetsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(assetsPath),
    RequestPath = "/assets",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Database seeding is only triggered explicitly via:
//   dotnet run --project Event.API seed
// It does NOT run during normal startup.
if (args != null && System.Array.IndexOf(args, "seed") >= 0)
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        System.Console.WriteLine("Executing database seeding...");
        await Event.Data.Seed.DbSeed.SeedAsync(context);
        System.Console.WriteLine("Database seeding completed successfully.");
    }
    return; // Exit after seeding — do not start the web server
}

app.Run();

