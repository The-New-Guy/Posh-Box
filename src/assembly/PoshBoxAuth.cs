using System.IO;
using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;

namespace PoshBox
{

    /// <summary>
    /// Authentication library used for connecting to Box API and establishing admin and user based clients.
    /// </summary>
    public static class PoshBoxAuth
    {

        // Property backing fields.
        private static bool isInitialized = false;
        private static BoxClient boxClient;

        // Properties.

        /// <summary>
        /// Indicates whether or not this library has been initialized with a Box configuration.
        /// </summary>
        public static bool IsInitialized { get => isInitialized; private set => isInitialized = value; }

        /// <summary>
        /// Returns the current Box configuration if one exists.
        /// </summary>
        public static IBoxConfig BoxConfiguration { get => boxClient?.Config; }

        /// <summary>
        /// Returns the current Box JWT authentication object. Can be used to generate new tokens and clients.
        /// </summary>
        public static BoxJWTAuth BoxJWT { get => ((JWTAuthRepository)(boxClient.Auth))?.BoxJWTAuth; }

        /// <summary>
        /// Returns the current Box administrative client if one exists.
        /// </summary>
        /// <value>Current BoxClient</value>
        public static BoxClient BoxClient { get => boxClient; private set => boxClient = value; }

        /// <summary>
        /// Loads a Box JWT authentication configuration for an app service account.
        /// </summary>
        /// <param name="configFilePath">The path to the Box JWT app configuration JSON file.</param>
        internal static void LoadJWTAuthConfig(string configFilePath)
        {
            if (string.IsNullOrWhiteSpace(configFilePath) || !File.Exists(configFilePath))
                throw new FileNotFoundException("Invalid Box configuration file path.", configFilePath);

            // Read the configuration from the file.
            IBoxConfig config;
            using (var configStream = File.OpenRead(configFilePath))
                config = BoxConfig.CreateFromJsonFile(configStream);

            // Create a Box client and authenticate as the service account.
            var boxJwtAuth = new BoxJWTAuth(config);
            var adminToken = boxJwtAuth.AdminToken();
            BoxClient = boxJwtAuth.AdminClient(adminToken);
            IsInitialized = true;
        }

        /// <summary>
        /// Creates a new authenticated client for the provided user account.
        /// </summary>
        /// <param name="userId">The Box UserID of the user the client is for.</param>
        /// <returns>An authenticated Box user client.</returns>
        internal static BoxClient NewUserClient(string userId)
        {
            try
            {
                return BoxJWT.UserClient(BoxJWT.UserToken(userId), userId);
            }
            catch (System.NullReferenceException)
            {
                throw new System.Exception("User ID not found.");
            }
        }

    }
}