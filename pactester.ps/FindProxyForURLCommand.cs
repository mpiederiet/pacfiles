using System;
using System.IO;
using System.Management.Automation;

namespace pacfiles
{
    [Cmdlet(VerbsCommon.Find,"ProxyForURL",
        DefaultParameterSetName = "PacFile")]
    [OutputType(typeof(string))]
    public class FindProxyForURLCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            ParameterSetName = "PacFile",
            Position = 0)]
        [Alias("File", "Pac")] 
        public string PacFile { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = "PacFunction",
            Position = 0)]
        [Alias("Function")]
        public string PacFunction { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1)]
        public Uri Url { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            Position = 1)]
        new public string Host { get; set; } = null;

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true)]
        public string MyIPAddress { get; set; } = null;

        pacparser pacparser;

        protected override void BeginProcessing()
        {
            if (ParameterSetName == "PacFile")
            {
                // This will happily return more than one path, eg if a wildcard is used.
                // Only the last one is used. TODO: fix this
                // Some confusing errors can result as there's no validation,
                // eg Access Denied if a directory is provided
                // and unexpected identifier for things that aren't pac files
                var PacPaths = SessionState.Path.GetResolvedPSPathFromPSPath(PacFile);
                foreach (var Path in PacPaths)
                {
                    PacFunction = File.ReadAllText(Path.ProviderPath);
                }
            }
            pacparser = new pacparser(PacFunction);

            if (MyIPAddress != null) {
                pacparser.myIpAddress = MyIPAddress;
                pacparser.myIpAddressEx = MyIPAddress;
            }
        }

        protected override void ProcessRecord()
        {
            WriteObject(pacparser.FindProxyForURL(Url, Host));
        }
    }
}
