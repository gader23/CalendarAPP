namespace CalendarOcrApp.Core
{
    public static class AppConfig
    {
        /// <summary>
        /// URL base de tu backend. DEBE terminar en "/".
        /// Ejemplo: "https://calendarocr-api.azurewebsites.net/".
        /// En el emulador de Android, el PC es 10.0.2.2, pero Android bloquea HTTP sin cifrar
        /// por defecto: es más fácil probar contra el backend ya desplegado con HTTPS.
        /// </summary>
        public const string ApiBaseUrl = "http://10.0.2.2:5000/";

        /// <summary>
        /// Debe coincidir con App__Token en el servidor. Es una barrera básica, no un secreto
        /// fuerte: cualquiera que descompile la app puede leerlo. Es suficiente para empezar;
        /// más adelante conviene añadir App Attest (iOS) / Play Integrity (Android).
        /// </summary>
        public const string AppToken = "3f9a1c0b7d2e4a68b5c1d9e8f7a6b5c4";
    }
}
