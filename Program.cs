using CRUD.Data;
using crud.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVICES CONFIGURATION
// ==========================================

// Add MVC Controllers with Views
builder.Services.AddControllersWithViews();

// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Session Management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ==========================================
// 2. HTTP REQUEST PIPELINE (MIDDLEWARE)
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ==========================================
// 3. DATABASE SEEDING (DEFAULT ADMIN USER)
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Apply any pending EF Core migrations before the application starts.
    // This keeps the database schema in sync with the MVC models.
    context.Database.Migrate();

    if (!context.Users.Any())
    {
        var admin = new UserModel
        {
            UserName = "admin",
            Email = "admin@gmail.com",
            UserRole = "Admin"
        };

        var hasher = new PasswordHasher<UserModel>();
        admin.Password = hasher.HashPassword(admin, "admin123");

        context.Users.Add(admin);
        context.SaveChanges();
    }
}

app.Run();