
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserRegistrationConfirmationService, UserRegistrationConfirmationService>();
builder.Services.AddExceptionHandler<BloqqerExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<ArticleCreateValidator>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication("Bearer").AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

builder.Services.AddDbContext<BloqqerDbContext>(options =>
{
    options.AddInterceptors(new BloqqerSaveChangesInterceptor());
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloqqerDatabase"));
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();
app.MapControllers();
app.Run();

/// <summary>
/// This is essential for the integration tests to be able
/// to reference this application in the WebApplicationFactory<T>
/// generic type parameter.
/// </summary>
public partial class Program;