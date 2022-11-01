namespace Postgrest.StatefulConnection

module StatefulConnection =
    type PostgrestClientConnection = internal {
        Url: string
        Headers: Option<Map<string, string>>
    }