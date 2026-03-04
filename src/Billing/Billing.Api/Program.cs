using Billing.Application.Services;
using Billing.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add billing services
builder.Services.AddBillingInfrastructure(builder.Configuration);

// Add application services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IUsageReportingService, UsageReportingService>();
builder.Services.AddScoped<ICostSavingsService, CostSavingsService>();

// Add memory cache
builder.Services.AddMemoryCache();

// Add authentication (JWT Bearer)
builder.Services.AddAuthentication()
    .AddJwtBearer();

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
