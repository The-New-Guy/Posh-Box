using System.Management.Automation;

namespace PoshBox.Commands
{

    [Cmdlet(VerbsCommunications.Connect, "Box")]
    [OutputType(typeof(void))]
    public class ConnectBoxCommand : PSCmdlet
    {

        [Parameter(
            Mandatory = true,
            Position = 0)]
        public string ConfigPath { get; set; }

        [Parameter()]
        public SwitchParameter Force { get; set; }

        protected override void BeginProcessing()
        {

            if (PoshBoxAuth.IsInitialized && !Force)
            {
                WriteVerbose("Box configuration already imported. Use -Force to force a new configuration.");
                return;
            }

            WriteVerbose("Authenticating with configuration file: " + ConfigPath);

            PoshBoxAuth.LoadJWTAuthConfig(ConfigPath);

        }

    }

}
