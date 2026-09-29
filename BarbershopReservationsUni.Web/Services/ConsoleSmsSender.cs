namespace BarbershopReservationsUni.Web.Services;

/// <summary>
/// Заместител на SMS доставчик за разработка: записва съобщението в лога.
/// В продукция трябва да се замени с реален доставчик – кодовете не бива да се логват.
/// </summary>
public class ConsoleSmsSender : ISmsSender
{
    private readonly ILogger<ConsoleSmsSender> logger;
    private readonly IHostEnvironment environment;

    public ConsoleSmsSender(ILogger<ConsoleSmsSender> logger, IHostEnvironment environment)
    {
        this.logger = logger;
        this.environment = environment;
    }

    public Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogWarning("SMS не е изпратен: не е конфигуриран SMS доставчик (среда {Environment}).", environment.EnvironmentName);
            return Task.FromResult(false);
        }

        logger.LogInformation("[SMS] До: {Phone} Съобщение: {Message}", phoneNumber, message);
        return Task.FromResult(true);
    }
}
