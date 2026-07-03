using System.Text.Json;

namespace ECommerce.ServiceDefaults.Messaging;

public static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
