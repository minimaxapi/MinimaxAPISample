namespace MinimaxAPISample
{
    class Config
    {
        public static string Lokalizacija => "SI"; // RS, HR
        public static string BaseUrl => $"https://moj.minimax.{Lokalizacija}/{Lokalizacija}/";
        public static string ApiTokenEndpoint => BaseUrl + "AUT/OAuth20/Token";
        public static string APIBaseUrl => BaseUrl + "API/";

        public static string UserName => "xxx";
        public static string Password => "xxx";

        public static string ClientId => "xxx";
        public static string ClientSecret => "xxx";

    }
}
