using Mango.Services.PaymentAPI.Messaging;
using Mango.Services.PaymentAPI.RabbitMqSender;
using Mango.Services.PaymentAPI.RabbitMQSender;
using Microsoft.OpenApi.Models;
using PaymentProcessor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProcessPayment, ProcessPayment>();

builder.Services.AddSingleton<IRabbitMqPaymentMessageSender, RabbitMqPaymentMessageSender>();

builder.Services.AddHostedService<RabbitMqPaymentConsumer>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mango.Services.PaymentAPI", Version = "v1" });
});

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mango.Services.PaymentAPI v1"));
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapControllers();

app.Run();