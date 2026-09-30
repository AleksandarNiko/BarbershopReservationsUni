using BarbershopReservationsUni.Data;
using BarbershopReservationsUni.Services;
using BarbershopReservationsUni.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;


//номер за влизане в първия акаунт на бръснаря: 1000000000, а за втория: 2000000000.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<BarbershopReservationsUniDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

builder.Services
    .AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.OtpSecret) && o.OtpSecret.Length >= 32,
        "Booking:OtpSecret трябва да е зададена и да е поне 32 символа (използвайте user-secrets или променлива на средата).")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IBookingService, BookingService>();

// Подписана и криптирана сесия за „Моите часове“ вместо бисквитка с чист телефонен номер.
builder.Services.AddDataProtection();
builder.Services.AddSingleton<ClientSession>();
builder.Services.AddSingleton<BarberSession>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Автоматично прилагане на миграциите само в Development (в продукция – ръчно/чрез pipeline).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BarbershopReservationsUniDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
