namespace BarbershopReservationsUni.Web.Services;

public interface ISmsSender
{
    /// <summary>Изпраща SMS. Връща true, ако е изпратен (или подготвен за изпращане).</summary>
    Task<bool> SendSmsAsync(string phoneNumber, string message);
}
