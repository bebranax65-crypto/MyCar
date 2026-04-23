using Microsoft.EntityFrameworkCore;
using CarSharingWebsite.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контекст базы данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем сервисы MVC
builder.Services.AddControllersWithViews();

// Настройка сессий
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CarSharing.Session";
});

// Добавление IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Создание БД при запуске (если её нет) и добавление админа по умолчанию
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    dbContext.Database.EnsureCreated();
    
    // Создаем пользователя admin только если его нет
    if (!dbContext.Users.Any(u => u.Login == "admin"))
    {
        var admin = new User 
        { 
            Login = "admin", 
            password = 12345, 
            first_name = "Main Admin", 
            Role = "Admin", 
            registration_date = DateTime.Now 
        };
        dbContext.Users.Add(admin);
        dbContext.SaveChanges();
            
        dbContext.Administrators.Add(new Administrator 
        { 
            ID_User = admin.ID_User, 
            manage_cars = true, 
            manage_tariffs = true, 
            _view_statistics = true, 
            control_rentals = true 
        });
        dbContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();