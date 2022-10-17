namespace Postgrest.Connection

module Connection =
    type PostgrestClientConnection = internal {
        Url: string
        Headers: Option<Map<string, string>>
    }