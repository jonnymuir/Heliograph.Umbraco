using Heliograph.Documents;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>Who a correspondence goes to, and for which request.</summary>
internal sealed record Delivery(string InstanceId, string Correspondence, string? Email);

/// <summary>
/// Delivery, which is always the host's job: the email through Umbraco's mail settings, with the letter attached as a
/// PDF when LibreOffice is installed; the letter and the text message written to the outbox folder.
/// </summary>
internal static class CorrespondenceDelivery
{
    public static async Task DeliverAsync(IServiceProvider services, Delivery delivery, RenderedCorrespondence parts, CancellationToken ct)
    {
        var outbox = Directory.CreateDirectory(Path.Combine(
            services.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "umbraco", "Data", "Outbox", delivery.InstanceId));
        var attachments = new List<EmailMessageAttachment>();
        if (parts.Docx is { } docx)
        {
            await File.WriteAllBytesAsync(Path.Combine(outbox.FullName, $"{delivery.Correspondence}.docx"), docx, ct);
            if (services.GetService<IPdfConverter>() is { } converter && await TryConvertAsync(converter, docx, ct) is { } pdf)
            {
                await File.WriteAllBytesAsync(Path.Combine(outbox.FullName, $"{delivery.Correspondence}.pdf"), pdf, ct);
                attachments.Add(new EmailMessageAttachment(new MemoryStream(pdf), $"{delivery.Correspondence}.pdf"));
            }
        }

        if (parts.Sms is { } sms)
        {
            await File.WriteAllTextAsync(Path.Combine(outbox.FullName, "text-message.txt"), sms.Text, ct);
        }

        if (parts.Email is { } email && !string.IsNullOrWhiteSpace(delivery.Email))
        {
            var from = services.GetRequiredService<IConfiguration>()["Umbraco:CMS:Global:Smtp:From"] ?? "noreply@example.test";
            await services.GetRequiredService<IEmailSender>().SendAsync(
                new EmailMessage(from, [delivery.Email], null, null, null, email.Subject, email.Html, true, attachments),
                "Heliograph", enableNotification: false, expires: null);
        }
    }

    private static async Task<byte[]?> TryConvertAsync(IPdfConverter converter, byte[] docx, CancellationToken ct)
    {
        try
        {
            return await converter.ConvertAsync(docx, ct);
        }
        catch (PdfConversionException)
        {
            // No LibreOffice on this machine: the letter goes as a Word document only.
            return null;
        }
    }
}
